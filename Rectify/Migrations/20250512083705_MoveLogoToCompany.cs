using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rectify.Migrations
{
    /// <inheritdoc />
    public partial class MoveLogoToCompany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoImage",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<byte[]>(
                name: "LogoImage",
                table: "CompanyModel",
                type: "varbinary(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoImage",
                table: "CompanyModel");

            migrationBuilder.AddColumn<byte[]>(
                name: "LogoImage",
                table: "AspNetUsers",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
