namespace DeckDuel2.Configuration
{
    public sealed class AzureOpenAIOptions
    {
        public const string SectionName = "AzureOpenAI";

        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string DeploymentName { get; set; } = string.Empty;
    }
}