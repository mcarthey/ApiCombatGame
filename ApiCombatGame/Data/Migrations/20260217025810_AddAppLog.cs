using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiCombatGame.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ExceptionDetails = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: true),
                    SupportId = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PlayerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppLogs_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppLogs_CreatedAt",
                table: "AppLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AppLogs_Level",
                table: "AppLogs",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_AppLogs_PlayerId_CreatedAt",
                table: "AppLogs",
                columns: new[] { "PlayerId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppLogs_SupportId",
                table: "AppLogs",
                column: "SupportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLogs");
        }
    }
}
