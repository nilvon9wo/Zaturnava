using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStandingAgainstBots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DrawnAgainstBots",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LostAgainstBots",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WonAgainstBots",
                table: "Standings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
