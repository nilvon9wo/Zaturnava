using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddBotMatchups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BotMatchups",
                columns: table => new
                {
                    Ruleset = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    First = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Second = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Won = table.Column<int>(type: "INTEGER", nullable: false),
                    Lost = table.Column<int>(type: "INTEGER", nullable: false),
                    Drawn = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BotMatchups", x => new { x.Ruleset, x.First, x.Second });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BotMatchups");
        }
    }
}
