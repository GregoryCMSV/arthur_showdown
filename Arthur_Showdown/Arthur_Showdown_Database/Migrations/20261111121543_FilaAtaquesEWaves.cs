using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arthur_Showdown_Database.Migrations
{
    /// <inheritdoc />
    public partial class FilaAtaquesEWaves : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Preparado",
                table: "InimigosRun",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RecuoPendente",
                table: "InimigosRun",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Telegrafando",
                table: "InimigosRun",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "WaveNumero",
                table: "Games",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PosicaoFila",
                table: "DecksAtaques",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Preparado",
                table: "InimigosRun");

            migrationBuilder.DropColumn(
                name: "RecuoPendente",
                table: "InimigosRun");

            migrationBuilder.DropColumn(
                name: "Telegrafando",
                table: "InimigosRun");

            migrationBuilder.DropColumn(
                name: "WaveNumero",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "PosicaoFila",
                table: "DecksAtaques");
        }
    }
}
