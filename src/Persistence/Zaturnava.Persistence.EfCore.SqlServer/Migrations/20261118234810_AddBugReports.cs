using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddBugReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BugReports",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ReporterId = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    GameId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RulesetId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    Actions = table.Column<int>(type: "int", nullable: false),
                    Deciding = table.Column<int>(type: "int", nullable: true),
                    IsOver = table.Column<bool>(type: "bit", nullable: false),
                    Seat = table.Column<int>(type: "int", nullable: true),
                    ViewVersion = table.Column<int>(type: "int", nullable: true),
                    SelectedCell = table.Column<int>(type: "int", nullable: true),
                    InspectedPiece = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    At = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BugReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_At",
                table: "BugReports",
                column: "At");

            migrationBuilder.CreateIndex(
                name: "IX_BugReports_GameId",
                table: "BugReports",
                column: "GameId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BugReports");
        }
    }
}
