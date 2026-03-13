namespace DeckDuel2.DTOs
{
    public class OpenGameDto
    {
        public int GameId { get; set; }
        public string DeckTopic { get; set; } = string.Empty;
        public bool IsOwned { get; set; }
        public bool Joined { get; set; }
        public List<string> UserNames { get; set; } = new List<string>();

    }
}
