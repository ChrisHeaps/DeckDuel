namespace DeckDuel2.Models
{
    public class UserGame
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int GameId { get; set; }
        public Game Game { get; set; }

        public ICollection<Hand> Hands { get; set; } = new List<Hand>(); 

        public ICollection<Turn> Turns { get; set; } = new List<Turn>(); 
    }

}
