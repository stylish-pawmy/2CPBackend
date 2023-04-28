using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2cpbackend.Migrations
{
    /// <inheritdoc />
    public partial class Addcategoriestabletodatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_EventCategory_CategoryId",
                table: "Events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_EventCategory",
                table: "EventCategory");

            migrationBuilder.RenameTable(
                name: "EventCategory",
                newName: "Categories");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Categories",
                table: "Categories",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Categories_CategoryId",
                table: "Events",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Categories_CategoryId",
                table: "Events");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Categories",
                table: "Categories");

            migrationBuilder.RenameTable(
                name: "Categories",
                newName: "EventCategory");

            migrationBuilder.AddPrimaryKey(
                name: "PK_EventCategory",
                table: "EventCategory",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_EventCategory_CategoryId",
                table: "Events",
                column: "CategoryId",
                principalTable: "EventCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
