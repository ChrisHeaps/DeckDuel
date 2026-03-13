namespace DeckDuel2.Models
{
    public class Game
    {
        public int Id { get; set; }

        public int DeckId { get; set; }      

        public int OwnerUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        public int CurrentRoundNumber { get; set; } = 1;
        
        public int? CurrentRoundUserGameId { get; set; }


        public string? DrawPileCardList { get; set; } = string.Empty;

        public int? WinningUserId { get; set; }

        public ICollection<UserGame> UserGames { get; set; } = new List<UserGame>();

        public ICollection<Round> Rounds { get; set; } = new List<Round>();

      
    }

}
