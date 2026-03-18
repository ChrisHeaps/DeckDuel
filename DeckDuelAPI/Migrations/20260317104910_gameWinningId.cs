using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeckDuel2.Migrations
{
    /// <inheritdoc />
    public partial class gameWinningId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WinningUserId",
                table: "Games",
                newName: "WinningUserGameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WinningUserGameId",
                table: "Games",
                newName: "WinningUserId");
        }
    }
}
