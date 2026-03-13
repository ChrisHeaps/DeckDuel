using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DeckDuel2.Hubs
{
    [Authorize]
    public class GameHub : Hub
    {
        public Task JoinGameGroup(int gameId) =>
            Groups.AddToGroupAsync(Context.ConnectionId, $"game-{gameId}");

        public Task LeaveGameGroup(int gameId) =>
            Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game-{gameId}");
    }
}