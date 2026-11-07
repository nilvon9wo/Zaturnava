using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddBotStandings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DrawnAgainstBots",
                table: "Standings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LostAgainstBots",
                table: "Standings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WonAgainstBots",
                table: "Standings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "BotStandings",
                columns: table => new
                {
                    BotId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Game = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    Won = table.Column<int>(type: "INTEGER", nullable: false),
                    Lost = table.Column<int>(type: "INTEGER", nullable: false),
                    Drawn = table.Column<int>(type: "INTEGER", nullable: false),
                    WonAgainstPeople = table.Column<int>(type: "INTEGER", nullable: false),
                    LostAgainstPeople = table.Column<int>(type: "INTEGER", nullable: false),
                    DrawnAgainstPeople = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotStandings", x => new { x.BotId, x.Game });
                });

            migrationBuilder.CreateIndex(
                name: "IX_BotStandings_Game_Won",
                table: "BotStandings",
                columns: new[] { "Game", "Won" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotStandings");

            migrationBuilder.DropColumn(
                name: "DrawnAgainstBots",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "LostAgainstBots",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "WonAgainstBots",
                table: "Standings");
        }
    }
}
