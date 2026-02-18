using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiCombatGame.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLedgerEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LedgerEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Property = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    OldValue = table.Column<long>(type: "bigint", nullable: false),
                    NewValue = table.Column<long>(type: "bigint", nullable: false),
                    Delta = table.Column<long>(type: "bigint", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ContextJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_CreatedAt",
                table: "LedgerEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_EntityId_CreatedAt",
                table: "LedgerEntries",
                columns: new[] { "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_EntityId_Property_CreatedAt",
                table: "LedgerEntries",
                columns: new[] { "EntityId", "Property", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_RelatedEntityId",
                table: "LedgerEntries",
                column: "RelatedEntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LedgerEntries");
        }
    }
}
