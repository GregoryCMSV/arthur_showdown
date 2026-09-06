using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arthur_Showdown_Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionandoMetagame : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgressoJogadores",
                columns: table => new
                {
                    JogadorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmblemasKamelot = table.Column<int>(type: "integer", nullable: false),
                    PersonagensDesbloqueadosIds = table.Column<List<int>>(type: "integer[]", nullable: false),
                    AtaquesDesbloqueadosIds = table.Column<List<int>>(type: "integer[]", nullable: false),
                    HabilidadesDesbloqueadasIds = table.Column<List<int>>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgressoJogadores", x => x.JogadorId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProgressoJogadores");
        }
    }
}
