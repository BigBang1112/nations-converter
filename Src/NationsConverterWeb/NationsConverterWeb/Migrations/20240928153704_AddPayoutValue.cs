using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NationsConverterWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "CurrentlyPaidValue",
                table: "Users",
                type: "float",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "PayoutValue",
                table: "Users",
                type: "float",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentlyPaidValue",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PayoutValue",
                table: "Users");
        }
    }
}
