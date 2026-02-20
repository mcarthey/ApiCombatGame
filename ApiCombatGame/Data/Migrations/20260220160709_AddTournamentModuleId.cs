using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiCombatGame.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTournamentModuleId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ModuleId",
                table: "Tournaments",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModuleId",
                table: "Tournaments");
        }
    }
}
