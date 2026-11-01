using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddGameSummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOver",
                table: "Games",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastChanged",
                table: "Games",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Games_LastChanged",
                table: "Games",
                column: "LastChanged");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Games_LastChanged",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "IsOver",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "LastChanged",
                table: "Games");
        }
    }
}
