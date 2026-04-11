using System.Collections.Concurrent;

namespace DeckDuel2.Domain
{
    public sealed class GameTurnLockProvider
    {
        private readonly ConcurrentDictionary<int, SemaphoreSlim> _locks = new();

        public async Task<T> RunAsync<T>(int gameId, Func<Task<T>> action)
        {
            var gate = _locks.GetOrAdd(gameId, static _ => new SemaphoreSlim(1, 1));

            await gate.WaitAsync();

            try
            {
                return await action();
            }
            finally
            {
                gate.Release();
            }
        }
    }
}