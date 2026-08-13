using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChainDegree.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchUniquenessConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DEGREE_VERSIONS_DegreeId",
                table: "DEGREE_VERSIONS");

            migrationBuilder.DropIndex(
                name: "IX_BATCH_DEGREE_RECORDS_DegreeId",
                table: "BATCH_DEGREE_RECORDS");

            migrationBuilder.AddColumn<string>(
                name: "LeaseId",
                table: "DEGREE_PROCESSING_RECORDS",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "BATCH_DEGREE_RECORDS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_DEGREE_VERSIONS_DegreeId_Version",
                table: "DEGREE_VERSIONS",
                columns: new[] { "DegreeId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BATCH_RECORDS_MerkleRoot",
                table: "BATCH_RECORDS",
                column: "MerkleRoot",
                unique: true,
                filter: "[MerkleRoot] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BATCH_DEGREE_RECORDS_DegreeId_Version",
                table: "BATCH_DEGREE_RECORDS",
                columns: new[] { "DegreeId", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DEGREE_VERSIONS_DegreeId_Version",
                table: "DEGREE_VERSIONS");

            migrationBuilder.DropIndex(
                name: "IX_BATCH_RECORDS_MerkleRoot",
                table: "BATCH_RECORDS");

            migrationBuilder.DropIndex(
                name: "IX_BATCH_DEGREE_RECORDS_DegreeId_Version",
                table: "BATCH_DEGREE_RECORDS");

            migrationBuilder.DropColumn(
                name: "LeaseId",
                table: "DEGREE_PROCESSING_RECORDS");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "BATCH_DEGREE_RECORDS");

            migrationBuilder.CreateIndex(
                name: "IX_DEGREE_VERSIONS_DegreeId",
                table: "DEGREE_VERSIONS",
                column: "DegreeId");

            migrationBuilder.CreateIndex(
                name: "IX_BATCH_DEGREE_RECORDS_DegreeId",
                table: "BATCH_DEGREE_RECORDS",
                column: "DegreeId");
        }
    }
}
