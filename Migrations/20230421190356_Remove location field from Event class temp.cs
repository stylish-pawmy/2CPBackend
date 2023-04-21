using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace _2cpbackend.Migrations
{
    /// <inheritdoc />
    public partial class RemovelocationfieldfromEventclasstemp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Events");

            migrationBuilder.AddColumn<string>(
                name: "CoverPhoto",
                table: "Events",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoverPhoto",
                table: "Events");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Events",
                type: "geometry",
                nullable: true);
        }
    }
}
