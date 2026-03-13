using DeckDuel2.DTOs;
using DeckDuel2.Models;
using DeckDuel2.Repositories;
using DeckDuel2.Data;

namespace DeckDuel2.Domain
{
    public interface IDeckService
    {
        Task<DDResult<Deck>> GenerateDeckAsync(string topic, int userId);
        Task<DDResult<DeckDto[]>> GetDecksAsync(int userId);
        Task<DDResult<CardDto[]>> GetDeckCardsAsync(int deckId, int userId);
    }

    public class DeckService : IDeckService
    {
        private readonly IDeckRepository _deckRepo;
        private readonly AIService _aiService;
        private readonly AppDbContext _db;

        public DeckService(IDeckRepository deckRepo, AIService aiService, AppDbContext db)
        {
            _deckRepo = deckRepo;
            _aiService = aiService;
            _db = db;
        }

        public async Task<DDResult<Deck>> GenerateDeckAsync(string topic, int userId)
        {
            if (string.IsNullOrWhiteSpace(topic))
                return DDResult<Deck>.Fail(DDError.InvalidInput, "Topic is required.");

            Deck generated;
            try
            {
                generated = await _aiService.GenerateDeckAsync(topic);
            }
            catch (Exception ex)
            {
                return DDResult<Deck>.Fail(DDError.InvalidInput, $"AI deck generation failed: {ex.Message}");
            }

            generated.UserId = userId;
            _db.Decks.Add(generated);
            await _db.SaveChangesAsync();

            return DDResult<Deck>.Ok(generated);
        }

        public async Task<DDResult<DeckDto[]>> GetDecksAsync(int userId)
        {
            var decks = await _deckRepo.GetDeckNamesAsync(userId);
            return DDResult<DeckDto[]>.Ok(decks);
        }

        public async Task<DDResult<CardDto[]>> GetDeckCardsAsync(int deckId, int userId)
        {
            var cards = await _deckRepo.GetDeckCardsAsync(deckId, userId);
            return DDResult<CardDto[]>.Ok(cards);
        }
    }
}