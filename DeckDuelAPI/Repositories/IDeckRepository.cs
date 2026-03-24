using DeckDuel2.DTOs;
using DeckDuel2.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeckDuel2.Repositories
{
    public interface IDeckRepository
    {     

        Task<DeckDto[]> GetDeckNamesAsync(int userID);

        Task<CardDto[]> GetDeckCardsAsync(int deckId, int? userId);

        Task<Card> GetCardAsync(int cardId);

        Task<CategoryType> GetCategoryTypeAsync(int? categoryTypeId);
    }
}
