using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.SqlServer.Migrations
{
    /// <summary>
    /// Each standing's Glicko-2 and TrueSkill ratings beside its Elo (the user's word of 2026-11-20: all three, past
    /// games counted at once). The standings go, and every game over is to be rated again, in the order the games
    /// ended, as the server rates them, so that all three are counted from every game kept.
    /// </summary>
    public partial class AddStandingGlickoAndTrueSkill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "GlickoDeviation",
                table: "Standings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GlickoRating",
                table: "Standings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "GlickoVolatility",
                table: "Standings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TrueSkillDeviation",
                table: "Standings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TrueSkillMean",
                table: "Standings",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.Sql("DELETE FROM Standings;");
            migrationBuilder.Sql("DELETE FROM BotStandings;");
            migrationBuilder.Sql("UPDATE Games SET IsRated = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GlickoDeviation",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "GlickoRating",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "GlickoVolatility",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "TrueSkillDeviation",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "TrueSkillMean",
                table: "Standings");
        }
    }
}
