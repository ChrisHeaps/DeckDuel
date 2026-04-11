using DeckDuel2.DTOs;
using DeckDuel2.Models;
using DeckDuel2.Repositories;

namespace DeckDuel2.Domain
{
    public interface IGameService
    {
        Task<DDResult<UserGameDto>> CreateGameAsync(int deckId, int userId);
        Task<DDResult<Game>> StartGameAsync(int gameId, int userId);
        Task<DDResult<Game>> JoinGameAsync(int gameId, int userId);
        Task<DDResult<OpenGameDto[]>> GetOpenGamesAsync(int userId);
        Task<DDResult<ActiveGameDto[]>> GetActiveGamesAsync(int userId);
        Task<DDResult<Game>> TakeTurnAsync(int gameId, int userId, int categoryTypeId);
        Task<DDResult<TurnCardDto>> GetCurrentHandTopCardAsync(int userGameId, int userId);
        Task<DDResult<GameStatusDto>> GetCurrentGameStatusAsync(int userGameId, int userId);
    }

    public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepo;
        private readonly IGameRealtimeNotifier _notifier; 
      
        public GameService(IGameRepository gameRepo, IGameRealtimeNotifier notifier) 
        {
            _gameRepo = gameRepo;
            _notifier = notifier;
        }

        public async Task<DDResult<UserGameDto>> CreateGameAsync(int deckId, int userId)
        {
            var userGameDto = await _gameRepo.CreateGameAsync(deckId, userId);
            await _notifier.NotifyGameOpenedAsync(userGameDto.GameId);
            return DDResult<UserGameDto>.Ok(userGameDto);
        }

        public async Task<DDResult<Game>> StartGameAsync(int gameId, int userId)
        {
            // 1. Validate owner
            if (!_gameRepo.IsGameOwner(gameId, userId))
                return DDResult<Game>.Fail(DDError.NotOwner, "Only the game owner can start the game.");

            // 2. Load game
            var game = await _gameRepo.GetGameAsync(gameId);
            if (game == null)
                return DDResult<Game>.Fail(DDError.NotFound, "Game not found.");

            //Ensure minimum players (add bot if only one)
            var playerCount = await _gameRepo.GetPlayerCountAsync(gameId);
            if (playerCount == 1)
            {
                var botId = await _gameRepo.GetBotUserIdAsync();
                if (botId != 0)
                {
                    await _gameRepo.AddUserToGameAsync(gameId, botId);
                    playerCount++;
                }
            }

            var userGames = await _gameRepo.GetUserGamesAsync(gameId);            
            //Choose one at random from userGames to be the first player.
            //This player will be the one to choose the category for the first round. 
            if (userGames == null)
                return DDResult<Game>.Fail(DDError.InvalidInput, "No players in game.");

            var firstPlayer = userGames[Random.Shared.Next(userGames.Length)];           
            game.CurrentRoundUserGameId = firstPlayer.Id; // set the first player to choose category for the first round
               
           
            // 4. Load deck cards
            var cards = await _gameRepo.GetDeckCardsForGameAsync(game.DeckId);
            if (cards == null || cards.Length == 0)
                return DDResult<Game>.Fail(DDError.InvalidInput, "Deck has no cards.");

            // 5. Shuffle and distribute
            var rnd = new Random();
            var shuffled = cards.OrderBy(_ => rnd.Next()).ToArray();

            // prepare hands
            var playerHands = new Dictionary<int, List<int>>();
            foreach (var ug in userGames)
                playerHands[ug.UserId] = new List<int>();

            for (int i = 0; i < shuffled.Length; i++)
            {
                var targetUserId = userGames[i % userGames.Length].UserId;
                playerHands[targetUserId].Add(shuffled[i].Id);
            }

            // 6. Persist hands
            foreach (var kvp in playerHands)
            {
                await _gameRepo.CreateHandAsync(gameId, 1, kvp.Key, string.Join(",", kvp.Value));
            }

            // 7. Mark started and save
            game.StartedAt = DateTime.UtcNow;
            game.CurrentRoundNumber = 1;    
            await _gameRepo.SaveChangesAsync();

            //notify all players of game start
            await _notifier.NotifyGameStartedAsync(gameId);

            return DDResult<Game>.Ok(game);
        }

        //add method JoinGameAsyncto join game. Only allow joining if the game is in the "open" state and not already started. Check if the user is already in the game. If they are, return their existing UserGameDto. If not, add them to the game and return the new UserGameDto.
        public async Task<DDResult<Game>> JoinGameAsync(int gameId, int userId)
        {

            // 1. Load game
            var game = await _gameRepo.GetGameAsync(gameId);
            if (game == null)
                return DDResult<Game>.Fail(DDError.NotFound, "Game not found.");
            
            // 2. Check if game is open
            if (game.StartedAt != null)
                return DDResult<Game>.Fail(DDError.InvalidInput, "Game has already started.");
            
            // 3. Check if user is already in game
            var existingUserGame = await _gameRepo.GetUserGameAsync(gameId, userId);
            if (existingUserGame != null)   
                return DDResult<Game>.Ok(game); // User already in game, return existing game info

            // 4. Add user to game
            await _gameRepo.AddUserToGameAsync(gameId, userId);

            await _notifier.NotifyGameJoinedAsync(game.Id);

            return DDResult<Game>.Ok(game);

        }

        public async Task<DDResult<OpenGameDto[]>> GetOpenGamesAsync(int userId)
        {            
            var dto = await _gameRepo.GetOpenGamesAsync(userId);
            return DDResult<OpenGameDto[]>.Ok(dto);
        }

        public async Task<DDResult<ActiveGameDto[]>> GetActiveGamesAsync(int userId)
        {
            var dto = await _gameRepo.GetActiveGamesAsync(userId);
            return DDResult<ActiveGameDto[]>.Ok(dto);
        }

        public async Task<DDResult<Game>> TakeTurnAsync(int gameId, int userId, int categoryTypeId)
        {
            var (game, userGame, error) = await ValidateAndLoadTurnContextAsync(gameId, userId);
            if (error != null)
                return error;

            if (userGame!.Id == game!.CurrentRoundUserGameId)
                //starting player for round is kicking off new round
                //add a new round with the categoryType chosen by this player for this round and
                //set the RoundNumber to the Game CurrentRoundId
                await _gameRepo.AddRoundAsync(new Round
                {
                    GameId = gameId,
                    CategoryTypeId = categoryTypeId,
                    RoundNumber = game.CurrentRoundNumber
                });

            var currentRound = await _gameRepo.GetRoundAsync(game.Id, game.CurrentRoundNumber);

            if (currentRound == null)
                return DDResult<Game>.Fail(DDError.InvalidInput, "Current player has not yet chosen a category for this round.");

            var addTurnError = await AddTurnFromTopCardAsync(game, userGame);
            if (addTurnError != null)
                return addTurnError;

            //if we have a turn for each player in this round, then we can determine
            //the winner of the round and update the game state accordingly
            //(e.g. increment CurrentRoundId, set CurrentRoundUserGameId to the winner of the round, etc.)
            var (isRoundComplete, turns, userGames) = await GetRoundStateAsync(game);

            if (isRoundComplete)
            {

                // Get the category type to find the position for scoring
                var categoryType = await _gameRepo.GetCategoryTypeAsync(currentRound.CategoryTypeId);
                if (categoryType == null)
                    return DDResult<Game>.Fail(DDError.NotFound, "Category type not found.");

                // Get card details with scores for the chosen category
                var turnScores = await BuildTurnScoresAsync(turns, categoryType.Position);

                if (turnScores.Count == 0)
                    return DDResult<Game>.Fail(DDError.InvalidInput, "No valid turns found.");

                var allPlayedCards = turnScores.Select(ts => ts.cardId).ToList();

                // Determine winner (highest score)
                var winner = turnScores.OrderByDescending(ts => ts.score).First();


                //if at this point there is a tie for the highest score we add all cards to Game.DrawPileCardList and start a new round 
                //check for tie
                var highestScore = winner.score;
                var tiedPlayers = turnScores.Where(ts => ts.score == highestScore).ToList();
                if (tiedPlayers.Count > 1)
                {
                    return await ResolveTieAsync(game, turnScores);
                }

                return await ResolveWinnerAsync(
                    game,
                    userGames,
                    turnScores,
                    allPlayedCards,
                    winner.turn.UserGameId);
            }

            // if we reach here, a turn was taken but the round is not complete yet
            await _notifier.NotifyTurnChangedAsync(game.Id, userGame.Id, game.CurrentRoundNumber);

            return DDResult<Game>.Ok(game);
        }

        public async Task<DDResult<TurnCardDto>> GetCurrentHandTopCardAsync(int userGameId, int userId)
        {
            var userGame = await _gameRepo.GetUserGameByIdAsync(userGameId);
            if (userGame == null)
                return DDResult<TurnCardDto>.Fail(DDError.NotFound, "UserGame not found.");

            // Security: user can only fetch their own card
            if (userGame.UserId != userId)
                return DDResult<TurnCardDto>.Fail(DDError.NotOwner, "Not allowed to view this hand.");

            var game = await _gameRepo.GetGameAsync(userGame.GameId);
            if (game == null)
                return DDResult<TurnCardDto>.Fail(DDError.NotFound, "Game not found.");

            var hand = await _gameRepo.GetLatestHandAsync(userGameId);
            if (hand == null || string.IsNullOrWhiteSpace(hand.CardList))
                return DDResult<TurnCardDto>.Fail(DDError.NotFound, "No current hand found.");

            var firstCardId = hand.CardList
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var id) ? id : (int?)null)
                .FirstOrDefault(x => x.HasValue);

            if (firstCardId == null)
                return DDResult<TurnCardDto>.Fail(DDError.NotFound, "No card in hand.");

            var cardDto = await _gameRepo.GetCardDetailsDtoAsync(firstCardId.Value);
            if (cardDto == null)
                return DDResult<TurnCardDto>.Fail(DDError.NotFound, "Card not found.");

            // Same MyTurn logic as GameRepository.GetActiveGamesAsync
            var turns = await _gameRepo.GetTurnsForRoundAsync(game.Id, game.CurrentRoundNumber);

            var myTurn =
                !turns.Any(t => t.UserGameId == userGameId) &&
                (
                    game.CurrentRoundUserGameId == userGameId ||
                    turns.Any(t => t.UserGameId == game.CurrentRoundUserGameId)
                );

            var turnCard = new TurnCardDto
            {
                GameId = game.Id,
                CardId = cardDto.Id,
                Name = cardDto.Name,
                Categories = cardDto.Categories,
                MyTurn = myTurn
            };

            return DDResult<TurnCardDto>.Ok(turnCard);
        }

        public async Task<DDResult<GameStatusDto>> GetCurrentGameStatusAsync(int userGameId, int userId)
        {
            var requestingUserGame = await _gameRepo.GetUserGameByIdAsync(userGameId);
            if (requestingUserGame == null)
                return DDResult<GameStatusDto>.Fail(DDError.NotFound, "UserGame not found.");

            if (requestingUserGame.UserId != userId)
                return DDResult<GameStatusDto>.Fail(DDError.NotOwner, "Not allowed to view this game status.");

            var game = await _gameRepo.GetGameAsync(requestingUserGame.GameId);
            if (game == null)
                return DDResult<GameStatusDto>.Fail(DDError.NotFound, "Game not found.");

            var currentRound = await _gameRepo.GetRoundAsync(game.Id, game.CurrentRoundNumber);

            CategoryType? categoryType = null;
            if (currentRound?.CategoryTypeId != null)
                categoryType = await _gameRepo.GetCategoryTypeAsync(currentRound.CategoryTypeId);

            var userGames = await _gameRepo.GetUserGamesAsync(game.Id);
            var turns = await _gameRepo.GetTurnsForRoundAsync(game.Id, game.CurrentRoundNumber);

            var turnScoresByUserGameId = new Dictionary<int, int>();
            if (categoryType != null)
            {
                foreach (var turn in turns)
                {
                    var card = await _gameRepo.GetCardAsync(turn.CardId);
                    var score = card?.Categories
                        .FirstOrDefault(c => c.Position == categoryType.Position)
                        ?.Score;

                    if (score.HasValue)
                        turnScoresByUserGameId[turn.UserGameId] = score.Value;
                }
            }

            var players = new List<GameStatusPlayerDto>();

            foreach (var ug in userGames)
            {
                int handCardCount;

                if (game.FinishedAt != null)
                {
                    // Terminal state: only winner keeps cards
                    handCardCount = ug.Id == game.WinningUserGameId
                        ? (await _gameRepo.GetHandAsync(ug.Id, game.CurrentRoundNumber) is { CardList: var list } && !string.IsNullOrWhiteSpace(list)
                            ? list.Split(',', StringSplitOptions.RemoveEmptyEntries).Length
                            : 0)
                        : 0;
                }
                else
                {
                    var hand = await _gameRepo.GetHandAsync(ug.Id, game.CurrentRoundNumber)
                        ?? await _gameRepo.GetLatestHandAsync(ug.Id);

                    handCardCount = string.IsNullOrWhiteSpace(hand?.CardList)
                        ? 0
                        : hand.CardList.Split(',', StringSplitOptions.RemoveEmptyEntries).Length;
                }

                players.Add(new GameStatusPlayerDto
                {
                    InGameName = ug.User?.InGameName ?? string.Empty,
                    HandCardCount = handCardCount,
                    CurrentTurnScore = turnScoresByUserGameId.TryGetValue(ug.Id, out var score) ? score : null
                });
            }

            var winningUserInGameName = game.WinningUserGameId == null
                ? null
                : userGames
                    .FirstOrDefault(ug => ug.Id == game.WinningUserGameId)?
                    .User?
                    .InGameName;

            var dto = new GameStatusDto
            {
                GameId = game.Id,
                CurrentRoundCategoryName = categoryType?.Description,
                Players = players,
                IsGameOver = game.FinishedAt != null,
                WinningUserGameId = game.WinningUserGameId,
                WinningUserInGameName = winningUserInGameName
            };

            return DDResult<GameStatusDto>.Ok(dto);
        }

        private async Task<(Game? game, UserGame? userGame, DDResult<Game>? error)> ValidateAndLoadTurnContextAsync(int gameId, int userId)
        {
            var game = await _gameRepo.GetGameAsync(gameId);
            if (game == null)
                return (null, null, DDResult<Game>.Fail(DDError.NotFound, "Game not found."));

            if (game.StartedAt == null || game.FinishedAt != null)
                return (null, null, DDResult<Game>.Fail(DDError.InvalidInput, "Game is not active."));

            var userGame = await _gameRepo.GetUserGameAsync(gameId, userId);
            if (userGame == null)
                return (null, null, DDResult<Game>.Fail(DDError.NotFound, "User is not in this game."));

            return (game, userGame, null);
        }

        private async Task<DDResult<Game>?> AddTurnFromTopCardAsync(Game game, UserGame userGame)
        {
            //first add a Turn for this player's move with the card they played
            //(the left most card in their hand) i.e. the left most id from the
            //comma separated string in Hand.CardList

            //get the hand for this player and parse the CardList to get the first card id
            var hand = await _gameRepo.GetHandAsync(userGame.Id, game.CurrentRoundNumber);
            if (hand == null)
                return DDResult<Game>.Fail(DDError.NotFound, "No hand for this player");

            var firstCardId = hand.CardList?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x, out var id) ? id : (int?)null)
                    .FirstOrDefault(x => x.HasValue);

            //only add a turn if the player has a card to play 
            if (firstCardId != null)
            {
                //add a turn for this player with the chosen category and card
                await _gameRepo.AddTurnAsync(new Turn
                {
                    RoundNumber = game.CurrentRoundNumber,
                    UserGameId = userGame.Id,
                    CardId = firstCardId.Value
                });
            }

            return null;
        }

        private async Task<(bool isRoundComplete, Turn[] turns, int[] userGames)> GetRoundStateAsync(Game game)
        {
            var turns = await _gameRepo.GetTurnsForRoundAsync(game.Id, game.CurrentRoundNumber);
            var userGames = await _gameRepo.GetUsersStillInGameAsync(game.Id, game.CurrentRoundNumber);
            return (turns.Length == userGames.Length, turns, userGames);
        }

        private async Task<List<(Turn turn, int cardId, int score)>> BuildTurnScoresAsync(Turn[] turns, int categoryPosition)
        {
            // Get card details with scores for the chosen category
            var turnScores = new List<(Turn turn, int cardId, int score)>();

            foreach (var turn in turns)
            {
                var card = await _gameRepo.GetCardAsync(turn.CardId);
                if (card == null) continue;

                // Find the category score at the chosen position
                var categoryScore = card.Categories
                    .FirstOrDefault(c => c.Position == categoryPosition)?.Score ?? 0;

                turnScores.Add((turn, card.Id, categoryScore));
            }

            return turnScores;
        }

        private async Task<DDResult<Game>> ResolveTieAsync(Game game, List<(Turn turn, int cardId, int score)> turnScores)
        {
            // It's a tie - add all played cards to the draw pile
            var allPlayedCardIds = turnScores.Select(ts => ts.cardId).ToList();
            var existingDrawPile = string.IsNullOrWhiteSpace(game.DrawPileCardList)
                ? new List<int>()
                : game.DrawPileCardList.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x, out var id) ? id : 0)
                    .Where(x => x > 0)
                    .ToList();

            existingDrawPile.AddRange(allPlayedCardIds);
            game.DrawPileCardList = string.Join(",", existingDrawPile);

            // Build next-round hands with played cards removed
            var playersWhoPlayed = turnScores
                .GroupBy(ts => ts.turn.UserGameId)
                .Select(g => g.First());

            foreach (var turnScore in playersWhoPlayed)
            {
                var currentHand = await _gameRepo.GetHandAsync(turnScore.turn.UserGameId, game.CurrentRoundNumber);
                if (currentHand == null)
                    continue;

                var nextHandCards = string.IsNullOrWhiteSpace(currentHand.CardList)
                    ? new List<int>()
                    : currentHand.CardList
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => int.TryParse(x, out var id) ? id : 0)
                        .Where(x => x > 0)
                        .ToList();

                // Remove the played card that was moved to draw pile
                nextHandCards.Remove(turnScore.cardId);

                // No hand created means player is out
                if (nextHandCards.Count == 0)
                    continue;

                var userGame = await _gameRepo.GetUserGameByIdAsync(turnScore.turn.UserGameId);
                if (userGame == null)
                    continue;

                await _gameRepo.CreateHandAsync(
                    game.Id,
                    game.CurrentRoundNumber + 1,
                    userGame.UserId,
                    string.Join(",", nextHandCards));
            }

            // Start new round with same category starter
            game.CurrentRoundNumber++;
            await _gameRepo.SaveChangesAsync();

            // Notify players of new round due to tie
            await _notifier.NotifyTurnChangedAsync(game.Id, game.CurrentRoundUserGameId, game.CurrentRoundNumber);

            return DDResult<Game>.Ok(game);
        }

        private async Task<DDResult<Game>> ResolveWinnerAsync(
            Game game,
            int[] userGames,
            List<(Turn turn, int cardId, int score)> turnScores,
            List<int> allPlayedCards,
            int winnerUserGameId)
        {
            // Parse tied cards already in draw pile (from previous tied rounds)
            var drawPileCards = string.IsNullOrWhiteSpace(game.DrawPileCardList)
                ? new List<int>()
                : game.DrawPileCardList
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x, out var id) ? id : 0)
                    .Where(x => x > 0)
                    .ToList();

            // Create new hands for all players
            foreach (var usersStillIn in userGames)
            {
                var ug = await _gameRepo.GetUserGameAsync(game.Id, usersStillIn);
                if (ug == null) continue;

                var currentHand = await _gameRepo.GetHandAsync(ug.Id, game.CurrentRoundNumber);
                if (currentHand == null) continue;

                var currentCards = currentHand.CardList
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x, out var id) ? id : 0)
                    .Where(x => x > 0)
                    .ToList();

                // Find this player's played card
                var playedCardId = turnScores.FirstOrDefault(ts => ts.turn.UserGameId == ug.Id).cardId;

                // Remove the played card from hand
                currentCards.Remove(playedCardId);

                List<int> newHandCards;

                if (ug.Id == winnerUserGameId)
                {
                    // Winner: add their played card back + all other played cards + draw pile cards
                    newHandCards = currentCards;
                    newHandCards.Add(playedCardId); // their card first
                    newHandCards.AddRange(allPlayedCards.Where(c => c != playedCardId)); // won cards from this round
                    newHandCards.AddRange(drawPileCards); // won cards from tied rounds
                }
                else
                {
                    // Loser: just the remaining cards (already removed played card)
                    newHandCards = currentCards;
                }

                // Only create hand if player has cards left
                if (newHandCards.Count > 0)
                {
                    await _gameRepo.CreateHandAsync(
                        game.Id,
                        game.CurrentRoundNumber + 1,
                        usersStillIn,
                        string.Join(",", newHandCards)
                    );
                }
            }

            // Winner has now claimed draw pile cards, so clear it
            game.DrawPileCardList = string.Empty;

            //if at this point the winning player has Deck.Count cards they have won the game - set FinishedAt and return
            var winnerHand = await _gameRepo.GetHandAsync(winnerUserGameId, game.CurrentRoundNumber + 1);
            var winnerCardCount = string.IsNullOrWhiteSpace(winnerHand?.CardList)
                ? 0
                : winnerHand!.CardList
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Length;

            var totalCardsInGame = await _gameRepo.GetTotalCardCountForGameAsync(game.Id);
            if (winnerCardCount >= totalCardsInGame)
            {
                game.FinishedAt = DateTime.UtcNow;
                game.WinningUserGameId = winnerUserGameId;

                // Move state to the hand round you just created
                game.CurrentRoundNumber++;
                game.CurrentRoundUserGameId = winnerUserGameId;

                await _gameRepo.SaveChangesAsync();
                await _notifier.NotifyGameFinishedAsync(game.Id);
                return DDResult<Game>.Ok(game);
            }

            // Advance to next round
            game.CurrentRoundNumber++;
            game.CurrentRoundUserGameId = winnerUserGameId; // winner chooses category next
            await _gameRepo.SaveChangesAsync();

            // Notify players
            await _notifier.NotifyTurnChangedAsync(game.Id, winnerUserGameId, game.CurrentRoundNumber);

            return DDResult<Game>.Ok(game);
        }
    }
}
