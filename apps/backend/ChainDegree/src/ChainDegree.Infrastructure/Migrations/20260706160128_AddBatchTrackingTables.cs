using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChainDegree.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchTrackingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DEGREES",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "BATCH_RECORDS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstitutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DegreeCount = table.Column<int>(type: "int", nullable: false),
                    MerkleRoot = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    TxHash = table.Column<string>(type: "nvarchar(66)", maxLength: 66, nullable: true),
                    BlockNumber = table.Column<long>(type: "bigint", nullable: true),
                    EstimatedWaitTimeSeconds = table.Column<int>(type: "int", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BATCH_RECORDS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BATCH_RECORDS_EDUCATION_INSTITUTIONS_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "EDUCATION_INSTITUTIONS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DEGREE_PROCESSING_RECORDS",
                columns: table => new
                {
                    DegreeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    NextRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastRetryAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeaseUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEGREE_PROCESSING_RECORDS", x => x.DegreeId);
                    table.ForeignKey(
                        name: "FK_DEGREE_PROCESSING_RECORDS_DEGREES_DegreeId",
                        column: x => x.DegreeId,
                        principalTable: "DEGREES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IDEMPOTENCY_RECORDS",
                columns: table => new
                {
                    IdempotencyKey = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ResponseBodyJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IDEMPOTENCY_RECORDS", x => x.IdempotencyKey);
                });

            migrationBuilder.CreateTable(
                name: "BATCH_DEGREE_RECORDS",
                columns: table => new
                {
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DegreeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LeafIndex = table.Column<int>(type: "int", nullable: false),
                    ProofHashesJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BATCH_DEGREE_RECORDS", x => new { x.BatchId, x.DegreeId });
                    table.ForeignKey(
                        name: "FK_BATCH_DEGREE_RECORDS_BATCH_RECORDS_BatchId",
                        column: x => x.BatchId,
                        principalTable: "BATCH_RECORDS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BATCH_DEGREE_RECORDS_DEGREES_DegreeId",
                        column: x => x.DegreeId,
                        principalTable: "DEGREES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BATCH_DEGREE_RECORDS_DegreeId",
                table: "BATCH_DEGREE_RECORDS",
                column: "DegreeId");

            migrationBuilder.CreateIndex(
                name: "IX_BATCH_RECORDS_BatchName",
                table: "BATCH_RECORDS",
                column: "BatchName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BATCH_RECORDS_InstitutionId",
                table: "BATCH_RECORDS",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_IDEMPOTENCY_RECORDS_ExpiresAt",
                table: "IDEMPOTENCY_RECORDS",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BATCH_DEGREE_RECORDS");

            migrationBuilder.DropTable(
                name: "DEGREE_PROCESSING_RECORDS");

            migrationBuilder.DropTable(
                name: "IDEMPOTENCY_RECORDS");

            migrationBuilder.DropTable(
                name: "BATCH_RECORDS");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DEGREES");
        }
    }
}
