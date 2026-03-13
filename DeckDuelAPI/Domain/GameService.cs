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
    }

    public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepo;
        private readonly IGameRealtimeNotifier _notifier; 
        private int _firstPlayerId;

        public GameService(IGameRepository gameRepo, IGameRealtimeNotifier notifier) 
        {
            _gameRepo = gameRepo;
            _notifier = notifier;
        }

        public async Task<DDResult<UserGameDto>> CreateGameAsync(int deckId, int userId)
        {
            //TODO: Check if the deck exists and if the user has access to it
            //TODO: Check if there's already an open game for this user/deck combination
            
            var userGameDto = await _gameRepo.CreateGameAsync(deckId, userId);
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
            _firstPlayerId = firstPlayer.Id;
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
            await _notifier.NotifyGameStartedAsync(gameId, _firstPlayerId);

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
            var game = await _gameRepo.GetGameAsync(gameId);
            if (game == null)
                return DDResult<Game>.Fail(DDError.NotFound, "Game not found.");

            if (game.StartedAt == null || game.FinishedAt != null)
                return DDResult<Game>.Fail(DDError.InvalidInput, "Game is not active.");

            var userGame = await _gameRepo.GetUserGameAsync(gameId, userId);
            if (userGame == null)
                return DDResult<Game>.Fail(DDError.NotFound, "User is not in this game.");

            
            if (userGame.Id == game.CurrentRoundUserGameId)
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

            //if we have a turn for each player in this round, then we can determine
            //the winner of the round and update the game state accordingly
            //(e.g. increment CurrentRoundId, set CurrentRoundUserGameId to the winner of the round, etc.)
            var turns = await _gameRepo.GetTurnsForRoundAsync(game.Id, game.CurrentRoundNumber);
            var userGames = await _gameRepo.GetUsersStillInGameAsync(game.Id, game.CurrentRoundNumber);

            if (turns.Length == userGames.Length)
            {

                // Get the category type to find the position for scoring
                var categoryType = await _gameRepo.GetCategoryTypeAsync(currentRound.CategoryTypeId);
                if (categoryType == null)
                    return DDResult<Game>.Fail(DDError.NotFound, "Category type not found.");

                // Get card details with scores for the chosen category
                var turnScores = new List<(Turn turn, int cardId, int score)>();
                foreach (var turn in turns)
                {
                    var card = await _gameRepo.GetCardAsync(turn.CardId);
                    if (card == null) continue;

                    // Find the category score at the chosen position
                    var categoryScore = card.Categories
                        .FirstOrDefault(c => c.Position == categoryType.Position)?.Score ?? 0;

                    turnScores.Add((turn, card.Id, categoryScore));
                }

                if (turnScores.Count == 0)
                    return DDResult<Game>.Fail(DDError.InvalidInput, "No valid turns found.");

                // Determine winner (highest score)
                var winner = turnScores.OrderByDescending(ts => ts.score).First();
                var winnerUserGameId = winner.turn.UserGameId;
                var allPlayedCards = turnScores.Select(ts => ts.cardId).ToList();

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
                        // Winner: add their played card back + all other played cards
                        newHandCards = currentCards;
                        newHandCards.Add(playedCardId); // their card first
                        newHandCards.AddRange(allPlayedCards.Where(c => c != playedCardId)); // won cards
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

                // Advance to next round
                game.CurrentRoundNumber++;
                game.CurrentRoundUserGameId = winnerUserGameId; // winner chooses category next
                await _gameRepo.SaveChangesAsync();

                // Notify players
                //await _notifier.NotifyRoundCompletedAsync(game.Id, winnerUserGameId, game.CurrentRoundNumber);

                return DDResult<Game>.Ok(game);

            }

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
                CardId = cardDto.Id,
                Name = cardDto.Name,
                Categories = cardDto.Categories,
                MyTurn = myTurn
            };

            return DDResult<TurnCardDto>.Ok(turnCard);
        }
    }
}
