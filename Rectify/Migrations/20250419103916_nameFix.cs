using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rectify.Migrations
{
    /// <inheritdoc />
    public partial class nameFix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BrandAddress",
                table: "AspNetUsers",
                newName: "BranchAddress");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "BranchAddress",
                table: "AspNetUsers",
                newName: "BrandAddress");
        }
    }
}
