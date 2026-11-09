using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Arthur_Showdown_Database.Migrations
{
    /// <inheritdoc />
    public partial class FilaAcoesInimigo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DirecaoOlhar",
                table: "PersonagensRun",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Posicao",
                table: "PersonagensRun",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CooldownRestante",
                table: "DecksAtaques",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "InimigoAcoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    Ordem = table.Column<int>(type: "integer", nullable: false),
                    Tipo = table.Column<string>(type: "text", nullable: false),
                    AtaqueId = table.Column<int>(type: "integer", nullable: true),
                    Valor = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InimigoAcoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InimigoAcoes_Ataques_AtaqueId",
                        column: x => x.AtaqueId,
                        principalTable: "Ataques",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InimigoAcoes_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InimigosRun",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    VidaAtual = table.Column<int>(type: "integer", nullable: false),
                    Posicao = table.Column<int>(type: "integer", nullable: false),
                    CursorAcao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InimigosRun", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InimigosRun_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InimigosRun_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InimigoAcoes_AtaqueId",
                table: "InimigoAcoes",
                column: "AtaqueId");

            migrationBuilder.CreateIndex(
                name: "IX_InimigoAcoes_PersonagemId_Ordem",
                table: "InimigoAcoes",
                columns: new[] { "PersonagemId", "Ordem" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InimigosRun_GameId",
                table: "InimigosRun",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_InimigosRun_PersonagemId",
                table: "InimigosRun",
                column: "PersonagemId");


            // Seed: cria um inimigo de teste se nao existir nenhum 
            migrationBuilder.Sql(@"
                INSERT INTO ""Personagens"" (""Nome"", ""TipoPersonagemId"", ""ImagemUrl"")
                SELECT 'Esqueleto', 2, 'https://magicoloriage.com/wp-content/uploads/2025/06/Pixel-art-de-squelette-Minecraft-avec-arc.jpeg'
                WHERE NOT EXISTS (SELECT 1 FROM ""Personagens"" WHERE ""TipoPersonagemId"" = 2);
            ");

            // Seed: fila de acoes para todo inimigo que ainda nao tem script.
            // Mover: Valor = tiles na direcao do jogador | Atacar sem AtaqueId: Valor = dano
            migrationBuilder.Sql(@"
                INSERT INTO ""InimigoAcoes"" (""PersonagemId"", ""Ordem"", ""Tipo"", ""AtaqueId"", ""Valor"")
                SELECT p.""Id"", v.""Ordem"", v.""Tipo"", NULL, v.""Valor""
                FROM ""Personagens"" p
                CROSS JOIN (VALUES (0, 'Mover', 1), (1, 'Mover', 1), (2, 'Preparar', 0), (3, 'Atacar', 1)) AS v(""Ordem"", ""Tipo"", ""Valor"")
                WHERE p.""TipoPersonagemId"" = 2
                  AND NOT EXISTS (SELECT 1 FROM ""InimigoAcoes"" a WHERE a.""PersonagemId"" = p.""Id"");
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InimigoAcoes");

            migrationBuilder.DropTable(
                name: "InimigosRun");

            migrationBuilder.DropColumn(
                name: "DirecaoOlhar",
                table: "PersonagensRun");

            migrationBuilder.DropColumn(
                name: "Posicao",
                table: "PersonagensRun");

            migrationBuilder.DropColumn(
                name: "CooldownRestante",
                table: "DecksAtaques");
        }
    }
}
