namespace DeckDuel2.DTOs
{
    public class TurnCardDto
    {
        public int GameId { get; set; }
        public int CardId { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<CategoryDto> Categories { get; set; } = new();
        public bool MyTurn { get; set; }
    }
}
