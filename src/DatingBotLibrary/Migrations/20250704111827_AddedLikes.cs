using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBotLibrary.Migrations
{
    /// <inheritdoc />
    public partial class AddedLikes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<long>>(
                name: "Likes",
                table: "Profiles",
                type: "bigint[]",
                nullable: false,
                defaultValue: new List<long>()); // Важно: указать default значение
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Likes",
                table: "Profiles");
        }
    }
}
