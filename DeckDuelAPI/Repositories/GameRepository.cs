using DeckDuel2.Data;
using DeckDuel2.DTOs;
using DeckDuel2.Models;
using Microsoft.EntityFrameworkCore;

namespace DeckDuel2.Repositories
{
    public class GameRepository : IGameRepository
    {

        private readonly AppDbContext _db;
        private readonly IDeckRepository _deckRepo;

        public GameRepository(AppDbContext db, IDeckRepository deckRepo)
        {
            _db = db;
            _deckRepo = deckRepo;
        }

        public bool IsGameOwner(int gameId, int userId)
        {
            var game = _db.Games.Find(gameId);
            return game != null && game.OwnerUserId == userId;
        }


        public async Task<UserGameDto> CreateGameAsync(int deckId, int userId)
        {
            var game = new Game
            {
                DeckId = deckId,
                OwnerUserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            var userGame = new UserGame
            {
                UserId = userId,
                Game = game
            };

            _db.UserGames.Add(userGame);
            await _db.SaveChangesAsync();

            return new UserGameDto
            {
                Id = userGame.Id,
                UserId = userGame.UserId,
                GameId = userGame.GameId
            };
        }

        public async Task<int> GetPlayerCountAsync(int gameId)
        {
            return await _db.UserGames.CountAsync(ug => ug.GameId == gameId);
        }

        // Data access helpers for service orchestration
        public async Task<Game?> GetGameAsync(int gameId)
        {
            return await _db.Games.FindAsync(gameId);
        }

        public async Task<int> GetBotUserIdAsync()
        {
            return await _db.Users.Where(u => u.IsBot).Select(u => u.Id).FirstOrDefaultAsync();
        }


        public async Task AddUserToGameAsync(int gameId, int userId)
        {
            var game = await _db.Games.FindAsync(gameId);
            if (game == null) return;
            _db.UserGames.Add(new UserGame { UserId = userId, Game = game });
            await _db.SaveChangesAsync();
        }


        public async Task<ActiveGameDto[]> GetActiveGamesAsync(int userId)
        {
            return await _db.Games
                .Where(g =>
                    g.StartedAt != null &&
                    g.FinishedAt == null &&
                    g.UserGames.Any(ug => ug.UserId == userId))
                .Select(g => new ActiveGameDto
                {
                    UserGameId = g.UserGames
                        .Where(ug => ug.UserId == userId)
                        .Select(ug => ug.Id)
                        .FirstOrDefault(),

                    DeckTopic = _db.Decks
                .Where(d => d.Id == g.DeckId)
                .Select(d => d.Topic!)
                .FirstOrDefault() ?? string.Empty,

                    IsOwned = g.OwnerUserId == userId,                

                    UserNames = g.UserGames
                        .Select(ug => ug.User.InGameName)
                        .ToList(),

                    MyTurn =
                        // this user has not played this round
                        !_db.Turns.Any(t =>
                            t.RoundNumber == g.CurrentRoundNumber &&
                            t.UserGameId == g.UserGames
                                .Where(ug => ug.UserId == userId)
                                .Select(ug => ug.Id)
                                .FirstOrDefault())
                        &&
                        (
                            // starter is this user
                            g.CurrentRoundUserGameId == g.UserGames
                                .Where(ug => ug.UserId == userId)
                                .Select(ug => ug.Id)
                                .FirstOrDefault()
                            ||
                            // starter already played this round
                            _db.Turns.Any(t =>
                                t.RoundNumber == g.CurrentRoundNumber &&
                                t.UserGameId == g.CurrentRoundUserGameId)
                        )
                })
                .ToArrayAsync();
        }

        //GetUserGameAsync
        public async Task<UserGame?> GetUserGameAsync(int gameId, int userId)
        {   
            return await _db.UserGames.FirstOrDefaultAsync(ug => ug.GameId == gameId && ug.UserId == userId);
        }

        public async Task<UserGame?> GetUserGameByIdAsync(int userGameId)
        {
            return await _db.UserGames.FirstOrDefaultAsync(ug => ug.Id == userGameId);
        }

        public async Task<UserGame[]> GetUserGamesAsync(int gameId)
        {
            return await _db.UserGames
                .Include(ug => ug.User)
                .Where(ug => ug.GameId == gameId)
                .ToArrayAsync();
        }

        public async Task<UserGame[]> GetUserGamesForUserAsync(int userId)
        {
            return await _db.UserGames.Where(ug => ug.UserId == userId).ToArrayAsync();
        }


        public async Task CreateHandAsync(int gameId, int roundNum, int userId, string cardList)
        {
            var userGame = await _db.UserGames
                .Include(ug => ug.Hands)
                .FirstOrDefaultAsync(ug => ug.GameId == gameId && ug.UserId == userId);

            if (userGame == null) return;
           
            var hand = new Hand { CardList = cardList, UserGameId = userGame.Id, RoundNumber = roundNum };
            _db.Hands.Add(hand);
            
            await _db.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        // Convenience wrapper to fetch deck cards for a game (delegates to IDeckRepository)
        public async Task<CardDto[]> GetDeckCardsForGameAsync(int deckId)
        {
            return await _deckRepo.GetDeckCardsAsync(deckId, null);
        }

        //GetRoundAsync
        public async Task<Round?> GetRoundAsync(int gameId, int roundNum)
        {
            //get round for game and round number
            return await _db.Rounds.FirstOrDefaultAsync(r => r.GameId == gameId && r.RoundNumber == roundNum);
        }


        public async Task<Hand?> GetHandAsync(int userGameId, int roundNum)
        {
            return await _db.Hands.FirstOrDefaultAsync(h => h.UserGameId == userGameId && h.RoundNumber == roundNum);
        }


        //GetTurnsForRoundAsync
        public async Task<Turn[]> GetTurnsForRoundAsync(int gameId, int roundNum)
        {
            //return turns for game and round number
            return await _db.Turns
                .Include(t => t.UserGame)
                .Where(t => t.UserGame.GameId == gameId && t.RoundNumber == roundNum)
                .ToArrayAsync();
        }

        //GetUsersStillInGameAsync(game.Id, game.CurrentRoundNumber);
        //a user is still in the game if they have a Hand for the current round
        public async Task<int[]> GetUsersStillInGameAsync(int gameId, int roundNum)
        {
            return await _db.UserGames
                .Where(ug => ug.GameId == gameId && ug.Hands.Any(h => h.RoundNumber == roundNum))
                .Select(ug => ug.UserId)
                .ToArrayAsync();
        }


        //AddRoundAsync
        public async Task<Round> AddRoundAsync(Round round)
        {
            _db.Rounds.Add(round);
            await _db.SaveChangesAsync();
            return round;
        }

        //AddTurnAsync
        public async Task<Turn> AddTurnAsync(Turn turn)
        {
            _db.Turns.Add(turn);
            await _db.SaveChangesAsync();
            return turn;
        }

        //GetCardAsync(turn.CardId);
        public async Task<Card> GetCardAsync(int cardId)
        {
            return await _deckRepo.GetCardAsync(cardId);            
        }


        public async Task<Hand?> GetLatestHandAsync(int userGameId)
        {
            return await _db.Hands
                .Where(h => h.UserGameId == userGameId)
                .OrderByDescending(h => h.RoundNumber)
                .FirstOrDefaultAsync();
        }

        public async Task<CardDto?> GetCardDetailsDtoAsync(int cardId)
        {
            return await _db.Cards
                .Where(c => c.Id == cardId)
                .Select(c => new CardDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Categories = (from cat in c.Categories
                                  join ct in _db.CategoryTypes
                                  on new { DeckId = c.DeckId, cat.Position }
                                  equals new { ct.DeckId, ct.Position }
                                  select new CategoryDto
                                  {
                                      Id = cat.Id,
                                      CategoryTypeId = ct.Id,
                                      Description = ct.Description,
                                      Score = cat.Score
                                  }).ToList()
                })
                .FirstOrDefaultAsync();
        }



        //GetCategoryTypeAsync(currentRound.CategoryTypeId)
        public async Task<CategoryType> GetCategoryTypeAsync(int? categoryTypeId)
        {
            return await _deckRepo.GetCategoryTypeAsync(categoryTypeId);
        }

        public async Task<OpenGameDto[]> GetOpenGamesAsync(int userId)
        {
            return await _db.Games
                .Where(g => g.StartedAt == null)
                .Select(g => new OpenGameDto
                {
                    GameId = g.Id,

                    DeckTopic = _db.Decks
                        .Where(d => d.Id == g.DeckId)
                        .Select(d => d.Topic!)
                        .FirstOrDefault() ?? string.Empty,

                    IsOwned = g.OwnerUserId == userId,
                    Joined = g.UserGames.Any(ug => ug.UserId == userId),

                    UserNames = g.UserGames
                        .Select(ug => ug.User.InGameName)
                        .ToList()
                })
                .ToArrayAsync();
        }

        public async Task<int> GetTotalCardCountForGameAsync(int gameId)
        {
            return await _db.Games
                .Where(g => g.Id == gameId)
                .Join(
                    _db.Cards,
                    g => g.DeckId,
                    c => c.DeckId,
                    (_, c) => c.Id)
                .CountAsync();
        }
    }
}
