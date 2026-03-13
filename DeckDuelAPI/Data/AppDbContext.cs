using DeckDuel2.Models;
using Microsoft.EntityFrameworkCore;

namespace DeckDuel2.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users{ get; set; } = null!;
        public DbSet<Deck> Decks { get; set; } = null!;

        public DbSet<Hand> Hands { get; set; } = null!;

        public DbSet<Round> Rounds { get; set; } = null!;

        public DbSet<Turn> Turns { get; set; } = null!;

        public DbSet<Card> Cards { get; set; } = null!;

        public DbSet<Category> Categories { get; set; } = null!;

        public DbSet<CategoryType> CategoryTypes { get; set; } = null!;

        public DbSet<Game> Games { get; set; } = null!;

        public DbSet<UserGame> UserGames { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserGame>()
                .HasOne(ug => ug.User)
                .WithMany(u => u.UserGames)
                .HasForeignKey(ug => ug.UserId);

            modelBuilder.Entity<UserGame>()
                .HasOne(ug => ug.Game)
                .WithMany(g => g.UserGames)
                .HasForeignKey(ug => ug.GameId);

            modelBuilder.Entity<Hand>()
                .HasOne(h => h.UserGame)
                .WithMany(ug => ug.Hands)
                .HasForeignKey(h => h.UserGameId);

            modelBuilder.Entity<Turn>()
                .HasOne(t => t.UserGame)
                .WithMany(ug => ug.Turns)
                .HasForeignKey(t => t.UserGameId);

            modelBuilder.Entity<Round>()
               .HasOne(r => r.Game)
               .WithMany(g => g.Rounds)
               .HasForeignKey(r => r.GameId);

            modelBuilder.Entity<Round>()
                .HasOne(r => r.CategoryType)
                .WithMany()
                .HasForeignKey(r => r.CategoryTypeId);
        }
    }
}