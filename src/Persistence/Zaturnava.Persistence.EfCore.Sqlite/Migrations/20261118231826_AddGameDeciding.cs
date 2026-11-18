using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddGameDeciding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Deciding",
                table: "Games",
                type: "INTEGER",
                nullable: true);

            // Every game not over summarized again at start-up, so that who decides next is worked out for each.
            migrationBuilder.Sql("UPDATE Games SET IsOver = NULL WHERE IsOver = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deciding",
                table: "Games");
        }
    }
}
