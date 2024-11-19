using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NationsConverterWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddMacroblockUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MacroblockUploads",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Data = table.Column<byte[]>(type: "longblob", nullable: false),
                    OriginalFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModifiedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    UploadedById = table.Column<int>(type: "int", nullable: true),
                    BlockItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacroblockUploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MacroblockUploads_BlockItems_BlockItemId",
                        column: x => x.BlockItemId,
                        principalTable: "BlockItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MacroblockUploads_Users_UploadedById",
                        column: x => x.UploadedById,
                        principalTable: "Users",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_MacroblockUploads_BlockItemId",
                table: "MacroblockUploads",
                column: "BlockItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MacroblockUploads_UploadedById",
                table: "MacroblockUploads",
                column: "UploadedById");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MacroblockUploads");
        }
    }
}
