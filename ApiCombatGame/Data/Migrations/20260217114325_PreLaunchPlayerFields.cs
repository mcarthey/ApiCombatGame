using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiCombatGame.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreLaunchPlayerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Players",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationToken",
                table: "Players",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "Players",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Players",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PasswordResetExpiresAt",
                table: "Players",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordResetToken",
                table: "Players",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationToken",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PasswordResetExpiresAt",
                table: "Players");

            migrationBuilder.DropColumn(
                name: "PasswordResetToken",
                table: "Players");
        }
    }
}
