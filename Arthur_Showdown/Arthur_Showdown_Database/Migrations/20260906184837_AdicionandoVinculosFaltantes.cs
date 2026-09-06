using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arthur_Showdown_Database.Migrations
{
    /// <inheritdoc />
    public partial class AdicionandoVinculosFaltantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WavesAtuais_GameId",
                table: "WavesAtuais",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventarios_PersonagemRunId",
                table: "Inventarios",
                column: "PersonagemRunId");

            migrationBuilder.CreateIndex(
                name: "IX_HabilidadesRun_PersonagemRunId",
                table: "HabilidadesRun",
                column: "PersonagemRunId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_LocalAtualId",
                table: "Games",
                column: "LocalAtualId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_SessaoAtualId",
                table: "Games",
                column: "SessaoAtualId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_WaveAtualId",
                table: "Games",
                column: "WaveAtualId");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Locais_LocalAtualId",
                table: "Games",
                column: "LocalAtualId",
                principalTable: "Locais",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Games_Sessoes_SessaoAtualId",
                table: "Games",
                column: "SessaoAtualId",
                principalTable: "Sessoes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Games_WavesAtuais_WaveAtualId",
                table: "Games",
                column: "WaveAtualId",
                principalTable: "WavesAtuais",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_HabilidadesRun_PersonagensRun_PersonagemRunId",
                table: "HabilidadesRun",
                column: "PersonagemRunId",
                principalTable: "PersonagensRun",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Inventarios_PersonagensRun_PersonagemRunId",
                table: "Inventarios",
                column: "PersonagemRunId",
                principalTable: "PersonagensRun",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_WavesAtuais_Games_GameId",
                table: "WavesAtuais",
                column: "GameId",
                principalTable: "Games",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Games_Locais_LocalAtualId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_Sessoes_SessaoAtualId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_Games_WavesAtuais_WaveAtualId",
                table: "Games");

            migrationBuilder.DropForeignKey(
                name: "FK_HabilidadesRun_PersonagensRun_PersonagemRunId",
                table: "HabilidadesRun");

            migrationBuilder.DropForeignKey(
                name: "FK_Inventarios_PersonagensRun_PersonagemRunId",
                table: "Inventarios");

            migrationBuilder.DropForeignKey(
                name: "FK_WavesAtuais_Games_GameId",
                table: "WavesAtuais");

            migrationBuilder.DropIndex(
                name: "IX_WavesAtuais_GameId",
                table: "WavesAtuais");

            migrationBuilder.DropIndex(
                name: "IX_Inventarios_PersonagemRunId",
                table: "Inventarios");

            migrationBuilder.DropIndex(
                name: "IX_HabilidadesRun_PersonagemRunId",
                table: "HabilidadesRun");

            migrationBuilder.DropIndex(
                name: "IX_Games_LocalAtualId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_SessaoAtualId",
                table: "Games");

            migrationBuilder.DropIndex(
                name: "IX_Games_WaveAtualId",
                table: "Games");
        }
    }
}
