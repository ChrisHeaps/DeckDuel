using DeckDuel2.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace DeckDuel2.Domain
{

    public class AIService
    {

        private readonly HttpClient _httpClient;

        public AIService(HttpClient client)
        {
            _httpClient = client;
        }


        public async Task<Deck> GenerateDeckAsync(string topic)
        {
            /*
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
      },
      {        
        "name": "Shadowmane Wraith",
        "categories": [
          { "Position": 1, "score": 63 },
          { "Position": 2, "score": 89 },
          { "Position": 3, "score": 95 },
          { "Position": 4, "score": 4 }
        ]
      },
      {        
        "name": "Stonehide Behemoth",
        "categories": [
          { "Position": 1, "score": 98 },
          { "Position": 2, "score": 28 },
          { "Position": 3, "score": 15 },
          { "Position": 4, "score": 12 }
        ]
      },
      {        
        "name": "Silverwing Seraph",
        "categories": [
          { "Position": 1, "score": 74 },
          { "Position": 2, "score": 83 },
          { "Position": 3, "score": 67 },
          { "Position": 4, "score": 3 }
        ]
      },
      {        
        "name": "Bogroot Colossus",
        "categories": [
          { "Position": 1, "score": 91 },
          { "Position": 2, "score": 34 },
          { "Position": 3, "score": 22 },
          { "Position": 4, "score": 15 }
        ]
      },
      {        
        "name": "Frostveil Phoenix",
        "categories": [
          { "Position": 1, "score": 79 },
          { "Position": 2, "score": 88 },
          { "Position": 3, "score": 53 },
          { "Position": 4, "score": 6 }
        ]
      }
    ]
  }
}
            */



            //var request = new HttpRequestMessage(HttpMethod.Get, "https://dummyjson.com/c/e8b1-1d42-485b-9c56");
            var request = new HttpRequestMessage(HttpMethod.Get, "https://dummyjson.com/c/294c-7cc4-4755-a2bf");
                //request.Headers.Add("Authorization", $"Bearer {_apiKey}");
                //request.Content = new StringContent(
                //    JsonSerializer.Serialize(requestBody),
                //    Encoding.UTF8,
                //    "application/json"
                //);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            // Deserialize directly from the JSON string (or use wrapper.RootElement.GetRawText())
            var deck = JsonSerializer.Deserialize<DeckWrapper>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (deck == null)
            {
                throw new InvalidOperationException("Failed to deserialize Deck from response JSON.");
            }

            return deck.Deck;
        }
    }
}
