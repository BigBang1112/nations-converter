using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NationsConverterWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddBlocksAndDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "JoinedAt",
                table: "Users",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ConnectedAt",
                table: "DiscordUsers",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<int>(
                name: "AssignedToId",
                table: "Blocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "Blocks",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Blocks",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Blocks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EnvironmentId",
                table: "Blocks",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IconWebp",
                table: "Blocks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Blocks",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PageName",
                table: "Blocks",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SubCategoryId",
                table: "Blocks",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BlockItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FileName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Modifier = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Variant = table.Column<int>(type: "int", nullable: false),
                    SubVariant = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<byte[]>(type: "longblob", nullable: true),
                    BlockId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockItems_Blocks_BlockId",
                        column: x => x.BlockId,
                        principalTable: "Blocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConverterCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConverterCategories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConverterSubCategories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConverterSubCategories", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GameEnvironments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameEnvironments", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_AssignedToId",
                table: "Blocks",
                column: "AssignedToId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_CategoryId",
                table: "Blocks",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_EnvironmentId",
                table: "Blocks",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Blocks_SubCategoryId",
                table: "Blocks",
                column: "SubCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockItems_BlockId",
                table: "BlockItems",
                column: "BlockId");

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_ConverterCategories_CategoryId",
                table: "Blocks",
                column: "CategoryId",
                principalTable: "ConverterCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_ConverterSubCategories_SubCategoryId",
                table: "Blocks",
                column: "SubCategoryId",
                principalTable: "ConverterSubCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_GameEnvironments_EnvironmentId",
                table: "Blocks",
                column: "EnvironmentId",
                principalTable: "GameEnvironments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Blocks_Users_AssignedToId",
                table: "Blocks",
                column: "AssignedToId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Blocks_ConverterCategories_CategoryId",
                table: "Blocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Blocks_ConverterSubCategories_SubCategoryId",
                table: "Blocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Blocks_GameEnvironments_EnvironmentId",
                table: "Blocks");

            migrationBuilder.DropForeignKey(
                name: "FK_Blocks_Users_AssignedToId",
                table: "Blocks");

            migrationBuilder.DropTable(
                name: "BlockItems");

            migrationBuilder.DropTable(
                name: "ConverterCategories");

            migrationBuilder.DropTable(
                name: "ConverterSubCategories");

            migrationBuilder.DropTable(
                name: "GameEnvironments");

            migrationBuilder.DropIndex(
                name: "IX_Blocks_AssignedToId",
                table: "Blocks");

            migrationBuilder.DropIndex(
                name: "IX_Blocks_CategoryId",
                table: "Blocks");

            migrationBuilder.DropIndex(
                name: "IX_Blocks_EnvironmentId",
                table: "Blocks");

            migrationBuilder.DropIndex(
                name: "IX_Blocks_SubCategoryId",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "JoinedAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ConnectedAt",
                table: "DiscordUsers");

            migrationBuilder.DropColumn(
                name: "AssignedToId",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "EnvironmentId",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "IconWebp",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "PageName",
                table: "Blocks");

            migrationBuilder.DropColumn(
                name: "SubCategoryId",
                table: "Blocks");
        }
    }
}
