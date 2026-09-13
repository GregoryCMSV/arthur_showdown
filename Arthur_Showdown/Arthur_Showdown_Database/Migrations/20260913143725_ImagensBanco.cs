using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arthur_Showdown_Database.Migrations
{
    /// <inheritdoc />
    public partial class ImagensBanco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagemUrl",
                table: "Personagens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagemUrl",
                table: "Locais",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagemUrl",
                table: "Itens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagemUrl",
                table: "Habilidades",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImagemUrl",
                table: "Ataques",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagemUrl",
                table: "Personagens");

            migrationBuilder.DropColumn(
                name: "ImagemUrl",
                table: "Locais");

            migrationBuilder.DropColumn(
                name: "ImagemUrl",
                table: "Itens");

            migrationBuilder.DropColumn(
                name: "ImagemUrl",
                table: "Habilidades");

            migrationBuilder.DropColumn(
                name: "ImagemUrl",
                table: "Ataques");
        }
    }
}
