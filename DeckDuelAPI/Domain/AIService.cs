using Azure;
using Azure.AI.OpenAI;
using DeckDuel2.Configuration;
using DeckDuel2.Models;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

namespace DeckDuel2.Domain
{
    public class AIService
    {
        private readonly ChatClient _chatClient;

        public AIService(IOptions<AzureOpenAIOptions> options)
        {
            var opts = options.Value;
            var azureClient = new AzureOpenAIClient(
                new Uri(opts.Endpoint),
                new AzureKeyCredential(opts.ApiKey));

            try
            {
                _chatClient = azureClient.GetChatClient(opts.DeploymentName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Azure OpenAI client failed.  Message={ex.Message}", ex);
            }
        }

        public async Task<Deck> GenerateDeckAsync(string topic)
        {
            const string systemPrompt = """
                You are a card game deck generator.

                When given a topic, respond with ONLY valid JSON.

                The JSON MUST strictly follow the same structure, formatting, and style as the example below.

                Rules:
                - Generate exactly 4 categoryTypes with positions 1 through 4
                - Generate exactly 20 unique cards
                - Try to use real people and things if possible before inventing names or items
                - Each card must have exactly 4 categories matching the 4 categoryType positions
                - Scores must be integers between 1 and 100
                - Higher should always be better when comparing scores
                - Return ONLY the JSON object, no markdown formatting or explanation
                - Do not include any text outside the JSON
                - Use the exact property names and casing
                - Keep the same structure for categoryTypes and cards
                - Each card must include a score for every category Position
                - Position values must match those defined in categoryTypes
                - Keep consistent formatting                
                - If you cannot generate valid JSON that matches the schema exactly, respond with: {""error"": ""generation_failed""}

                Example format:
                {
                  "deck": {
                    "topic": "Mythic Creatures",
                    "categoryTypes": [
                      {
                        "Position": 1,
                        "description": "Strength",
                        "higherWins": true
                      },
                      {
                        "Position": 2,
                        "description": "Speed",
                        "higherWins": true
                      },
                      {
                        "Position": 3,
                        "description": "Stealth",
                        "higherWins": true
                      },
                      {
                        "Position": 4,
                        "description": "Rarity Rank",
                        "higherWins": false
                      }
                    ],
                    "cards": [
                      {
                        "name": "Emberfang Drake",
                        "categories": [
                          { "Position": 1, "score": 87 },
                          { "Position": 2, "score": 72 },
                          { "Position": 3, "score": 41 },
                          { "Position": 4, "score": 9 }
                        ]
                      }
                    ]
                  }
                }

                """;

            ChatCompletion response;
            try
            {
                response = await _chatClient.CompleteChatAsync(
                    [
                        new SystemChatMessage(systemPrompt),
                        new UserChatMessage($"Generate a deck for the topic: {topic}")
                    ],
                    new ChatCompletionOptions
                    {
                        ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                    });
            }
            catch (ClientResultException ex)
            {
                throw new InvalidOperationException(
                    $"Azure OpenAI call failed. Status={ex.Status}, Message={ex.Message}", ex);
            }

            var json = response.Content[0].Text;

            var wrapper = JsonSerializer.Deserialize<DeckWrapper>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (wrapper?.Deck == null)
                throw new InvalidOperationException("Failed to deserialize Deck from AI response.");

            return wrapper.Deck;
        }
    }
}
