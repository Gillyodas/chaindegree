using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChainDegree.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlainDataToDegreeVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Classification",
                table: "DEGREE_VERSIONS",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Major",
                table: "DEGREE_VERSIONS",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PlainDataJson",
                table: "DEGREE_VERSIONS",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Salt",
                table: "DEGREE_VERSIONS",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Classification",
                table: "DEGREE_VERSIONS");

            migrationBuilder.DropColumn(
                name: "Major",
                table: "DEGREE_VERSIONS");

            migrationBuilder.DropColumn(
                name: "PlainDataJson",
                table: "DEGREE_VERSIONS");

            migrationBuilder.DropColumn(
                name: "Salt",
                table: "DEGREE_VERSIONS");
        }
    }
}
