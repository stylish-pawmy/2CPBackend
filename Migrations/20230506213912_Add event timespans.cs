using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2cpbackend.Migrations
{
    /// <inheritdoc />
    public partial class Addeventtimespans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "TimeSpan",
                table: "Events",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeSpan",
                table: "Events");
        }
    }
}
