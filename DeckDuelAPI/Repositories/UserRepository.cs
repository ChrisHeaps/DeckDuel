using DeckDuel2.Data;
using DeckDuel2.DTOs;
using DeckDuel2.Models;
using Microsoft.EntityFrameworkCore;

namespace DeckDuel2.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task AddUserAsync(User user)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        public async Task<User?> GetUserByUsernameAsync(string username)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetUserByInGameNameAsync(string nickName)
        {
            return await _db.Users.FirstOrDefaultAsync(u => u.InGameName == nickName);
        }
    }
}
