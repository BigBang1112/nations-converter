using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NationsConverterWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddItemUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Data",
                table: "BlockItems");

            migrationBuilder.CreateTable(
                name: "ItemUploads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Data = table.Column<byte[]>(type: "longblob", nullable: false),
                    OriginalFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    BlockItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemUploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemUploads_BlockItems_BlockItemId",
                        column: x => x.BlockItemId,
                        principalTable: "BlockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ItemUploads_BlockItemId",
                table: "ItemUploads",
                column: "BlockItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemUploads");

            migrationBuilder.AddColumn<byte[]>(
                name: "Data",
                table: "BlockItems",
                type: "longblob",
                nullable: true);
        }
    }
}
