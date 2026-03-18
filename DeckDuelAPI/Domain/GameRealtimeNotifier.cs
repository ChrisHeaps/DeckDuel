using DeckDuel2.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DeckDuel2.Domain
{
    public interface IGameRealtimeNotifier
    {
        Task NotifyTurnChangedAsync(int gameId, int? currentTurnUserId, int turnNumber);
        Task NotifyGameOpenedAsync(int gameId);
        Task NotifyGameStartedAsync(int gameId);
        Task NotifyGameJoinedAsync(int gameId);
        Task NotifyGameFinishedAsync(int gameId);
        //Task NotifyRoundCompletedAsync(int gameId, int winnerUserGameId, int currentRoundNumber);
    }

    public class GameRealtimeNotifier : IGameRealtimeNotifier
    {
        private readonly IHubContext<GameHub> _hub;

        public GameRealtimeNotifier(IHubContext<GameHub> hub)
        {
            _hub = hub;
        }

        public Task NotifyGameOpenedAsync(int gameId) =>
            _hub.Clients.All.SendAsync("GameOpened", new { gameId });

        public Task NotifyGameJoinedAsync(int gameId) =>
            _hub.Clients.All.SendAsync("GameJoined", new { gameId });

        public Task NotifyTurnChangedAsync(int gameId, int? currentTurnUserId, int turnNumber) =>
            _hub.Clients.Group($"game-{gameId}")
                .SendAsync("TurnChanged", new { gameId, currentTurnUserId, turnNumber });

        public Task NotifyGameStartedAsync(int gameId) =>
            _hub.Clients.All.SendAsync("GameStarted", new { gameId });

        public Task NotifyGameFinishedAsync(int gameId) =>
            _hub.Clients.All.SendAsync("GameFinished", new { gameId });

        //public Task NotifyRoundCompletedAsync(int gameId, int winnerUserGameId, int currentRoundNumber) =>
        //    _hub.Clients.Group($"game-{gameId}")
        //        .SendAsync("RoundCompleted", new { gameId, winnerUserGameId, currentRoundNumber });
    }
}