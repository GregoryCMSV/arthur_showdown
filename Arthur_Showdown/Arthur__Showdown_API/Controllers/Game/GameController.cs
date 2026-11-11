using Arthur__Showdown_API.Services;
using Arthur_Showdown_Database.Data;
using Arthur_Showdown_Database.Models.Auxiliar;
using Arthur_Showdown_Database.Models.Base;
using Arthur_Showdown_Database.Models.Run;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                return Unauthorized("Usuário inválido no token.");

            var jogoAtivo = await _db.Games
                .Where(j => j.JogadorId.ToString() == userId && j.DataFim == null)
                .Select(j => j.Id)
                .FirstOrDefaultAsync();

            if (jogoAtivo == 0)
                return Ok(new CurrentGameResponse { GameID = null });

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

            var local = await _db.Locais
                .OrderBy(l => l.OrdemProgressao)
                .ThenBy(l => l.Id)
                .FirstOrDefaultAsync();

            var sessao = local != null
                ? await _db.Sessoes.Where(s => s.LocalId == local.Id).OrderBy(s => s.NumeroSessao).FirstOrDefaultAsync()
                : null;

            var game = new Arthur_Showdown_Database.Models.Run.Game
            {
                JogadorId = jogadorId,
                DataInicio = DateTime.UtcNow,
                StatusRunId = 1,
                LocalAtualId = local?.Id ?? 1,
                SessaoAtualId = sessao?.Id,
                WaveNumero = 1
            };
            _db.Games.Add(game);
            await _db.SaveChangesAsync();

            var pr = new PersonagemRun
            {
                GameId = game.Id,
                PersonagemId = personagem.Id,
                VidaMaxima = 10,
                VidaAtual = 10,
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
                IsFastAction = pa.Ataque.IsFastAction,
                CooldownRestante = 0,
                PosicaoFila = null
            }));

            await SpawnWaveAsync(game, pr);

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(new CriarGameResponse { GameId = game.Id });
        }

        [HttpGet("{id:int}/estado")]
        public async Task<IActionResult> GetEstado(int id, [FromQuery] bool resetCooldown = false)
        {
            var userId = _tokenService.ObterUsuarioId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Usuário inválido no token.");
            var jogadorId = Guid.Parse(userId);

            var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == id && g.JogadorId == jogadorId);
            if (game is null) return NotFound();

            if (resetCooldown && game.DataFim == null)
            {
                var pr = await _db.PersonagensRun.FirstOrDefaultAsync(p => p.GameId == id);
                if (pr != null)
                {
                    var deck = await _db.DecksAtaques.Where(d => d.PersonagemRunId == pr.Id).ToListAsync();
                    foreach (var d in deck)
                    {
                        d.CooldownRestante = 0;
                        d.PosicaoFila = null;
                    }
                    await _db.SaveChangesAsync();
                }
            }

            return Ok(await MontarEstadoAsync(game));
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
            game.TotalOuroGanho = Math.Max(game.TotalOuroGanho, req.TotalOuroGanho);
            game.TempoSegundos = Math.Max(game.TempoSegundos, req.TempoSegundos);
            game.QtdMudancaDirecao = Math.Max(game.QtdMudancaDirecao, req.QtdMudancaDirecao);
            game.QtdHitsSofridos = Math.Max(game.QtdHitsSofridos, req.QtdHitsSofridos);

            var pr = await _db.PersonagensRun.FirstAsync(p => p.GameId == id);
            pr.VidaAtual = req.VidaAtual;
            pr.Posicao = req.Posicao;
            pr.DirecaoOlhar = req.DirecaoOlhar;

            var deck = await _db.DecksAtaques.Where(d => d.PersonagemRunId == pr.Id).ToListAsync();
            foreach (var c in req.Deck)
            {
                var d = deck.FirstOrDefault(x => x.Id == c.DeckAtaquesId);
                if (d != null)
                {
                    d.CooldownRestante = c.CooldownRestante;
                    d.PosicaoFila = c.PosicaoFila;
                    d.QtdUsosNaRun = Math.Max(d.QtdUsosNaRun, c.QtdUsosNaRun);
                    d.QtdVezesAdicionadoFila = Math.Max(d.QtdVezesAdicionadoFila, c.QtdVezesAdicionadoFila);
                    d.QtdVezesRemovidoFila = Math.Max(d.QtdVezesRemovidoFila, c.QtdVezesRemovidoFila);
                }
            }

            _db.InimigosRun.RemoveRange(_db.InimigosRun.Where(i => i.GameId == id));

            var vivos = req.Inimigos.Where(i => i.VidaAtual > 0).ToList();
            if (vivos.Count > 0 || pr.VidaAtual <= 0)
            {
                _db.InimigosRun.AddRange(vivos.Select(i => new InimigoRun
                {
                    GameId = id,
                    PersonagemId = i.PersonagemId,
                    VidaAtual = i.VidaAtual,
                    Posicao = i.Posicao,
                    CursorAcao = i.CursorAcao,
                    DirecaoOlhar = i.Direcao == 0 ? 1 : i.Direcao,
                    Preparado = i.Preparado,
                    Telegrafando = i.Telegrafando,
                    RecuoPendente = i.RecuoPendente
                }));
            }
            else
            {
                await AvancarWaveOuSessaoAsync(game, pr, deck);
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return Ok(await MontarEstadoAsync(game));
        }

        [HttpPost("{id:int}/melhoria")]
        public async Task<IActionResult> AplicarMelhoria(int id, [FromBody] AplicarMelhoriaRequest req)
        {
            var userId = _tokenService.ObterUsuarioId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Usuário inválido no token.");
            var jogadorId = Guid.Parse(userId);

            var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == id && g.JogadorId == jogadorId && g.DataFim == null);
            if (game is null) return NotFound();

            var pr = await _db.PersonagensRun.FirstAsync(p => p.GameId == id);
            var item = await _db.DecksAtaques
                .Include(d => d.Ataque)
                .FirstOrDefaultAsync(d => d.Id == req.DeckAtaquesId && d.PersonagemRunId == pr.Id);

            if (item != null && item.Ataque != null)
            {
                switch (req.Tipo)
                {
                    case TipoMelhoriaAtaque.MaisUmDano:
                        item.ModDano += 1;
                        break;
                    case TipoMelhoriaAtaque.MaisDoisDanoMaisUmCooldown:
                        item.ModDano += 2;
                        item.ModCooldown += 1;
                        break;
                    case TipoMelhoriaAtaque.MenosUmCooldown:
                        int cdAtual = item.Ataque.Cooldown + item.ModCooldown;
                        if (cdAtual > 0) item.ModCooldown -= 1;
                        break;
                    case TipoMelhoriaAtaque.AcaoRapida:
                        item.IsFastAction = true;
                        break;
                }
                item.NumeroDeUpgrades++;
                await _db.SaveChangesAsync();
            }

            return Ok(await MontarEstadoAsync(game));
        }

        [HttpGet("{id:int}/opcoes-ataque")]
        public async Task<IActionResult> ObterOpcoesNovoAtaque(int id)
        {
            var userId = _tokenService.ObterUsuarioId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Usuário inválido no token.");
            var jogadorId = Guid.Parse(userId);

            var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == id && g.JogadorId == jogadorId && g.DataFim == null);
            if (game is null) return NotFound();

            var pr = await _db.PersonagensRun.FirstAsync(p => p.GameId == id);
            var jaNoDeck = await _db.DecksAtaques
                .Where(d => d.PersonagemRunId == pr.Id)
                .Select(d => d.AtaqueId)
                .ToListAsync();

            var todosAtaques = await _db.Ataques.AsNoTracking().ToListAsync();
            var disponiveis = todosAtaques.Where(a => !jaNoDeck.Contains(a.Id)).ToList();
            if (disponiveis.Count < 2) disponiveis = todosAtaques;

            var escolhidos = disponiveis
                .OrderBy(_ => Random.Shared.Next())
                .Take(2)
                .Select(AtaqueMapper.ToDto)
                .ToList();

            return Ok(escolhidos);
        }

        [HttpPost("{id:int}/novo-ataque")]
        public async Task<IActionResult> EscolherNovoAtaque(int id, [FromBody] EscolherNovoAtaqueRequest req)
        {
            var userId = _tokenService.ObterUsuarioId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized("Usuário inválido no token.");
            var jogadorId = Guid.Parse(userId);

            var game = await _db.Games.FirstOrDefaultAsync(g => g.Id == id && g.JogadorId == jogadorId && g.DataFim == null);
            if (game is null) return NotFound();

            var pr = await _db.PersonagensRun.FirstAsync(p => p.GameId == id);
            var ataque = await _db.Ataques.FindAsync(req.AtaqueId);
            if (ataque != null)
            {
                _db.DecksAtaques.Add(new DeckAtaques
                {
                    PersonagemRunId = pr.Id,
                    AtaqueId = ataque.Id,
                    LimiteUpgrade = ataque.LimiteAprimoramento,
                    IsFastAction = ataque.IsFastAction,
                    CooldownRestante = 0,
                    PosicaoFila = null
                });
                await _db.SaveChangesAsync();
            }

            return Ok(await MontarEstadoAsync(game));
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
                return Unauthorized("Usuário inválido no token.");

            var jogoAtivo = await _db.Games
                .FirstOrDefaultAsync(j => j.JogadorId.ToString() == userId && j.DataFim == null);

            if (jogoAtivo == null)
                return NotFound(new { Mensagem = "Nenhum jogo ativo encontrado para este usuário." });

            jogoAtivo.DataFim = DateTime.UtcNow;
            jogoAtivo.StatusRunId = 4;
            await _db.SaveChangesAsync();

            return Ok(new { Mensagem = "Jogo abandonado com sucesso." });
        }

        private async Task AvancarWaveOuSessaoAsync(Arthur_Showdown_Database.Models.Run.Game game, PersonagemRun pr, List<DeckAtaques> deck)
        {
            var local = await _db.Locais.FindAsync(game.LocalAtualId);
            if (local == null) return;

            var sessoes = await _db.Sessoes
                .Where(s => s.LocalId == local.Id)
                .OrderBy(s => s.NumeroSessao)
                .ToListAsync();

            var sessao = sessoes.FirstOrDefault(s => s.Id == game.SessaoAtualId) ?? sessoes.FirstOrDefault();
            if (sessao == null) return;

            var waves = await _db.Waves
                .Where(w => w.SessaoId == sessao.Id)
                .OrderBy(w => w.Id)
                .ToListAsync();

            int proxWave = (game.WaveNumero <= 0 ? 1 : game.WaveNumero) + 1;
            if (proxWave <= waves.Count)
            {
                game.SessaoAtualId = sessao.Id;
                game.WaveNumero = proxWave;
                await SpawnWaveAsync(game, pr);
                return;
            }

            int idxSessao = sessoes.IndexOf(sessao);
            if (idxSessao + 1 < sessoes.Count)
            {
                var novaSessao = sessoes[idxSessao + 1];
                game.SessaoAtualId = novaSessao.Id;
                game.WaveNumero = 1;
                pr.Posicao = Math.Clamp(pr.Posicao, 0, Math.Max(0, novaSessao.TamanhoMapa - 1));

                // Ao trocar de sessao, o cooldown dos ataques vem sempre liberado
                foreach (var d in deck)
                {
                    d.CooldownRestante = 0;
                    d.PosicaoFila = null;
                }

                await SpawnWaveAsync(game, pr);
                return;
            }

            game.DataFim = DateTime.UtcNow;
            game.StatusRunId = 2;
        }

        private async Task SpawnWaveAsync(Arthur_Showdown_Database.Models.Run.Game game, PersonagemRun pr)
        {
            var local = await _db.Locais.FindAsync(game.LocalAtualId);
            var sessoes = local != null
                ? await _db.Sessoes.Where(s => s.LocalId == local.Id).OrderBy(s => s.NumeroSessao).ToListAsync()
                : new List<Sessao>();

            var sessao = sessoes.FirstOrDefault(s => s.Id == game.SessaoAtualId) ?? sessoes.FirstOrDefault();
            if (sessao != null && game.SessaoAtualId == null)
            {
                game.SessaoAtualId = sessao.Id;
                if (game.WaveNumero <= 0) game.WaveNumero = 1;
            }

            int tamanho = sessao?.TamanhoMapa > 0 ? sessao.TamanhoMapa : 5;
            bool ehBoss = local != null && sessao != null && sessao.NumeroSessao == local.QtdSessoes && local.ChefeId.HasValue;

            var geradosIds = new List<int>();

            if (ehBoss)
            {
                var chefe = await _db.Personagens.FindAsync(local!.ChefeId!.Value);
                if (chefe != null)
                {
                    int posBoss = pr.Posicao == tamanho - 1 ? 0 : tamanho - 1;
                    int dirBoss = Math.Sign(pr.Posicao - posBoss);
                    if (dirBoss == 0) dirBoss = -1;

                    _db.InimigosRun.Add(new InimigoRun
                    {
                        GameId = game.Id,
                        PersonagemId = chefe.Id,
                        VidaAtual = chefe.Vida > 0 ? chefe.Vida : 20,
                        Posicao = posBoss,
                        CursorAcao = 0,
                        DirecaoOlhar = dirBoss
                    });
                    geradosIds.Add(chefe.Id);
                    await RegistrarWaveAtualAsync(game, geradosIds);
                    return;
                }
            }

            int qtd = 1;
            if (sessao != null)
            {
                var waves = await _db.Waves.Where(w => w.SessaoId == sessao.Id).OrderBy(w => w.Id).ToListAsync();
                int idx = Math.Clamp((game.WaveNumero <= 0 ? 1 : game.WaveNumero) - 1, 0, Math.Max(0, waves.Count - 1));
                if (waves.Count > 0) qtd = waves[idx].QtdInimigos;
            }

            var poolIds = local?.InimigosPossiveisIds ?? new List<int>();
            var pool = poolIds.Count > 0
                ? await _db.Personagens.Where(p => poolIds.Contains(p.Id)).ToListAsync()
                : await _db.Personagens.Where(p => p.TipoPersonagemId == 2 && p.Id != (local != null ? local.ChefeId : null)).ToListAsync();

            if (pool.Count == 0)
            {
                var fallback = await _db.Personagens.Where(p => p.TipoPersonagemId == 2).OrderBy(p => p.Id).FirstOrDefaultAsync();
                if (fallback != null) pool.Add(fallback);
            }
            if (pool.Count == 0) return;

            var livres = Enumerable.Range(0, tamanho)
                .Where(t => t != pr.Posicao)
                .OrderByDescending(t => Math.Abs(t - pr.Posicao))
                .ToList();

            int totalSpawn = Math.Min(qtd, livres.Count);
            for (int i = 0; i < totalSpawn; i++)
            {
                var escolhido = pool[Random.Shared.Next(pool.Count)];
                _db.InimigosRun.Add(new InimigoRun
                {
                    GameId = game.Id,
                    PersonagemId = escolhido.Id,
                    VidaAtual = escolhido.Vida > 0 ? escolhido.Vida : 3,
                    Posicao = livres[i],
                    CursorAcao = 0,
                    DirecaoOlhar = Random.Shared.Next(2) == 0 ? -1 : 1
                });
                geradosIds.Add(escolhido.Id);
            }

            await RegistrarWaveAtualAsync(game, geradosIds);
        }

        private async Task RegistrarWaveAtualAsync(Arthur_Showdown_Database.Models.Run.Game game, List<int> inimigosIds)
        {
            var wa = new WaveAtual
            {
                GameId = game.Id,
                InimigosGeradosIds = inimigosIds
            };
            _db.WavesAtuais.Add(wa);
            await _db.SaveChangesAsync();
            game.WaveAtualId = wa.Id;
        }

        private async Task<EstadoJogoDto> MontarEstadoAsync(Arthur_Showdown_Database.Models.Run.Game game)
        {
            var pr = await _db.PersonagensRun.FirstAsync(p => p.GameId == game.Id);

            var local = await _db.Locais.FindAsync(game.LocalAtualId);
            var sessoes = local != null
                ? await _db.Sessoes.Where(s => s.LocalId == local.Id).OrderBy(s => s.NumeroSessao).ToListAsync()
                : new List<Sessao>();
            var sessao = sessoes.FirstOrDefault(s => s.Id == game.SessaoAtualId) ?? sessoes.FirstOrDefault();

            var inimigosRun = await _db.InimigosRun
                .Include(i => i.Personagem)
                .Where(i => i.GameId == game.Id)
                .OrderBy(i => i.Id)
                .ToListAsync();

            if (sessao != null && game.SessaoAtualId == null)
            {
                game.SessaoAtualId = sessao.Id;
                game.WaveNumero = 1;
                if (inimigosRun.Count == 0 && game.DataFim == null)
                {
                    await SpawnWaveAsync(game, pr);
                    await _db.SaveChangesAsync();
                    inimigosRun = await _db.InimigosRun
                        .Include(i => i.Personagem)
                        .Where(i => i.GameId == game.Id)
                        .OrderBy(i => i.Id)
                        .ToListAsync();
                }
                else
                {
                    await _db.SaveChangesAsync();
                }
            }

            int totalWaves = sessao != null
                ? await _db.Waves.CountAsync(w => w.SessaoId == sessao.Id)
                : 1;

            var deck = await _db.DecksAtaques
                .Include(d => d.Ataque)
                .Where(d => d.PersonagemRunId == pr.Id)
                .OrderBy(d => d.Id)
                .ToListAsync();

            var personagemIds = inimigosRun.Select(i => i.PersonagemId).Distinct().ToList();
            var acoes = await _db.InimigoAcoes
                .Include(a => a.Ataque)
                .Where(a => personagemIds.Contains(a.PersonagemId))
                .OrderBy(a => a.Ordem)
                .ToListAsync();

            return new EstadoJogoDto
            {
                GameId = game.Id,
                OuroAtual = game.OuroAtual,
                TotalOuroGanho = game.TotalOuroGanho,
                LocalAtualId = game.LocalAtualId,
                Turno = game.TurnosJogados,
                TempoSegundos = game.TempoSegundos,
                QtdMudancaDirecao = game.QtdMudancaDirecao,
                QtdHitsSofridos = game.QtdHitsSofridos,
                VidaAtual = pr.VidaAtual,
                VidaMaxima = pr.VidaMaxima,
                Posicao = pr.Posicao,
                DirecaoOlhar = pr.DirecaoOlhar == 0 ? 1 : pr.DirecaoOlhar,
                TamanhoMapa = sessao?.TamanhoMapa > 0 ? sessao.TamanhoMapa : 5,
                SessaoNumero = sessao?.NumeroSessao ?? 1,
                TotalSessoes = local?.QtdSessoes ?? Math.Max(1, sessoes.Count),
                WaveNumero = game.WaveNumero <= 0 ? 1 : game.WaveNumero,
                TotalWaves = Math.Max(1, totalWaves),
                Vitoria = game.StatusRunId == 2,
                Deck = deck.Select(d => new CartaEstadoDto
                {
                    DeckAtaquesId = d.Id,
                    CooldownRestante = d.CooldownRestante,
                    PosicaoFila = d.PosicaoFila,
                    QtdUsosNaRun = d.QtdUsosNaRun,
                    QtdVezesAdicionadoFila = d.QtdVezesAdicionadoFila,
                    QtdVezesRemovidoFila = d.QtdVezesRemovidoFila,
                    Ataque = AtaqueMapper.ToDto(d)
                }).ToList(),
                Inimigos = inimigosRun.Select(i => new InimigoEstadoDto
                {
                    PersonagemId = i.PersonagemId,
                    Nome = i.Personagem?.Nome ?? "Inimigo",
                    ImagemUrl = i.Personagem?.ImagemUrl,
                    VidaAtual = i.VidaAtual,
                    Posicao = i.Posicao,
                    CursorAcao = i.CursorAcao,
                    Direcao = i.DirecaoOlhar == 0 ? 1 : i.DirecaoOlhar,
                    Preparado = i.Preparado,
                    Telegrafando = i.Telegrafando,
                    RecuoPendente = i.RecuoPendente,
                    Acoes = acoes.Where(a => a.PersonagemId == i.PersonagemId)
                        .Select(a => new InimigoAcaoDto
                        {
                            Ordem = a.Ordem,
                            Tipo = a.Tipo.ToString(),
                            Valor = a.Valor,
                            Ataque = a.Ataque == null ? null : AtaqueMapper.ToDto(a.Ataque)
                        }).ToList()
                }).ToList()
            };
        }
    }
}
