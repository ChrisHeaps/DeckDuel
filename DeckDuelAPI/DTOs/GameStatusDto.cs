namespace DeckDuel2.DTOs
{
    public class GameStatusDto
    {
        public int GameId { get; set; }
        public string? CurrentRoundCategoryName { get; set; }
        public List<GameStatusPlayerDto> Players { get; set; } = new();

        public bool IsGameOver { get; set; }
        public int? WinningUserGameId { get; set; }
        public string? WinningUserInGameName { get; set; }
    }

    public class GameStatusPlayerDto
    {
        public string InGameName { get; set; } = string.Empty;
        public int HandCardCount { get; set; }
        public int? CurrentTurnScore { get; set; }
    }
}