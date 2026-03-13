using DeckDuel2.DTOs;
using DeckDuel2.Models;

namespace DeckDuel2.Repositories
{
    public interface IGameRepository
    {
        Task<UserGameDto> CreateGameAsync(int deckId, int userId);

        Task<Game?> GetGameAsync(int gameId);
        Task<int> GetPlayerCountAsync(int gameId);
        Task<int> GetBotUserIdAsync();
        Task AddUserToGameAsync(int gameId, int userId);
        Task<UserGame?> GetUserGameAsync(int gameId, int UserId);
        Task<UserGame[]> GetUserGamesAsync(int gameId);
        Task<UserGame[]> GetUserGamesForUserAsync(int userId);
        Task CreateHandAsync(int gameId, int roundNum, int userId, string cardList);
        Task SaveChangesAsync();

        Task<CardDto[]> GetDeckCardsForGameAsync(int deckId);

        bool IsGameOwner(int gameId, int userId);

        Task<ActiveGameDto[]> GetActiveGamesAsync(int userId);
        Task<OpenGameDto[]> GetOpenGamesAsync(int userId);

        Task<Round?> GetRoundAsync(int gameId, int roundId);
        Task<Round> AddRoundAsync(Round round);
        Task<Turn> AddTurnAsync(Turn turn);
        Task<Hand?> GetHandAsync(int userGameId, int roundNum);
        Task<Turn[]> GetTurnsForRoundAsync(int gameId, int roundNum);
        Task<int[]> GetUsersStillInGameAsync(int gameId, int roundNum);
        Task<Card> GetCardAsync(int cardId);
        Task<CategoryType> GetCategoryTypeAsync(int? categoryTypeId);

        Task<UserGame?> GetUserGameByIdAsync(int userGameId);
        Task<Hand?> GetLatestHandAsync(int userGameId);
        Task<CardDto?> GetCardDetailsDtoAsync(int cardId);
    }
}


