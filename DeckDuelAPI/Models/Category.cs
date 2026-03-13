using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DeckDuel2.Models
{
    public class Category
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        //[ForeignKey(nameof(CategoryType))]

        [Required]
        public int Position { get; set; }

        [Required]
        public int Score { get; set; }
        
        [Required]
        
        public int CardId { get; set; }
        [ForeignKey(nameof(CardId))]
        [JsonIgnore]
        public Card CardNavigation { get; set; } = null!;

    }
}
