using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GorgoDresses.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThumbUrlAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThumbUrl",
                table: "Dresses",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThumbUrl",
                table: "Dresses");
        }
    }
}
