using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChainDegree.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase2TablesAndColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentVersion",
                table: "DEGREES",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "DEGREE_PROCESSING_RECORDS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BlockchainTxHash",
                table: "DEGREE_PROCESSING_RECORDS",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "DEGREE_PROCESSING_RECORDS",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "DEGREE_PROCESSING_RECORDS",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "DEGREE_PROCESSING_RECORDS",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DEGREE_UPDATE_REQUESTS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DegreeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Major = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Classification = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PlainDataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Salt = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DataHashLocal = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReasonDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEGREE_UPDATE_REQUESTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DEGREE_UPDATE_REQUESTS_DEGREES_DegreeId",
                        column: x => x.DegreeId,
                        principalTable: "DEGREES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DEGREE_VERSIONS",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DegreeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    PreviousHash = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CurrentHash = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    BlockchainTxHash = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MerkleProofJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    EffectiveAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEGREE_VERSIONS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DEGREE_VERSIONS_DEGREES_DegreeId",
                        column: x => x.DegreeId,
                        principalTable: "DEGREES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DEGREE_UPDATE_REQUESTS_DegreeId",
                table: "DEGREE_UPDATE_REQUESTS",
                column: "DegreeId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DEGREE_UPDATE_REQUESTS_DeletedAt",
                table: "DEGREE_UPDATE_REQUESTS",
                column: "DeletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DEGREE_VERSIONS_DegreeId",
                table: "DEGREE_VERSIONS",
                column: "DegreeId");

            migrationBuilder.CreateIndex(
                name: "IX_DEGREE_VERSIONS_DeletedAt",
                table: "DEGREE_VERSIONS",
                column: "DeletedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DEGREE_UPDATE_REQUESTS");

            migrationBuilder.DropTable(
                name: "DEGREE_VERSIONS");

            migrationBuilder.DropColumn(
                name: "CurrentVersion",
                table: "DEGREES");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "DEGREE_PROCESSING_RECORDS");

            migrationBuilder.DropColumn(
                name: "BlockchainTxHash",
                table: "DEGREE_PROCESSING_RECORDS");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "DEGREE_PROCESSING_RECORDS");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "DEGREE_PROCESSING_RECORDS");

            migrationBuilder.DropColumn(
                name: "State",
                table: "DEGREE_PROCESSING_RECORDS");
        }
    }
}
