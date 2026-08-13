using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddExerciseMonitoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExerciseCheatInfo",
                columns: table => new
                {
                    ExerciseSubmissionId = table.Column<int>(type: "integer", nullable: false),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    SubmitUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseCheatInfo", x => x.ExerciseSubmissionId);
                    table.ForeignKey(
                        name: "FK_ExerciseCheatInfo_AspNetUsers_SourceUserId",
                        column: x => x.SourceUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExerciseCheatInfo_AspNetUsers_SubmitUserId",
                        column: x => x.SubmitUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExerciseCheatInfo_ExerciseSubmissions_ExerciseSubmissionId",
                        column: x => x.ExerciseSubmissionId,
                        principalTable: "ExerciseSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseCheatInfo_PoolChallenges_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "PoolChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PublishTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Values = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseEvents_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExerciseEvents_PoolChallenges_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "PoolChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseCheatInfo_ExerciseId",
                table: "ExerciseCheatInfo",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseCheatInfo_ExerciseSubmissionId",
                table: "ExerciseCheatInfo",
                column: "ExerciseSubmissionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseCheatInfo_SourceUserId",
                table: "ExerciseCheatInfo",
                column: "SourceUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseCheatInfo_SubmitUserId",
                table: "ExerciseCheatInfo",
                column: "SubmitUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseEvents_ExerciseId",
                table: "ExerciseEvents",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseEvents_PublishTimeUtc",
                table: "ExerciseEvents",
                column: "PublishTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseEvents_UserId",
                table: "ExerciseEvents",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExerciseCheatInfo");

            migrationBuilder.DropTable(
                name: "ExerciseEvents");
        }
    }
}
