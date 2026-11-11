using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Arthur_Showdown_Database.Migrations
{
    /// <inheritdoc />
    public partial class PrimeiraFaseFloresta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Vida",
                table: "Personagens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Vida base dos inimigos
            migrationBuilder.Sql(@"
                UPDATE ""Personagens"" SET ""Vida"" = 3 WHERE ""Nome"" = 'Esqueleto';
            ");

            // Novos inimigos (TipoPersonagemId = 2)
            migrationBuilder.Sql(@"
                INSERT INTO ""Personagens"" (""Nome"", ""TipoPersonagemId"", ""ImagemUrl"", ""Vida"")
                SELECT v.""Nome"", 2, v.""ImagemUrl"", v.""Vida""
                FROM (VALUES
                    ('Tank',     'https://png.pngtree.com/png-clipart/20230703/ourmid/pngtree-battle-tank-png-image_7444190.png', 20),
                    ('Arqueiro', 'https://thumbs.dreamstime.com/b/archer-fantasy-pixel-art-game-cartoon-character-elf-archer-fantasy-cartoon-character-video-game-sprite-pixel-art-style-413574152.jpg', 3),
                    ('Lanceiro', 'https://i.pinimg.com/736x/fd/fe/11/fdfe11675f947ee2fe982472a3ba0f4d.jpg', 4)
                ) AS v(""Nome"", ""ImagemUrl"", ""Vida"")
                WHERE NOT EXISTS (SELECT 1 FROM ""Personagens"" p WHERE p.""Nome"" = v.""Nome"");
            ");

            // Fila de acoes (Mover: Valor = tiles | Atacar sem AtaqueId: Valor = dano)
            migrationBuilder.Sql(@"
                INSERT INTO ""InimigoAcoes"" (""PersonagemId"", ""Ordem"", ""Tipo"", ""AtaqueId"", ""Valor"")
                SELECT p.""Id"", a.""Ordem"", a.""Tipo"", NULL, a.""Valor""
                FROM ""Personagens"" p
                JOIN (VALUES
                    ('Tank',     0, 'Mover',    1),
                    ('Tank',     1, 'Preparar', 0),
                    ('Tank',     2, 'Atacar',   3),
                    ('Arqueiro', 0, 'Esperar',  0),
                    ('Arqueiro', 1, 'Preparar', 0),
                    ('Arqueiro', 2, 'Atacar',   2),
                    ('Lanceiro', 0, 'Mover',    2),
                    ('Lanceiro', 1, 'Preparar', 0),
                    ('Lanceiro', 2, 'Atacar',   2)
                ) AS a(""Nome"", ""Ordem"", ""Tipo"", ""Valor"") ON a.""Nome"" = p.""Nome""
                WHERE NOT EXISTS (SELECT 1 FROM ""InimigoAcoes"" x WHERE x.""PersonagemId"" = p.""Id"");
            ");

            // Local: Floresta (boss = Tank, inimigos validos = Esqueleto, Arqueiro, Lanceiro)
            migrationBuilder.Sql(@"
                INSERT INTO ""Locais"" (""Nome"", ""OrdemProgressao"", ""QtdSessoes"", ""ChefeId"", ""InimigosPossiveisIds"", ""ImagemUrl"")
                SELECT 'Floresta', 1, 3,
                       (SELECT ""Id"" FROM ""Personagens"" WHERE ""Nome"" = 'Tank' ORDER BY ""Id"" LIMIT 1),
                       ARRAY(SELECT ""Id"" FROM ""Personagens"" WHERE ""Nome"" IN ('Esqueleto', 'Arqueiro', 'Lanceiro') ORDER BY ""Id""),
                       NULL
                WHERE NOT EXISTS (SELECT 1 FROM ""Locais"" WHERE ""Nome"" = 'Floresta');
            ");

            // Sessoes: 1 e 2 normais, 3 = boss
            migrationBuilder.Sql(@"
                INSERT INTO ""Sessoes"" (""NumeroSessao"", ""TamanhoMapa"", ""LocalId"")
                SELECT v.""Numero"", v.""Tamanho"", l.""Id""
                FROM ""Locais"" l
                CROSS JOIN (VALUES (1, 8), (2, 8), (3, 8)) AS v(""Numero"", ""Tamanho"")
                WHERE l.""Nome"" = 'Floresta'
                  AND NOT EXISTS (SELECT 1 FROM ""Sessoes"" s WHERE s.""LocalId"" = l.""Id"");
            ");

            // Waves: sessao 1 = 3 waves, sessao 2 = 5 waves, sessao 3 (boss) = 1 wave com o chefe
            migrationBuilder.Sql(@"
                INSERT INTO ""Waves"" (""QtdInimigos"", ""SessaoId"")
                SELECT w.""Qtd"", s.""Id""
                FROM ""Sessoes"" s
                JOIN ""Locais"" l ON l.""Id"" = s.""LocalId"" AND l.""Nome"" = 'Floresta'
                JOIN (VALUES
                    (1, 2), (1, 2), (1, 3),
                    (2, 3), (2, 3), (2, 4), (2, 4), (2, 5),
                    (3, 1)
                ) AS w(""Sessao"", ""Qtd"") ON w.""Sessao"" = s.""NumeroSessao""
                WHERE NOT EXISTS (SELECT 1 FROM ""Waves"" x WHERE x.""SessaoId"" = s.""Id"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.Sql(@"
                DELETE FROM ""Waves"" WHERE ""SessaoId"" IN (SELECT s.""Id"" FROM ""Sessoes"" s JOIN ""Locais"" l ON l.""Id"" = s.""LocalId"" WHERE l.""Nome"" = 'Floresta');
                DELETE FROM ""Sessoes"" WHERE ""LocalId"" IN (SELECT ""Id"" FROM ""Locais"" WHERE ""Nome"" = 'Floresta');
                DELETE FROM ""Locais"" WHERE ""Nome"" = 'Floresta';
                DELETE FROM ""Personagens"" WHERE ""Nome"" IN ('Tank', 'Arqueiro', 'Lanceiro');
            ");
            migrationBuilder.DropColumn(
                name: "Vida",
                table: "Personagens");
        }
    }
}
