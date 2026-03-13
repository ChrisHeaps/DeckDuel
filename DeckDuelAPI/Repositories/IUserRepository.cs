using DeckDuel2.DTOs;
using DeckDuel2.Models;
using System.Threading.Tasks;

namespace DeckDuel2.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetUserByUsernameAsync(string user);
        Task<User?> GetUserByInGameNameAsync(string nickName);
        Task AddUserAsync(User user);
    }
}