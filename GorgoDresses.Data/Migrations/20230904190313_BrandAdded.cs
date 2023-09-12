using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorgoDresses.Data.Migrations
{
    /// <inheritdoc />
    public partial class BrandAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Dresses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Dresses");
        }
    }
}
