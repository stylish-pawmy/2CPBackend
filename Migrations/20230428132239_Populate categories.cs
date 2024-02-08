using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventi.Server.Migrations
{
    /// <inheritdoc />
    public partial class Populatecategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] {"Id", "Name"},
                values: new object[,] {
                    {1, "Sports"},
                    {2, "Culture"},
                    {3, "Youth"},
                    {4, "Business"},
                    {5, "Music"},
                    {7, "Health"},
                    {8, "History"},
                    {9, "General"}
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValues: new object[] {1, 2, 3, 4, 5, 6, 7, 8, 9}
            );
        }
    }
}
