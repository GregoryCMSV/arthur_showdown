using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Arthur_Showdown_Database.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Efeitos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Efeitos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Habilidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    CustoCompra = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Habilidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Itens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    CustoCompra = table.Column<int>(type: "integer", nullable: false),
                    CustoVenda = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Itens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposPersonagem",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposPersonagem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WavesAtuais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GameId = table.Column<int>(type: "integer", nullable: false),
                    InimigosGeradosIds = table.Column<List<int>>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WavesAtuais", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Ataques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    Descricao = table.Column<string>(type: "text", nullable: false),
                    Dano = table.Column<int>(type: "integer", nullable: false),
                    Cooldown = table.Column<int>(type: "integer", nullable: false),
                    IsFastAction = table.Column<bool>(type: "boolean", nullable: false),
                    LimiteAprimoramento = table.Column<int>(type: "integer", nullable: false),
                    EfeitoId = table.Column<int>(type: "integer", nullable: true),
                    Hitbox = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ataques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ataques_Efeitos_EfeitoId",
                        column: x => x.EfeitoId,
                        principalTable: "Efeitos",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "HabilidadesRun",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemRunId = table.Column<int>(type: "integer", nullable: false),
                    HabilidadeId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HabilidadesRun", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HabilidadesRun_Habilidades_HabilidadeId",
                        column: x => x.HabilidadeId,
                        principalTable: "Habilidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Inventarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemRunId = table.Column<int>(type: "integer", nullable: false),
                    ItemId = table.Column<int>(type: "integer", nullable: false),
                    Qtd = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventarios_Itens_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Itens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JogadorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalOuroGanho = table.Column<int>(type: "integer", nullable: false),
                    OuroAtual = table.Column<int>(type: "integer", nullable: false),
                    LocalAtualId = table.Column<int>(type: "integer", nullable: false),
                    DataInicio = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataFim = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatusRunId = table.Column<int>(type: "integer", nullable: false),
                    TempoSegundos = table.Column<int>(type: "integer", nullable: false),
                    TurnosJogados = table.Column<int>(type: "integer", nullable: false),
                    QtdMudancaDirecao = table.Column<int>(type: "integer", nullable: false),
                    QtdHitsSofridos = table.Column<int>(type: "integer", nullable: false),
                    SessaoAtualId = table.Column<int>(type: "integer", nullable: true),
                    WaveAtualId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_StatusRuns_StatusRunId",
                        column: x => x.StatusRunId,
                        principalTable: "StatusRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Personagens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    TipoPersonagemId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Personagens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Personagens_TiposPersonagem_TipoPersonagemId",
                        column: x => x.TipoPersonagemId,
                        principalTable: "TiposPersonagem",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Locais",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "text", nullable: false),
                    OrdemProgressao = table.Column<int>(type: "integer", nullable: false),
                    QtdSessoes = table.Column<int>(type: "integer", nullable: false),
                    ChefeId = table.Column<int>(type: "integer", nullable: true),
                    InimigosPossiveisIds = table.Column<List<int>>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locais", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Locais_Personagens_ChefeId",
                        column: x => x.ChefeId,
                        principalTable: "Personagens",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PersonagensRun",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemId = table.Column<int>(type: "integer", nullable: false),
                    VidaMaxima = table.Column<int>(type: "integer", nullable: false),
                    VidaAtual = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonagensRun", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonagensRun_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PersonagensRun_Personagens_PersonagemId",
                        column: x => x.PersonagemId,
                        principalTable: "Personagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    NumeroSessao = table.Column<int>(type: "integer", nullable: false),
                    TamanhoMapa = table.Column<int>(type: "integer", nullable: false),
                    LocalId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessoes_Locais_LocalId",
                        column: x => x.LocalId,
                        principalTable: "Locais",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DecksAtaques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PersonagemRunId = table.Column<int>(type: "integer", nullable: false),
                    AtaqueId = table.Column<int>(type: "integer", nullable: false),
                    NumeroDeUpgrades = table.Column<int>(type: "integer", nullable: false),
                    LimiteUpgrade = table.Column<int>(type: "integer", nullable: false),
                    ModDano = table.Column<int>(type: "integer", nullable: false),
                    ModCooldown = table.Column<int>(type: "integer", nullable: false),
                    EfeitoId = table.Column<int>(type: "integer", nullable: true),
                    QtdUsosNaRun = table.Column<int>(type: "integer", nullable: false),
                    QtdVezesRemovidoFila = table.Column<int>(type: "integer", nullable: false),
                    QtdVezesAdicionadoFila = table.Column<int>(type: "integer", nullable: false),
                    IsFastAction = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecksAtaques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DecksAtaques_Ataques_AtaqueId",
                        column: x => x.AtaqueId,
                        principalTable: "Ataques",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DecksAtaques_Efeitos_EfeitoId",
                        column: x => x.EfeitoId,
                        principalTable: "Efeitos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DecksAtaques_PersonagensRun_PersonagemRunId",
                        column: x => x.PersonagemRunId,
                        principalTable: "PersonagensRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Waves",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    QtdInimigos = table.Column<int>(type: "integer", nullable: false),
                    SessaoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Waves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Waves_Sessoes_SessaoId",
                        column: x => x.SessaoId,
                        principalTable: "Sessoes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "StatusRuns",
                columns: new[] { "Id", "Nome" },
                values: new object[,]
                {
                    { 1, "Em Andamento" },
                    { 2, "Vitoria" },
                    { 3, "Derrota" },
                    { 4, "Desistencia" }
                });

            migrationBuilder.InsertData(
                table: "TiposPersonagem",
                columns: new[] { "Id", "Nome" },
                values: new object[,]
                {
                    { 1, "Playable" },
                    { 2, "Enemy" },
                    { 3, "Npc" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ataques_EfeitoId",
                table: "Ataques",
                column: "EfeitoId");

            migrationBuilder.CreateIndex(
                name: "IX_DecksAtaques_AtaqueId",
                table: "DecksAtaques",
                column: "AtaqueId");

            migrationBuilder.CreateIndex(
                name: "IX_DecksAtaques_EfeitoId",
                table: "DecksAtaques",
                column: "EfeitoId");

            migrationBuilder.CreateIndex(
                name: "IX_DecksAtaques_PersonagemRunId",
                table: "DecksAtaques",
                column: "PersonagemRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_StatusRunId",
                table: "Games",
                column: "StatusRunId");

            migrationBuilder.CreateIndex(
                name: "IX_HabilidadesRun_HabilidadeId",
                table: "HabilidadesRun",
                column: "HabilidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventarios_ItemId",
                table: "Inventarios",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Locais_ChefeId",
                table: "Locais",
                column: "ChefeId");

            migrationBuilder.CreateIndex(
                name: "IX_Personagens_TipoPersonagemId",
                table: "Personagens",
                column: "TipoPersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagensRun_GameId",
                table: "PersonagensRun",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonagensRun_PersonagemId",
                table: "PersonagensRun",
                column: "PersonagemId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessoes_LocalId",
                table: "Sessoes",
                column: "LocalId");

            migrationBuilder.CreateIndex(
                name: "IX_Waves_SessaoId",
                table: "Waves",
                column: "SessaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecksAtaques");

            migrationBuilder.DropTable(
                name: "HabilidadesRun");

            migrationBuilder.DropTable(
                name: "Inventarios");

            migrationBuilder.DropTable(
                name: "Waves");

            migrationBuilder.DropTable(
                name: "WavesAtuais");

            migrationBuilder.DropTable(
                name: "Ataques");

            migrationBuilder.DropTable(
                name: "PersonagensRun");

            migrationBuilder.DropTable(
                name: "Habilidades");

            migrationBuilder.DropTable(
                name: "Itens");

            migrationBuilder.DropTable(
                name: "Sessoes");

            migrationBuilder.DropTable(
                name: "Efeitos");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Locais");

            migrationBuilder.DropTable(
                name: "StatusRuns");

            migrationBuilder.DropTable(
                name: "Personagens");

            migrationBuilder.DropTable(
                name: "TiposPersonagem");
        }
    }
}
