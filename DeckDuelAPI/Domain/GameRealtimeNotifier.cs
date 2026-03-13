using DeckDuel2.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace DeckDuel2.Domain
{
    public interface IGameRealtimeNotifier
    {
        Task TurnChangedAsync(int gameId, int currentTurnUserId, int turnNumber);

        Task NotifyGameStartedAsync(int gameId, int currentTurnUserId);
    }

    public class GameRealtimeNotifier : IGameRealtimeNotifier
    {
        private readonly IHubContext<GameHub> _hub;

        public GameRealtimeNotifier(IHubContext<GameHub> hub)
        {
            _hub = hub;
        }

        public Task TurnChangedAsync(int gameId, int currentTurnUserId, int turnNumber) =>
            _hub.Clients.Group($"game-{gameId}")
                .SendAsync("TurnChanged", new { gameId, currentTurnUserId, turnNumber });

        public Task NotifyGameStartedAsync(int gameId, int currentTurnUserId) =>
            _hub.Clients.Group($"game-{gameId}")
                .SendAsync("GameStarted", new { gameId, currentTurnUserId });

    }
}