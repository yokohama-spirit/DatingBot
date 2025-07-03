using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBotLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddedGendersAndInterests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Gender",
                table: "Profiles",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InInterests",
                table: "Profiles",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Gender",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "InInterests",
                table: "Profiles");
        }
    }
}
