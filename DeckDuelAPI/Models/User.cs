using DeckDuel2.DTOs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DeckDuel2.Models
{
    public class User
    {
        public int Id { get; set; }
        [MaxLength(50)]
        public string Username { get; set; }
        [MaxLength(30)]
        public string InGameName { get; set; }
        public string PasswordHash { get; set; }    
        [MaxLength(254)]
        public string? Email { get; set; }
        public bool IsBot { get; set; } = false;        

        [InverseProperty(nameof(Deck.UserNavigation))]
        public IEnumerable<Deck>? Decks { get; set; }
        public ICollection<UserGame> UserGames { get; set; } = new List<UserGame>();
    }
}
