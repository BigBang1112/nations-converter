using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NationsConverterWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadedByAndValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UploadedById",
                table: "ItemUploads",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Value",
                table: "ItemUploads",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Value",
                table: "BlockItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ItemUploads_UploadedById",
                table: "ItemUploads",
                column: "UploadedById");

            migrationBuilder.AddForeignKey(
                name: "FK_ItemUploads_Users_UploadedById",
                table: "ItemUploads",
                column: "UploadedById",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ItemUploads_Users_UploadedById",
                table: "ItemUploads");

            migrationBuilder.DropIndex(
                name: "IX_ItemUploads_UploadedById",
                table: "ItemUploads");

            migrationBuilder.DropColumn(
                name: "UploadedById",
                table: "ItemUploads");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "ItemUploads");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "BlockItems");
        }
    }
}
