using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.SqlServer.Migrations
{
    /// <summary>
    /// Every standing counted again from the games themselves (Stated, 2026-11-12), now that a win's experience
    /// rewards carrying a team, not being carried by it: the standings go, and every game over is to be rated again,
    /// in the order the games ended, as the server rates them.
    /// </summary>
    public partial class RecountExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Standings;");
            migrationBuilder.Sql("DELETE FROM BotStandings;");
            migrationBuilder.Sql("UPDATE Games SET IsRated = 0;");
        }

        /// <summary>
        /// The standings as they were cannot come back; the games over are taken as rated again, the standings left as
        /// far as they were counted.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Games SET IsRated = 1 WHERE IsOver = 1;");
        }
    }
}
