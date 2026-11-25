using System;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261110120000_AddAiDetection")]
public partial class AddAiDetection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "EnableAiDetection", table: "GameChallenges", type: "boolean",
            nullable: false, defaultValue: false);

        migrationBuilder.AddColumn<Guid>(
            name: "AiDetectionToken", table: "Containers", type: "uuid", nullable: true);

        migrationBuilder.CreateTable(
            name: "AiDetectionEvents",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                GameId = table.Column<int>(type: "integer", nullable: false),
                ChallengeId = table.Column<int>(type: "integer", nullable: false),
                ParticipationId = table.Column<int>(type: "integer", nullable: false),
                TeamId = table.Column<int>(type: "integer", nullable: false),
                ContainerId = table.Column<Guid>(type: "uuid", nullable: false),
                ChallengeTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                TeamName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                ModelId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                SeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_AiDetectionEvents", x => x.Id));

        migrationBuilder.CreateIndex(
            name: "IX_Containers_AiDetectionToken", table: "Containers",
            column: "AiDetectionToken", unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_AiDetectionEvents_ContainerId", table: "AiDetectionEvents",
            column: "ContainerId");
        migrationBuilder.CreateIndex(
            name: "IX_AiDetectionEvents_GameId_SeenAt", table: "AiDetectionEvents",
            columns: new[] { "GameId", "SeenAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "AiDetectionEvents");
        migrationBuilder.DropIndex(name: "IX_Containers_AiDetectionToken", table: "Containers");
        migrationBuilder.DropColumn(name: "AiDetectionToken", table: "Containers");
        migrationBuilder.DropColumn(name: "EnableAiDetection", table: "GameChallenges");
    }
}
