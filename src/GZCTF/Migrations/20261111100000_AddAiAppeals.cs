using System;
using GZCTF.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GZCTF.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261111100000_AddAiAppeals")]
public partial class AddAiAppeals : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>("AiBanAt", "Participations", type: "timestamp with time zone", nullable: true);
        migrationBuilder.AddColumn<Guid>("AiBanEventId", "Participations", type: "uuid", nullable: true);
        migrationBuilder.AddColumn<int>("AiAppealId", "Participations", type: "integer", nullable: true);
        migrationBuilder.CreateIndex("IX_Participations_AiAppealId", "Participations", "AiAppealId");
        migrationBuilder.AddForeignKey(
            name: "FK_Participations_LocalFiles_AiAppealId",
            table: "Participations",
            column: "AiAppealId",
            principalTable: "Files",
            principalColumn: "Id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey("FK_Participations_LocalFiles_AiAppealId", "Participations");
        migrationBuilder.DropIndex("IX_Participations_AiAppealId", "Participations");
        migrationBuilder.DropColumn("AiAppealId", "Participations");
        migrationBuilder.DropColumn("AiBanAt", "Participations");
        migrationBuilder.DropColumn("AiBanEventId", "Participations");
    }
}
