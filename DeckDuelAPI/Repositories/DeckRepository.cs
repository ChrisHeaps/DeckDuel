using DeckDuel2.Data;
using DeckDuel2.DTOs;
using DeckDuel2.Models;
using Microsoft.EntityFrameworkCore;

namespace DeckDuel2.Repositories
{
    public class DeckRepository : IDeckRepository
    {
        private readonly AppDbContext _db;

        public DeckRepository(AppDbContext db)
        {
            _db = db;
        }

        //public async Task<IEnumerable<Deck>> GetDeckNamesAsync()
        //{
        //    return await _db.Decks.ToListAsync();
        //}

        //public async Task<IEnumerable<Card>> GetDeckCardsAsync(int deckId)
        //{
        //    return await _db.Cards.Where(c => c.DeckId == deckId).ToListAsync();
        //}

        public async Task<DeckDto[]> GetSharedDeckNamesAsync()
        {
            return await _db.Decks
                .Where(u => u.UserId == 1)
                .Select(d => new DeckDto
                {
                    Id = d.Id,
                    Topic = d.Topic
                })
                .ToArrayAsync();
        }

        public async Task<DeckDto[]> GetDeckNamesAsync(int userId)
        {
            return await _db.Decks
                .Select(d => new DeckDto
                {
                    Id = d.Id,
                    Topic = d.Topic,
                    IsOwned = d.UserId == userId
                })
                .ToArrayAsync();
        }

        public async Task<Card> GetCardAsync(int cardId)
        {
            return await _db.Cards
                .Include(c => c.Categories)
                .FirstOrDefaultAsync(c => c.Id == cardId);
        }

        public async Task<CategoryType> GetCategoryTypeAsync(int? categoryTypeId)
        {
            return await _db.CategoryTypes.FindAsync(categoryTypeId);
        }

        public async Task<CardDto[]> GetDeckCardsAsync(int deckId, int? userId)
        {
            bool deckExistsForUser = false;
            if (userId != null)
            {
                // Ensure the deck belongs to the user
                deckExistsForUser = await _db.Decks
                    .AnyAsync(d => d.Id == deckId && d.UserId == userId);
            }
            else
            {
                deckExistsForUser = true;
            }


            if (!deckExistsForUser)
            {
                // Option: throw an exception (e.g., UnauthorizedAccessException or custom)
                // or return an empty array as shown below.
                return Array.Empty<CardDto>();
            }

            return await _db.Cards
                .Where(cd => cd.DeckId == deckId)
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
                                      Description = ct.Description,
                                      Score = cat.Score
                                  }).ToList()
                })
                .ToArrayAsync();
        }
    }
}