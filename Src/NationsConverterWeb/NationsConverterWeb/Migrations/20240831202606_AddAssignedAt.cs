using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NationsConverterWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddAssignedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AssignedAt",
                table: "Blocks",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "Blocks");
        }
    }
}
