using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2cpbackend.Migrations
{
    /// <inheritdoc />
    public partial class Addmaximumattendeesnumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxAttendees",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxAttendees",
                table: "Events");
        }
    }
}
