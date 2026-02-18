using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiCombatGame.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailConfirmationExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmailConfirmationExpiresAt",
                table: "Players",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmationExpiresAt",
                table: "Players");
        }
    }
}
