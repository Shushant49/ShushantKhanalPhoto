using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShushantKhanalPhoto.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriesAndCloudinarySync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CloudinaryPublicId",
                table: "Photos",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Portrait" },
                    { 2, "Prom" },
                    { 3, "Graduation" },
                    { 4, "Event" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_CloudinaryPublicId",
                table: "Photos",
                column: "CloudinaryPublicId",
                unique: true,
                filter: "[CloudinaryPublicId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Photos_CloudinaryPublicId",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "CloudinaryPublicId",
                table: "Photos");
        }
    }
}
