using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChainDegree.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AUTH_USERS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUTH_USERS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BEHAVIOR_LOGS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetTable = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BEHAVIOR_LOGS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "COMPANIES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    BusinessLicenseCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsVerified = table.Column<bool>(type: "bit", nullable: false),
                    CompanyStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_COMPANIES", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EDUCATION_INSTITUTIONS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EDUCATION_INSTITUTIONS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "STUDENTS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STUDENTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_STUDENTS_AUTH_USERS_UserId",
                        column: x => x.UserId,
                        principalTable: "AUTH_USERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RECRUITER_AGENTS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RECRUITER_AGENTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RECRUITER_AGENTS_AUTH_USERS_UserId",
                        column: x => x.UserId,
                        principalTable: "AUTH_USERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RECRUITER_AGENTS_COMPANIES_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "COMPANIES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "REGISTRARS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REGISTRARS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_REGISTRARS_AUTH_USERS_UserId",
                        column: x => x.UserId,
                        principalTable: "AUTH_USERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_REGISTRARS_EDUCATION_INSTITUTIONS_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "EDUCATION_INSTITUTIONS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JOBS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByAgentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartnerUniversityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SalaryMin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SalaryMax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ApplicationStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApplicationEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JOBS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JOBS_COMPANIES_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "COMPANIES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JOBS_EDUCATION_INSTITUTIONS_PartnerUniversityId",
                        column: x => x.PartnerUniversityId,
                        principalTable: "EDUCATION_INSTITUTIONS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JOBS_RECRUITER_AGENTS_CreatedByAgentId",
                        column: x => x.CreatedByAgentId,
                        principalTable: "RECRUITER_AGENTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DEGREES",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DegreeCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignedByRegistrarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Major = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PlainDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Salt = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DataHashLocal = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TxHashBlockchain = table.Column<string>(type: "nvarchar(66)", maxLength: 66, nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEGREES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DEGREES_EDUCATION_INSTITUTIONS_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "EDUCATION_INSTITUTIONS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DEGREES_REGISTRARS_SignedByRegistrarId",
                        column: x => x.SignedByRegistrarId,
                        principalTable: "REGISTRARS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DEGREES_STUDENTS_StudentId",
                        column: x => x.StudentId,
                        principalTable: "STUDENTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "APPLICATIONS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RankStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProcessStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsForceSubmitted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APPLICATIONS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_APPLICATIONS_JOBS_JobId",
                        column: x => x.JobId,
                        principalTable: "JOBS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_APPLICATIONS_STUDENTS_StudentId",
                        column: x => x.StudentId,
                        principalTable: "STUDENTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JOB_DEGREE_FILTERS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DegreeType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequiredMajor = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MinClassification = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JOB_DEGREE_FILTERS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JOB_DEGREE_FILTERS_JOBS_JobId",
                        column: x => x.JobId,
                        principalTable: "JOBS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "REPORTS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetDegreeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReporterRole = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReportType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvidenceFileUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REPORTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_REPORTS_DEGREES_TargetDegreeId",
                        column: x => x.TargetDegreeId,
                        principalTable: "DEGREES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "APPLICATION_ATTACHED_DEGREES",
                columns: table => new
                {
                    ApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DegreeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APPLICATION_ATTACHED_DEGREES", x => new { x.ApplicationId, x.DegreeId });
                    table.ForeignKey(
                        name: "FK_APPLICATION_ATTACHED_DEGREES_APPLICATIONS_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "APPLICATIONS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_APPLICATION_ATTACHED_DEGREES_DEGREES_DegreeId",
                        column: x => x.DegreeId,
                        principalTable: "DEGREES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_APPLICATION_ATTACHED_DEGREES_DegreeId",
                table: "APPLICATION_ATTACHED_DEGREES",
                column: "DegreeId");

            migrationBuilder.CreateIndex(
                name: "IX_APPLICATIONS_JobId",
                table: "APPLICATIONS",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_APPLICATIONS_StudentId",
                table: "APPLICATIONS",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AUTH_USERS_Email",
                table: "AUTH_USERS",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BEHAVIOR_LOGS_ActorId",
                table: "BEHAVIOR_LOGS",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_BEHAVIOR_LOGS_TargetTable_TargetId",
                table: "BEHAVIOR_LOGS",
                columns: new[] { "TargetTable", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_COMPANIES_BusinessLicenseCode",
                table: "COMPANIES",
                column: "BusinessLicenseCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_COMPANIES_ContactEmail",
                table: "COMPANIES",
                column: "ContactEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DEGREES_DegreeCode",
                table: "DEGREES",
                column: "DegreeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DEGREES_InstitutionId",
                table: "DEGREES",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_DEGREES_SignedByRegistrarId",
                table: "DEGREES",
                column: "SignedByRegistrarId");

            migrationBuilder.CreateIndex(
                name: "IX_DEGREES_StudentId",
                table: "DEGREES",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_EDUCATION_INSTITUTIONS_Code",
                table: "EDUCATION_INSTITUTIONS",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EDUCATION_INSTITUTIONS_Email",
                table: "EDUCATION_INSTITUTIONS",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JOB_DEGREE_FILTERS_JobId",
                table: "JOB_DEGREE_FILTERS",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_JOBS_CompanyId",
                table: "JOBS",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_JOBS_CreatedByAgentId",
                table: "JOBS",
                column: "CreatedByAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_JOBS_PartnerUniversityId",
                table: "JOBS",
                column: "PartnerUniversityId");

            migrationBuilder.CreateIndex(
                name: "IX_RECRUITER_AGENTS_CompanyId",
                table: "RECRUITER_AGENTS",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_RECRUITER_AGENTS_UserId",
                table: "RECRUITER_AGENTS",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_REGISTRARS_EmployeeCode",
                table: "REGISTRARS",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_REGISTRARS_InstitutionId",
                table: "REGISTRARS",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_REGISTRARS_UserId",
                table: "REGISTRARS",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTS_ReporterId",
                table: "REPORTS",
                column: "ReporterId");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTS_TargetDegreeId",
                table: "REPORTS",
                column: "TargetDegreeId");

            migrationBuilder.CreateIndex(
                name: "IX_STUDENTS_Email",
                table: "STUDENTS",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_STUDENTS_StudentCode",
                table: "STUDENTS",
                column: "StudentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_STUDENTS_UserId",
                table: "STUDENTS",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "APPLICATION_ATTACHED_DEGREES");

            migrationBuilder.DropTable(
                name: "BEHAVIOR_LOGS");

            migrationBuilder.DropTable(
                name: "JOB_DEGREE_FILTERS");

            migrationBuilder.DropTable(
                name: "REPORTS");

            migrationBuilder.DropTable(
                name: "APPLICATIONS");

            migrationBuilder.DropTable(
                name: "DEGREES");

            migrationBuilder.DropTable(
                name: "JOBS");

            migrationBuilder.DropTable(
                name: "REGISTRARS");

            migrationBuilder.DropTable(
                name: "STUDENTS");

            migrationBuilder.DropTable(
                name: "RECRUITER_AGENTS");

            migrationBuilder.DropTable(
                name: "EDUCATION_INSTITUTIONS");

            migrationBuilder.DropTable(
                name: "AUTH_USERS");

            migrationBuilder.DropTable(
                name: "COMPANIES");
        }
    }
}
