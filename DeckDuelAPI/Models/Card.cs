using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DeckDuel2.Models
{
 
        public class Card
        {
            [Key]
            [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
            public int Id { get; set; }

            [Required]
            public string Name { get; set; }

            [Required]
            [InverseProperty(nameof(Category.CardNavigation))]
            public List<Category> Categories { get; set; } = new();
            
            [Required]
            public int DeckId { get; set; }

            [Required]
            [ForeignKey(nameof(DeckId))]
            [JsonIgnore]
            public Deck DeckNavigation { get; set; }

        }
}
