namespace DeckDuel2.DTOs
{
    public class ActiveGameDto    
    {
        public int UserGameId { get; set; }
        public string DeckTopic { get; set; } = string.Empty;
        public bool IsOwned { get; set; }
        public bool MyTurn { get; set; }               
        public List<string> UserNames { get; set; } = new List<string>();

    }
}


