using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingBotLibrary.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeProfileQueries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_ChatId",
                table: "Profiles",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_UserId",
                table: "Profiles",
                column: "UserId",
                unique: true); 


            migrationBuilder.CreateIndex(
                name: "IX_Profiles_City_Age",
                table: "Profiles",
                columns: new[] { "City", "Age" });


            migrationBuilder.CreateIndex(
                name: "IX_Photos_FileId",
                table: "Photos",
                column: "FileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Profiles_ChatId",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_UserId",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Profiles_City_Age",
                table: "Profiles");

            migrationBuilder.DropIndex(
                name: "IX_Photos_FileId",
                table: "Photos");
        }
    }
}
