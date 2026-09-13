using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arthur_Showdown_Database.Migrations
{
    /// <inheritdoc />
    public partial class VincularPersonagemAtaque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PersonagemAtaques",
                columns: table => new
                {
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    AtaqueId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagemAtaques", x => new { x.PersonagemId, x.AtaqueId });
                    table.ForeignKey(
                        name: "FK_PersonagemAtaques_Ataques_AtaqueId",
                        column: x => x.AtaqueId,
                        principalTable: "Ataques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonagemAtaques_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PersonagemAtaques_AtaqueId",
                table: "PersonagemAtaques",
                column: "AtaqueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PersonagemAtaques");
        }
    }
}
