using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddStandingStreaks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PeopleStreak",
                table: "Standings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "PeopleStreakOpponentElo",
                table: "Standings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "RatedStreak",
                table: "Standings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "RatedStreakOpponentElo",
                table: "Standings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Streak",
                table: "Standings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "StreakOpponentElo",
                table: "Standings",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            // The streaks, counted from the games themselves: every standing counted again, as the server rates them.
            migrationBuilder.Sql("DELETE FROM Standings;");
            migrationBuilder.Sql("DELETE FROM BotStandings;");
            migrationBuilder.Sql("UPDATE Games SET IsRated = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PeopleStreak",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "PeopleStreakOpponentElo",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "RatedStreak",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "RatedStreakOpponentElo",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "Streak",
                table: "Standings");

            migrationBuilder.DropColumn(
                name: "StreakOpponentElo",
                table: "Standings");
        }
    }
}
