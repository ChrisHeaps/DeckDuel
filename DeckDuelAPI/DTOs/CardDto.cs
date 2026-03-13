using DeckDuel2.Models;
using System.Collections.Generic;

namespace DeckDuel2.DTOs
{
    public class CardDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<CategoryDto> Categories { get; set; } = new();
    }
}
