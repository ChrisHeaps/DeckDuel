using System.ComponentModel.DataAnnotations;

namespace DeckDuel2.DTOs
{
    public class GenerateDeckRequestDto
    {
        [Required]
        [StringLength(30, ErrorMessage = "deckPrompt must be 30 characters or fewer.")]
        public string DeckPrompt { get; set; } = string.Empty;
    }
}   