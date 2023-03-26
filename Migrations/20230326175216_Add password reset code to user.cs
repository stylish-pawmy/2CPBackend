using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _2cpbackend.Migrations
{
    /// <inheritdoc />
    public partial class Addpasswordresetcodetouser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PasswordResetCode",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PasswordResetCode",
                table: "AspNetUsers");
        }
    }
}
