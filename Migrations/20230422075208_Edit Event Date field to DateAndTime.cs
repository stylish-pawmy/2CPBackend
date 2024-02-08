using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eventi.Server.Migrations
{
    /// <inheritdoc />
    public partial class EditEventDatefieldtoDateAndTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Events",
                newName: "DateAndTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateAndTime",
                table: "Events",
                newName: "Date");
        }
    }
}
