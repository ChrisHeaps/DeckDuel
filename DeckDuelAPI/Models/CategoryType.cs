using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DeckDuel2.Models
{
    
    public class CategoryType
    {       
       
        public int Id { get; set; }

        public int Position { get; set; }

        public string Description { get; set; }

        public bool HigherWins { get; set; } = true;
        
        [Required]
        public int DeckId { get; set; }
        [JsonIgnore]
        [ForeignKey(nameof(DeckId))]
        public Deck? DeckNavigation { get; set; }

    }
}
