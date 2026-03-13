using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DeckDuel2.Models
{
 
        [NotMapped]
        public class DeckWrapper
        {
            public Deck Deck { get; set; } = null!;
        }

        public class Deck
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            [JsonPropertyName("DeckId")]
            public int Id { get; set; }

            public string Topic { get; set; } = string.Empty;

        public List<CategoryType>? CategoryTypes { get; set; }

            [InverseProperty(nameof(Card.DeckNavigation))]
            public IEnumerable<Card>? Cards { get; set; }

            [Required]       
            public int UserId { get; set; }
            
            [ForeignKey(nameof(UserId))]
            [JsonIgnore]
            public User? UserNavigation { get; set; }

    }
}
