namespace DeckDuel2.Models
{
    public class Round
    {

        public int Id { get; set; } 
        public int RoundNumber { get; set; }

        public Game Game { get; set; } 
        public int GameId { get; set; } 
        public CategoryType CategoryType { get; set; } 
        public int? CategoryTypeId { get; set; }

    }
}
