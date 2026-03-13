namespace DeckDuel2.Models
{
    public class Turn
    {

        public int Id { get; set; }
        public int RoundNumber { get; set; }

        public int UserGameId { get; set; }
        public UserGame UserGame { get; set; } = null!; //nav prop

        public int CardId { get; set; }
        public DateTime PlayedAt { get; set; } = DateTime.UtcNow;

    }
}
