using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChainDegree.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecruitmentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_APPLICATIONS_StudentId",
                table: "APPLICATIONS");

            migrationBuilder.DropColumn(
                name: "EvidenceFileUrl",
                table: "REPORTS");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "REPORTS",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "EvidenceFileName",
                table: "REPORTS",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "REPORTS",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DegreeId",
                table: "APPLICATIONS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_REPORTS_ReporterId_TargetDegreeId_Status",
                table: "REPORTS",
                columns: new[] { "ReporterId", "TargetDegreeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_APPLICATIONS_StudentId_JobId",
                table: "APPLICATIONS",
                columns: new[] { "StudentId", "JobId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_REPORTS_ReporterId_TargetDegreeId_Status",
                table: "REPORTS");

            migrationBuilder.DropIndex(
                name: "IX_APPLICATIONS_StudentId_JobId",
                table: "APPLICATIONS");

            migrationBuilder.DropColumn(
                name: "EvidenceFileName",
                table: "REPORTS");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "REPORTS");

            migrationBuilder.DropColumn(
                name: "DegreeId",
                table: "APPLICATIONS");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "REPORTS",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceFileUrl",
                table: "REPORTS",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_APPLICATIONS_StudentId",
                table: "APPLICATIONS",
                column: "StudentId");
        }
    }
}
