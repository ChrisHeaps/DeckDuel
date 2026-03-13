namespace DeckDuel2.Models
{
    public class Hand
    {
        public int Id { get; set; }

        public int UserGameId { get; set; }
        public UserGame UserGame { get; set; }

        public int RoundNumber { get; set; }

        public string CardList { get; set; } = string.Empty;
    }

}
