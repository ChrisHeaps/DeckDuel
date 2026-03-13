
namespace DeckDuel2.DTOs
{
    public class GameSummaryDto
    {
        public int Id { get; set; }
        public int DeckId { get; set; }
        public int OwnerUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}