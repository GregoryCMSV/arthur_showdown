using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arthur_Showdown_Database.Migrations
{
    /// <inheritdoc />
    public partial class ComportamentoInimigosFloresta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DirecaoOlhar",
                table: "InimigosRun",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Ataques dos inimigos. Hitbox: Origem 0 = Caster, Direcao 0 = Forward
            migrationBuilder.Sql(@"
                INSERT INTO ""Ataques"" (""Nome"", ""Descricao"", ""Dano"", ""Cooldown"", ""IsFastAction"", ""LimiteAprimoramento"", ""EfeitoId"", ""Hitbox"", ""ImagemUrl"")
                SELECT v.""Nome"", v.""Descricao"", v.""Dano"", 0, false, 0, NULL, v.""Hitbox""::jsonb, NULL
                FROM (VALUES
                    ('Golpe do Esqueleto',  '1 celula a frente',  2, '{""Origem"":0,""Direcao"":0,""AlcanceMin"":1,""AlcanceMax"":1}'),
                    ('Estocada do Lanceiro','2 celulas a frente', 2, '{""Origem"":0,""Direcao"":0,""AlcanceMin"":1,""AlcanceMax"":2}'),
                    ('Golpe do Tank',       '1 celula a frente',  3, '{""Origem"":0,""Direcao"":0,""AlcanceMin"":1,""AlcanceMax"":1}'),
                    ('Flecha do Arqueiro',  'Qualquer distancia, sem inimigo no caminho', 2, '{""Origem"":0,""Direcao"":0,""AlcanceMin"":1,""AlcanceMax"":99}')
                ) AS v(""Nome"", ""Descricao"", ""Dano"", ""Hitbox"")
                WHERE NOT EXISTS (SELECT 1 FROM ""Ataques"" a WHERE a.""Nome"" = v.""Nome"");
            ");

            // Substitui a fila de acoes (IA agora e reativa): Atacar define o ataque; Recuar* define o recuo
            migrationBuilder.Sql(@"
                DELETE FROM ""InimigoAcoes""
                WHERE ""PersonagemId"" IN (SELECT ""Id"" FROM ""Personagens"" WHERE ""Nome"" IN ('Esqueleto', 'Tank', 'Arqueiro', 'Lanceiro'));

                INSERT INTO ""InimigoAcoes"" (""PersonagemId"", ""Ordem"", ""Tipo"", ""AtaqueId"", ""Valor"")
                SELECT p.""Id"", v.""Ordem"", v.""Tipo"",
                       (SELECT a.""Id"" FROM ""Ataques"" a WHERE a.""Nome"" = v.""Ataque"" ORDER BY a.""Id"" LIMIT 1),
                       v.""Valor""
                FROM ""Personagens"" p
                JOIN (VALUES
                    ('Esqueleto', 0, 'Atacar',           'Golpe do Esqueleto',   2),
                    ('Esqueleto', 1, 'RecuarAposAtaque', NULL,                   1),
                    ('Lanceiro',  0, 'Atacar',           'Estocada do Lanceiro', 2),
                    ('Lanceiro',  1, 'RecuarAposAtaque', NULL,                   1),
                    ('Tank',      0, 'Atacar',           'Golpe do Tank',        3),
                    ('Arqueiro',  0, 'Atacar',           'Flecha do Arqueiro',   2),
                    ('Arqueiro',  1, 'RecuarSeProximo',  NULL,                   1)
                ) AS v(""Nome"", ""Ordem"", ""Tipo"", ""Ataque"", ""Valor"") ON v.""Nome"" = p.""Nome"";
            ");

            // Tamanho do mapa: sessao 1 = 5, sessoes 2 e 3 = 7
            migrationBuilder.Sql(@"
                UPDATE ""Sessoes"" s SET ""TamanhoMapa"" = CASE WHEN s.""NumeroSessao"" = 1 THEN 5 ELSE 7 END
                FROM ""Locais"" l
                WHERE l.""Id"" = s.""LocalId"" AND l.""Nome"" = 'Floresta';
            ");

            // Waves: sessao 1 = 2,2,3 | sessao 2 = 3,3,3,4,4 | sessao 3 (boss) = 1
            migrationBuilder.Sql(@"
                UPDATE ""Waves"" w SET ""QtdInimigos"" = v.""Qtd""
                FROM (
                    SELECT w2.""Id"" AS ""WaveId"", s.""NumeroSessao"" AS ""Sessao"",
                           ROW_NUMBER() OVER (PARTITION BY w2.""SessaoId"" ORDER BY w2.""Id"") AS ""Idx""
                    FROM ""Waves"" w2
                    JOIN ""Sessoes"" s ON s.""Id"" = w2.""SessaoId""
                    JOIN ""Locais"" l ON l.""Id"" = s.""LocalId"" AND l.""Nome"" = 'Floresta'
                ) o
                JOIN (VALUES
                    (1, 1, 2), (1, 2, 2), (1, 3, 3),
                    (2, 1, 3), (2, 2, 3), (2, 3, 3), (2, 4, 4), (2, 5, 4),
                    (3, 1, 1)
                ) AS v(""Sessao"", ""Idx"", ""Qtd"") ON v.""Sessao"" = o.""Sessao"" AND v.""Idx"" = o.""Idx""
                WHERE w.""Id"" = o.""WaveId"";
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
                DELETE FROM ""InimigoAcoes"" WHERE ""AtaqueId"" IN (SELECT ""Id"" FROM ""Ataques"" WHERE ""Nome"" IN ('Golpe do Esqueleto','Estocada do Lanceiro','Golpe do Tank','Flecha do Arqueiro'));
                DELETE FROM ""Ataques"" WHERE ""Nome"" IN ('Golpe do Esqueleto','Estocada do Lanceiro','Golpe do Tank','Flecha do Arqueiro');
            ");
            migrationBuilder.DropColumn(
                name: "DirecaoOlhar",
                table: "InimigosRun");
        }
    }
}
