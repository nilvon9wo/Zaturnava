using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddStandings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRated",
                table: "Games",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Standings",
                columns: table => new
                {
                    PlayerId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Game = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Elo = table.Column<double>(type: "float", nullable: false),
                    Experience = table.Column<int>(type: "int", nullable: false),
                    Won = table.Column<int>(type: "int", nullable: false),
                    Lost = table.Column<int>(type: "int", nullable: false),
                    Drawn = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Standings", x => new { x.PlayerId, x.Game });
                    table.ForeignKey(
                        name: "FK_Standings_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Standings_Game_Elo",
                table: "Standings",
                columns: new[] { "Game", "Elo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Standings");

            migrationBuilder.DropColumn(
                name: "IsRated",
                table: "Games");
        }
    }
}
