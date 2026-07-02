using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChainDegree.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFoundationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "StudentCode",
                table: "STUDENTS",
                newName: "IdentityNumber");

            migrationBuilder.RenameIndex(
                name: "IX_STUDENTS_StudentCode",
                table: "STUDENTS",
                newName: "IX_STUDENTS_IdentityNumber");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "STUDENTS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "STUDENTS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "STUDENTS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "REPORTS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "REPORTS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "REPORTS",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "REPORTS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "REGISTRARS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "REGISTRARS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "REGISTRARS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "RECRUITER_AGENTS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "RECRUITER_AGENTS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "RECRUITER_AGENTS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "JOBS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "JOBS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "JOBS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "JOB_DEGREE_FILTERS",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "JOB_DEGREE_FILTERS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "JOB_DEGREE_FILTERS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "JOB_DEGREE_FILTERS",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "JOB_DEGREE_FILTERS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "EDUCATION_INSTITUTIONS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "EDUCATION_INSTITUTIONS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "EDUCATION_INSTITUTIONS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "DEGREES",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "DEGREES",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "DEGREES",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "DEGREES",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "COMPANIES",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "COMPANIES",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "COMPANIES",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "APPLICATIONS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "APPLICATIONS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "APPLICATIONS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "DEGREE_TYPES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEGREE_TYPES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DEGREE_TYPES_EDUCATION_INSTITUTIONS_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "EDUCATION_INSTITUTIONS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "INSTITUTION_STUDENTS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INSTITUTION_STUDENTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_INSTITUTION_STUDENTS_EDUCATION_INSTITUTIONS_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "EDUCATION_INSTITUTIONS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INSTITUTION_STUDENTS_STUDENTS_StudentId",
                        column: x => x.StudentId,
                        principalTable: "STUDENTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OUTBOX_MESSAGES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OccurredOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedOn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OUTBOX_MESSAGES", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_STUDENTS_DeletedAt",
                table: "STUDENTS",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTS_DeletedAt",
                table: "REPORTS",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_REGISTRARS_DeletedAt",
                table: "REGISTRARS",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RECRUITER_AGENTS_DeletedAt",
                table: "RECRUITER_AGENTS",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JOBS_DeletedAt",
                table: "JOBS",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_JOB_DEGREE_FILTERS_DeletedAt",
                table: "JOB_DEGREE_FILTERS",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_EDUCATION_INSTITUTIONS_DeletedAt",
                table: "EDUCATION_INSTITUTIONS",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DEGREES_DeletedAt",
                table: "DEGREES",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_COMPANIES_DeletedAt",
                table: "COMPANIES",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_APPLICATIONS_DeletedAt",
                table: "APPLICATIONS",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DEGREE_TYPES_DeletedAt",
                table: "DEGREE_TYPES",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DEGREE_TYPES_InstitutionId_Code",
                table: "DEGREE_TYPES",
                columns: new[] { "InstitutionId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_STUDENTS_DeletedAt",
                table: "INSTITUTION_STUDENTS",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_STUDENTS_InstitutionId_StudentCode",
                table: "INSTITUTION_STUDENTS",
                columns: new[] { "InstitutionId", "StudentCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_STUDENTS_InstitutionId_StudentId",
                table: "INSTITUTION_STUDENTS",
                columns: new[] { "InstitutionId", "StudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_STUDENTS_StudentId",
                table: "INSTITUTION_STUDENTS",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_OUTBOX_MESSAGES_ProcessedOn",
                table: "OUTBOX_MESSAGES",
                column: "ProcessedOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DEGREE_TYPES");

            migrationBuilder.DropTable(
                name: "INSTITUTION_STUDENTS");

            migrationBuilder.DropTable(
                name: "OUTBOX_MESSAGES");

            migrationBuilder.DropIndex(
                name: "IX_STUDENTS_DeletedAt",
                table: "STUDENTS");

            migrationBuilder.DropIndex(
                name: "IX_REPORTS_DeletedAt",
                table: "REPORTS");

            migrationBuilder.DropIndex(
                name: "IX_REGISTRARS_DeletedAt",
                table: "REGISTRARS");

            migrationBuilder.DropIndex(
                name: "IX_RECRUITER_AGENTS_DeletedAt",
                table: "RECRUITER_AGENTS");

            migrationBuilder.DropIndex(
                name: "IX_JOBS_DeletedAt",
                table: "JOBS");

            migrationBuilder.DropIndex(
                name: "IX_JOB_DEGREE_FILTERS_DeletedAt",
                table: "JOB_DEGREE_FILTERS");

            migrationBuilder.DropIndex(
                name: "IX_EDUCATION_INSTITUTIONS_DeletedAt",
                table: "EDUCATION_INSTITUTIONS");

            migrationBuilder.DropIndex(
                name: "IX_DEGREES_DeletedAt",
                table: "DEGREES");

            migrationBuilder.DropIndex(
                name: "IX_COMPANIES_DeletedAt",
                table: "COMPANIES");

            migrationBuilder.DropIndex(
                name: "IX_APPLICATIONS_DeletedAt",
                table: "APPLICATIONS");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "STUDENTS");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "STUDENTS");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "STUDENTS");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "REPORTS");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "REPORTS");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "REPORTS");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "REPORTS");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "REGISTRARS");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "REGISTRARS");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "REGISTRARS");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "RECRUITER_AGENTS");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "RECRUITER_AGENTS");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "RECRUITER_AGENTS");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "JOBS");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "JOBS");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "JOBS");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "JOB_DEGREE_FILTERS");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "JOB_DEGREE_FILTERS");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "JOB_DEGREE_FILTERS");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "JOB_DEGREE_FILTERS");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "JOB_DEGREE_FILTERS");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EDUCATION_INSTITUTIONS");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "EDUCATION_INSTITUTIONS");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "EDUCATION_INSTITUTIONS");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DEGREES");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DEGREES");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "DEGREES");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DEGREES");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "COMPANIES");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "COMPANIES");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "COMPANIES");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "APPLICATIONS");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "APPLICATIONS");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "APPLICATIONS");

            migrationBuilder.RenameColumn(
                name: "IdentityNumber",
                table: "STUDENTS",
                newName: "StudentCode");

            migrationBuilder.RenameIndex(
                name: "IX_STUDENTS_IdentityNumber",
                table: "STUDENTS",
                newName: "IX_STUDENTS_StudentCode");
        }
    }
}
