using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddMarkedFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarkedFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    ChallengeId = table.Column<int>(type: "integer", nullable: false),
                    ParticipationId = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    ContainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamName = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ChallengeTitle = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ModelId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Flag = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubmissionId = table.Column<int>(type: "integer", nullable: true),
                    SubmittedByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarkedFlags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarkedFlags_ContainerId",
                table: "MarkedFlags",
                column: "ContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_MarkedFlags_GameId_Flag",
                table: "MarkedFlags",
                columns: new[] { "GameId", "Flag" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarkedFlags");
        }
    }
}
