using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Sqlite.Migrations
{
    /// <summary>
    /// Morpheus, Phaethon and Tantalus play as Midas more seldom than before (the user, 2026-11-26: at least three
    /// bots below 1200), so their games played to rate them go, for the bot-rating job to play them again as they now
    /// are, rather than mix the old bots' results with the new.
    /// </summary>
    public partial class RerateFaintBots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM BotMatchups WHERE First IN ('morpheus', 'phaethon', 'tantalus') "
                    + "OR Second IN ('morpheus', 'phaethon', 'tantalus');"
            );
        }

        /// <summary>
        /// The games gone cannot come back; but the bots go back to their old shares, so the games they played since,
        /// as they now are, go too, for the bot-rating job to play them again as they were.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DELETE FROM BotMatchups WHERE First IN ('morpheus', 'phaethon', 'tantalus') "
                    + "OR Second IN ('morpheus', 'phaethon', 'tantalus');"
            );
        }
    }
}
