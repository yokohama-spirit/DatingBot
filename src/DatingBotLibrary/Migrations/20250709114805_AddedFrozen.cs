using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBotLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddedFrozen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isFrozen",
                table: "Profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isFrozen",
                table: "Profiles");
        }
    }
}
