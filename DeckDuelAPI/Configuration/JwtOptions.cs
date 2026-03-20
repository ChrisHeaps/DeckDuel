namespace DeckDuel2.Configuration
{
    public sealed class JwtOptions
    {
        public const string SectionName = "Jwt";

        public string SigningKey { get; set; } = string.Empty;
    }
}