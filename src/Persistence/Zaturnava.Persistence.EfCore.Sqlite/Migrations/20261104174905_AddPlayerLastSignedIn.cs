using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Net.NowhereAtAll.Zaturnava.Persistence.EfCore.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerLastSignedIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastSignedIn",
                table: "Players",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastSignedIn",
                table: "Players");
        }
    }
}
