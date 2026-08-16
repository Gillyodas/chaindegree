using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChainDegree.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBatchMerkleRootUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BATCH_RECORDS_MerkleRoot",
                table: "BATCH_RECORDS");

            migrationBuilder.CreateIndex(
                name: "IX_BATCH_RECORDS_MerkleRoot",
                table: "BATCH_RECORDS",
                column: "MerkleRoot",
                filter: "[MerkleRoot] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BATCH_RECORDS_MerkleRoot",
                table: "BATCH_RECORDS");

            migrationBuilder.CreateIndex(
                name: "IX_BATCH_RECORDS_MerkleRoot",
                table: "BATCH_RECORDS",
                column: "MerkleRoot",
                unique: true,
                filter: "[MerkleRoot] IS NOT NULL");
        }
    }
}
