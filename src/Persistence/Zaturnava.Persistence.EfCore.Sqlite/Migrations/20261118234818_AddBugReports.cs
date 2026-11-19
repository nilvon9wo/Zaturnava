using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Sqlite.Migrations
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
                    Id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ReporterId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    GameId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RulesetId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Actions = table.Column<int>(type: "INTEGER", nullable: false),
                    Deciding = table.Column<int>(type: "INTEGER", nullable: true),
                    IsOver = table.Column<bool>(type: "INTEGER", nullable: false),
                    Seat = table.Column<int>(type: "INTEGER", nullable: true),
                    ViewVersion = table.Column<int>(type: "INTEGER", nullable: true),
                    SelectedCell = table.Column<int>(type: "INTEGER", nullable: true),
                    InspectedPiece = table.Column<int>(type: "INTEGER", nullable: true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    At = table.Column<DateTime>(type: "TEXT", nullable: false)
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
