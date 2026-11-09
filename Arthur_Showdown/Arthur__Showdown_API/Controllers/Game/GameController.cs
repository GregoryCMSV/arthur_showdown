using Arthur__Showdown_API.Services;
using Arthur_Showdown_Database.Data;
/* inicio flag */
using Arthur_Showdown_Database.Models.Base;
using Arthur_Showdown_Database.Models.Run;
/*fim flag*/
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Arthur__Showdown_API.Controllers.Game
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class GameController : ControllerBase
    {
        private readonly ArthurShowdownContext _db;
        private readonly TokenService _tokenService;

        public GameController(ArthurShowdownContext db, TokenService tokenService)
        {
            _db = db;
            _tokenService = tokenService;
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrentGame()
        {
            var userId = _tokenService.ObterUsuarioId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Usuário inválido no token.");
            }

            var jogoAtivo = await _db.Games
                .Where(j => j.JogadorId.ToString() == userId && j.DataFim == null)
                .Select(j => j.Id) 
                .FirstOrDefaultAsync();

            if (jogoAtivo == 0) 
            {
                return Ok(new CurrentGameResponse { GameID = null });
            }

            return Ok(new CurrentGameResponse { GameID = jogoAtivo });
        }

        [HttpPost]
        public async Task<IActionResult> Criar([FromBody] CriarGameRequest req)
        {
            var userId = _tokenService.ObterUsuarioId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Usuário inválido no token.");
            var jogadorId = Guid.Parse(userId);

            if (await _db.Games.AnyAsync(g => g.JogadorId == jogadorId && g.DataFim == null))
                return Conflict(new { Mensagem = "Já existe um jogo ativo." });

            var personagem = await _db.Personagens.FindAsync(req.PersonagemId);
            if (personagem is null) return NotFound(new { Mensagem = "Personagem não encontrado." });

            await using var tx = await _db.Database.BeginTransactionAsync();

            var game = new Arthur_Showdown_Database.Models.Run.Game
            {
                JogadorId = jogadorId,
                DataInicio = DateTime.UtcNow,
                StatusRunId = 1,   
                LocalAtualId = 1
            };
            _db.Games.Add(game);
            await _db.SaveChangesAsync();

            var pr = new PersonagemRun
            {
                GameId = game.Id,
                PersonagemId = personagem.Id,
                VidaMaxima = 100,
                VidaAtual = 100,
                Posicao = 1,
                DirecaoOlhar = 1
            };
            _db.PersonagensRun.Add(pr);
            await _db.SaveChangesAsync();

            var iniciais = await _db.PersonagemAtaques
                .Include(pa => pa.Ataque)
                .Where(pa => pa.PersonagemId == personagem.Id)
                .ToListAsync();

            _db.DecksAtaques.AddRange(iniciais.Select(pa => new DeckAtaques
            {
                PersonagemRunId = pr.Id,
                AtaqueId = pa.AtaqueId,
                LimiteUpgrade = pa.Ataque!.LimiteAprimoramento,
                IsFastAction = pa.Ataque.IsFastAction
            }));

            var inimigo = await _db.Personagens
                .Where(p => p.TipoPersonagemId == 2)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();
            if (inimigo != null)
            {
                _db.InimigosRun.Add(new InimigoRun
                {
                    GameId = game.Id,
                    PersonagemId = inimigo.Id,
                    VidaAtual = 3,
                    Posicao = 4,
                    CursorAcao = 0
                });
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new CriarGameResponse { GameId = game.Id });
        }

        [HttpGet("{id:int}/estado")]
        public async Task<IActionResult> GetEstado(int id)
        {
            var userId = _tokenService.ObterUsuarioId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Usuário inválido no token.");
            var jogadorId = Guid.Parse(userId);

            var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == id && g.JogadorId == jogadorId);
            if (game is null) return NotFound();

            var pr = await _db.PersonagensRun.FirstAsync(p => p.GameId == id);

            var deck = await _db.DecksAtaques
                .Include(d => d.Ataque)
                .Where(d => d.PersonagemRunId == pr.Id)
                .OrderBy(d => d.Id)
                .ToListAsync();

            var inimigosRun = await _db.InimigosRun
                .Include(i => i.Personagem)
                .Where(i => i.GameId == id)
                .OrderBy(i => i.Id)
                .ToListAsync();

            var personagemIds = inimigosRun.Select(i => i.PersonagemId).Distinct().ToList();
            var acoes = await _db.InimigoAcoes
                .Include(a => a.Ataque)
                .Where(a => personagemIds.Contains(a.PersonagemId))
                .OrderBy(a => a.Ordem)
                .ToListAsync();

            return Ok(new EstadoJogoDto
            {
                GameId = id,
                OuroAtual = game.OuroAtual,
                LocalAtualId = game.LocalAtualId,
                Turno = game.TurnosJogados,
                VidaAtual = pr.VidaAtual,
                VidaMaxima = pr.VidaMaxima,
                Posicao = pr.Posicao,
                DirecaoOlhar = pr.DirecaoOlhar == 0 ? 1 : pr.DirecaoOlhar,
                Deck = deck.Select(d => new CartaEstadoDto
                {
                    DeckAtaquesId = d.Id,
                    CooldownRestante = d.CooldownRestante,
                    Ataque = AtaqueMapper.ToDto(d.Ataque!)
                }).ToList(),
                Inimigos = inimigosRun.Select(i => new InimigoEstadoDto
                {
                    PersonagemId = i.PersonagemId,
                    Nome = i.Personagem?.Nome ?? "Inimigo",
                    ImagemUrl = i.Personagem?.ImagemUrl,
                    VidaAtual = i.VidaAtual,
                    Posicao = i.Posicao,
                    CursorAcao = i.CursorAcao,
                    Acoes = acoes.Where(a => a.PersonagemId == i.PersonagemId)
                        .Select(a => new InimigoAcaoDto
                        {
                            Ordem = a.Ordem,
                            Tipo = a.Tipo.ToString(),
                            Valor = a.Valor,
                            Ataque = a.Ataque == null ? null : AtaqueMapper.ToDto(a.Ataque)
                        }).ToList()
                }).ToList()
            });
        }

        [HttpPut("{id:int}/estado")]
        public async Task<IActionResult> Salvar(int id, [FromBody] EstadoJogoDto req)
        {
            var userId = _tokenService.ObterUsuarioId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Usuário inválido no token.");
            var jogadorId = Guid.Parse(userId);

            var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == id && g.JogadorId == jogadorId && g.DataFim == null);
            if (game is null) return NotFound();

            await using var tx = await _db.Database.BeginTransactionAsync();

            game.TurnosJogados = req.Turno;
            game.OuroAtual = req.OuroAtual;

            var pr = await _db.PersonagensRun.FirstAsync(p => p.GameId == id);
            pr.VidaAtual = req.VidaAtual;
            pr.Posicao = req.Posicao;
            pr.DirecaoOlhar = req.DirecaoOlhar;

            var deck = await _db.DecksAtaques.Where(d => d.PersonagemRunId == pr.Id).ToListAsync();
            foreach (var c in req.Deck)
            {
                var d = deck.FirstOrDefault(x => x.Id == c.DeckAtaquesId);
                if (d != null) d.CooldownRestante = c.CooldownRestante;
            }

            _db.InimigosRun.RemoveRange(_db.InimigosRun.Where(i => i.GameId == id));
            _db.InimigosRun.AddRange(req.Inimigos.Select(i => new InimigoRun
            {
                GameId = id,
                PersonagemId = i.PersonagemId,
                VidaAtual = i.VidaAtual,
                Posicao = i.Posicao,
                CursorAcao = i.CursorAcao
            }));

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/finalizar")]
        public async Task<IActionResult> Finalizar(int id, [FromQuery] bool vitoria = false)
        {
            var userId = _tokenService.ObterUsuarioId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Usuário inválido no token.");
            var jogadorId = Guid.Parse(userId);

            var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == id && g.JogadorId == jogadorId && g.DataFim == null);
            if (game is null) return NotFound();

            game.DataFim = DateTime.UtcNow;
            game.StatusRunId = vitoria ? 2 : 3;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("abandon")]
        public async Task<IActionResult> AbandonCurrentGame()
        {
            var userId = _tokenService.ObterUsuarioId();

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Usuário inválido no token.");
            }

            var jogoAtivo = await _db.Games
                .FirstOrDefaultAsync(j => j.JogadorId.ToString() == userId && j.DataFim == null);

            if (jogoAtivo == null)
            {
                return NotFound(new { Mensagem = "Nenhum jogo ativo encontrado para este usuário." });
            }

            jogoAtivo.DataFim = DateTime.UtcNow;
            jogoAtivo.StatusRunId = 4; 
            await _db.SaveChangesAsync();

            return Ok(new { Mensagem = "Jogo abandonado com sucesso." });
        }
    }
}
