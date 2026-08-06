using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GZCTF.Migrations
{
    /// <inheritdoc />
    public partial class AddPoolChallenge : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseInstances_ExerciseChallenges_ExerciseId",
                table: "ExerciseInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_FlagContexts_ExerciseChallenges_ExerciseId",
                table: "FlagContexts");

            migrationBuilder.DropTable(
                name: "ExerciseDependencies");

            migrationBuilder.DropTable(
                name: "ExerciseChallenges");

            migrationBuilder.RenameColumn(
                name: "ExerciseId",
                table: "FlagContexts",
                newName: "PoolChallengeId");

            migrationBuilder.RenameIndex(
                name: "IX_FlagContexts_ExerciseId",
                table: "FlagContexts",
                newName: "IX_FlagContexts_PoolChallengeId");

            migrationBuilder.AddColumn<int>(
                name: "PoolChallengeId",
                table: "GameChallenges",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PoolChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Difficulty = table.Column<byte>(type: "smallint", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    Note = table.Column<string>(type: "text", nullable: true),
                    RangeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RangeScore = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<byte>(type: "smallint", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false),
                    Hints = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    DeadlineUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubmissionLimit = table.Column<int>(type: "integer", nullable: false),
                    ContainerImage = table.Column<string>(type: "text", nullable: true),
                    MemoryLimit = table.Column<int>(type: "integer", nullable: true),
                    StorageLimit = table.Column<int>(type: "integer", nullable: true),
                    CPUCount = table.Column<int>(type: "integer", nullable: true),
                    ExposePort = table.Column<int>(type: "integer", nullable: true),
                    NetworkMode = table.Column<byte>(type: "smallint", nullable: true, defaultValue: (byte)0),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    FlagTemplate = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    AttachmentId = table.Column<int>(type: "integer", nullable: true),
                    TestContainerId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PoolChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PoolChallenges_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PoolChallenges_Containers_TestContainerId",
                        column: x => x.TestContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseSubmissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Answer = table.Column<string>(type: "character varying(127)", maxLength: 127, nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SubmitTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExerciseId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseSubmissions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseSubmissions_PoolChallenges_ExerciseId",
                        column: x => x.ExerciseId,
                        principalTable: "PoolChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameChallenges_PoolChallengeId",
                table: "GameChallenges",
                column: "PoolChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubmissions_ExerciseId",
                table: "ExerciseSubmissions",
                column: "ExerciseId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubmissions_UserId",
                table: "ExerciseSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseSubmissions_UserId_ExerciseId",
                table: "ExerciseSubmissions",
                columns: new[] { "UserId", "ExerciseId" });

            migrationBuilder.CreateIndex(
                name: "IX_PoolChallenges_AttachmentId",
                table: "PoolChallenges",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PoolChallenges_RangeEnabled",
                table: "PoolChallenges",
                column: "RangeEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_PoolChallenges_TestContainerId",
                table: "PoolChallenges",
                column: "TestContainerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseInstances_PoolChallenges_ExerciseId",
                table: "ExerciseInstances",
                column: "ExerciseId",
                principalTable: "PoolChallenges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FlagContexts_PoolChallenges_PoolChallengeId",
                table: "FlagContexts",
                column: "PoolChallengeId",
                principalTable: "PoolChallenges",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_GameChallenges_PoolChallenges_PoolChallengeId",
                table: "GameChallenges",
                column: "PoolChallengeId",
                principalTable: "PoolChallenges",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExerciseInstances_PoolChallenges_ExerciseId",
                table: "ExerciseInstances");

            migrationBuilder.DropForeignKey(
                name: "FK_FlagContexts_PoolChallenges_PoolChallengeId",
                table: "FlagContexts");

            migrationBuilder.DropForeignKey(
                name: "FK_GameChallenges_PoolChallenges_PoolChallengeId",
                table: "GameChallenges");

            migrationBuilder.DropTable(
                name: "ExerciseSubmissions");

            migrationBuilder.DropTable(
                name: "PoolChallenges");

            migrationBuilder.DropIndex(
                name: "IX_GameChallenges_PoolChallengeId",
                table: "GameChallenges");

            migrationBuilder.DropColumn(
                name: "PoolChallengeId",
                table: "GameChallenges");

            migrationBuilder.RenameColumn(
                name: "PoolChallengeId",
                table: "FlagContexts",
                newName: "ExerciseId");

            migrationBuilder.RenameIndex(
                name: "IX_FlagContexts_PoolChallengeId",
                table: "FlagContexts",
                newName: "IX_FlagContexts_ExerciseId");

            migrationBuilder.CreateTable(
                name: "ExerciseChallenges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttachmentId = table.Column<int>(type: "integer", nullable: true),
                    TestContainerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CPUCount = table.Column<int>(type: "integer", nullable: true),
                    Category = table.Column<byte>(type: "smallint", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    ContainerImage = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Credit = table.Column<bool>(type: "boolean", nullable: false),
                    DeadlineUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Difficulty = table.Column<byte>(type: "smallint", nullable: false),
                    ExposePort = table.Column<int>(type: "integer", nullable: true),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FlagTemplate = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Hints = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MemoryLimit = table.Column<int>(type: "integer", nullable: true),
                    NetworkMode = table.Column<byte>(type: "smallint", nullable: true, defaultValue: (byte)0),
                    StorageLimit = table.Column<int>(type: "integer", nullable: true),
                    SubmissionLimit = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<byte>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExerciseChallenges_Attachments_AttachmentId",
                        column: x => x.AttachmentId,
                        principalTable: "Attachments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ExerciseChallenges_Containers_TestContainerId",
                        column: x => x.TestContainerId,
                        principalTable: "Containers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ExerciseDependencies",
                columns: table => new
                {
                    SourceId = table.Column<int>(type: "integer", nullable: false),
                    TargetId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExerciseDependencies", x => new { x.SourceId, x.TargetId });
                    table.ForeignKey(
                        name: "FK_ExerciseDependencies_ExerciseChallenges_SourceId",
                        column: x => x.SourceId,
                        principalTable: "ExerciseChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExerciseDependencies_ExerciseChallenges_TargetId",
                        column: x => x.TargetId,
                        principalTable: "ExerciseChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseChallenges_AttachmentId",
                table: "ExerciseChallenges",
                column: "AttachmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseChallenges_TestContainerId",
                table: "ExerciseChallenges",
                column: "TestContainerId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseDependencies_SourceId",
                table: "ExerciseDependencies",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_ExerciseDependencies_TargetId",
                table: "ExerciseDependencies",
                column: "TargetId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExerciseInstances_ExerciseChallenges_ExerciseId",
                table: "ExerciseInstances",
                column: "ExerciseId",
                principalTable: "ExerciseChallenges",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FlagContexts_ExerciseChallenges_ExerciseId",
                table: "FlagContexts",
                column: "ExerciseId",
                principalTable: "ExerciseChallenges",
                principalColumn: "Id");
        }
    }
}
