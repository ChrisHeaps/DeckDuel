namespace DeckDuel2.Messaging
{
    public class TakeTurnMessage
    {
        public int GameId { get; set; }
        public int UserId { get; set; }
        public int UserGameId { get; set; }
        public int CategoryTypeId { get; set; }
    }
}