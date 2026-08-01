using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChainDegree.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDegreeId_AddIsPrimary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DegreeId",
                table: "APPLICATIONS");

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "APPLICATION_ATTACHED_DEGREES",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "APPLICATION_ATTACHED_DEGREES");

            migrationBuilder.AddColumn<Guid>(
                name: "DegreeId",
                table: "APPLICATIONS",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
