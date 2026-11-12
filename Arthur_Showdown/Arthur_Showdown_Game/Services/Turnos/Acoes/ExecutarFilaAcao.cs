using Arthur_Showdown_Game.Components.Game;
using Arthur_Showdown_Shared.Dtos.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Services;

public class ExecutarFilaAcao(List<CartaAtaque> todas) : IAcaoDeTurno
{
    public bool GastaTurno => true;

    public async Task ExecutarAsync(BatalhaContexto ctx)
    {
        var fila = todas.Where(c => c.EmFila).OrderBy(c => c.PosicaoFila).ToList();
        if (fila.Count == 0)
        {
            await Task.Delay(150);
            return;
        }

        foreach (var carta in fila)
        {
            var atk = carta.Ataque;
            var nome = atk.Nome ?? string.Empty;
            int posJog = ctx.Jogador.PosicaoPersonagem;
            int olhar = ctx.Jogador.DirecaoOlhar == 0 ? 1 : ctx.Jogador.DirecaoOlhar;

            var alvosAtingidos = new List<InimigoEmCombate>();
            bool acertou = false;
            bool ehFlecha = false;

            if (nome.Equals("Virar", StringComparison.OrdinalIgnoreCase))
            {
                ctx.Jogador.DirecaoOlhar = -olhar;
                olhar = ctx.Jogador.DirecaoOlhar;
                ctx.QtdMudancaDirecao++;
                ctx.TocarSomMovimento?.Invoke();

                if (atk.Dano > 0)
                {
                    var inimigoFrente = ctx.Inimigos.FirstOrDefault(i => i.Vivo && i.Estado.Posicao == posJog + olhar);
                    if (inimigoFrente != null)
                    {
                        inimigoFrente.ReceberDano(atk.Dano);
                        alvosAtingidos.Add(inimigoFrente);
                        acertou = true;
                    }
                }
            }
            else if (nome.Equals("Bomba de fumaça", StringComparison.OrdinalIgnoreCase)
                  || nome.Equals("Bomba de fumaça costas", StringComparison.OrdinalIgnoreCase))
            {
                int dirBusca = nome.Contains("costas", StringComparison.OrdinalIgnoreCase) ? -olhar : olhar;
                var alvoTroca = ctx.Inimigos.FirstOrDefault(i => i.Vivo && i.Estado.Posicao == posJog + dirBusca)
                    ?? ctx.Inimigos
                        .Where(i => i.Vivo && (i.Estado.Posicao - posJog) * dirBusca > 0)
                        .OrderBy(i => Math.Abs(i.Estado.Posicao - posJog))
                        .FirstOrDefault();

                if (alvoTroca != null)
                {
                    int posAntiga = ctx.Jogador.PosicaoPersonagem;
                    ctx.Jogador.PosicaoPersonagem = alvoTroca.Estado.Posicao;
                    alvoTroca.Estado.Posicao = posAntiga;
                    ctx.TocarSomMovimento?.Invoke();
                    alvosAtingidos.Add(alvoTroca);

                    ctx.EfeitoVisual = new EfeitoVisualAtaque
                    {
                        TilesImpacto = new HashSet<int> { posAntiga, ctx.Jogador.PosicaoPersonagem },
                        EhFlecha = false,
                        OrigemTile = posAntiga,
                        DestinoTile = ctx.Jogador.PosicaoPersonagem,
                        Direcao = dirBusca
                    };

                    if (atk.Dano > 0)
                    {
                        alvoTroca.ReceberDano(atk.Dano);
                        acertou = true;
                    }
                }
            }
            else if (nome.Equals("Espelho do lago", StringComparison.OrdinalIgnoreCase))
            {
                var vivos = ctx.Inimigos.Where(i => i.Vivo).ToList();
                alvosAtingidos.AddRange(vivos);
                if (atk.Dano > 0)
                {
                    foreach (var ini in vivos)
                    {
                        ini.ReceberDano(atk.Dano);
                        acertou = true;
                    }
                }
            }
            else if (nome.Equals("Amaldiçoar", StringComparison.OrdinalIgnoreCase))
            {
                var alvoMaldicao = ctx.Inimigos.FirstOrDefault(i => i.Vivo && i.Estado.Posicao == posJog + olhar)
                    ?? ctx.Inimigos
                        .Where(i => i.Vivo && (i.Estado.Posicao - posJog) * olhar > 0)
                        .OrderBy(i => Math.Abs(i.Estado.Posicao - posJog))
                        .FirstOrDefault();

                int tileAlvo = alvoMaldicao?.Estado.Posicao ?? Math.Clamp(posJog + olhar, 0, ctx.TilesTotal - 1);
                ctx.EfeitoVisual = new EfeitoVisualAtaque
                {
                    TilesImpacto = new HashSet<int> { tileAlvo },
                    EhFlecha = false,
                    OrigemTile = posJog,
                    DestinoTile = tileAlvo,
                    Direcao = olhar
                };

                if (alvoMaldicao != null)
                {
                    alvosAtingidos.Add(alvoMaldicao);
                    if (atk.Dano > 0)
                    {
                        alvoMaldicao.ReceberDano(atk.Dano);
                        acertou = true;
                    }
                }
            }
            else if (nome.Equals("Raio", StringComparison.OrdinalIgnoreCase))
            {
                var maisDistante = ctx.Inimigos
                    .Where(i => i.Vivo && (i.Estado.Posicao - posJog) * olhar > 0)
                    .OrderByDescending(i => Math.Abs(i.Estado.Posicao - posJog))
                    .FirstOrDefault()
                    ?? ctx.Inimigos
                        .Where(i => i.Vivo)
                        .OrderByDescending(i => Math.Abs(i.Estado.Posicao - posJog))
                        .FirstOrDefault();

                int tileAlvo = maisDistante?.Estado.Posicao ?? Math.Clamp(posJog + olhar * (ctx.TilesTotal - 1), 0, ctx.TilesTotal - 1);
                ctx.EfeitoVisual = new EfeitoVisualAtaque
                {
                    TilesImpacto = new HashSet<int> { tileAlvo },
                    EhFlecha = false,
                    OrigemTile = posJog,
                    DestinoTile = tileAlvo,
                    Direcao = olhar
                };

                if (maisDistante != null)
                {
                    alvosAtingidos.Add(maisDistante);
                    if (atk.Dano > 0)
                    {
                        maisDistante.ReceberDano(atk.Dano);
                        acertou = true;
                    }
                }
            }
            else
            {
                var hitbox = HitboxHelper.Ler(atk);
                if (nome.Equals("Ataque duplo", StringComparison.OrdinalIgnoreCase))
                {
                    hitbox.Direcao = HitboxDirecao.Both;
                    hitbox.AlcanceMin = 1;
                    hitbox.AlcanceMax = 1;
                }
                else if (nome.Equals("Ataque nas costas", StringComparison.OrdinalIgnoreCase))
                {
                    hitbox.Direcao = HitboxDirecao.Backward;
                    hitbox.AlcanceMin = 1;
                    hitbox.AlcanceMax = 1;
                }

                var tiles = HitboxHelper.Tiles(hitbox, posJog, olhar, ctx.TilesTotal);
                ehFlecha = nome.Contains("Flecha", StringComparison.OrdinalIgnoreCase)
                        || nome.Contains("Arco", StringComparison.OrdinalIgnoreCase)
                        || nome.Contains("Shuriken", StringComparison.OrdinalIgnoreCase)
                        || hitbox.AlcanceMax > 2;

                int destino = Math.Clamp(posJog + olhar * Math.Min(hitbox.AlcanceMax, ctx.TilesTotal - 1), 0, ctx.TilesTotal - 1);
                var primeiroInimigo = ctx.Inimigos
                    .Where(i => i.Vivo && tiles.Contains(i.Estado.Posicao))
                    .OrderBy(i => Math.Abs(i.Estado.Posicao - posJog))
                    .FirstOrDefault();
                if (primeiroInimigo != null) destino = primeiroInimigo.Estado.Posicao;

                ctx.EfeitoVisual = new EfeitoVisualAtaque
                {
                    TilesImpacto = ehFlecha ? new HashSet<int> { destino } : tiles,
                    EhFlecha = ehFlecha,
                    OrigemTile = posJog,
                    DestinoTile = destino,
                    Direcao = olhar
                };

                if (ehFlecha)
                {
                    if (primeiroInimigo != null)
                    {
                        primeiroInimigo.ReceberDano(atk.Dano);
                        alvosAtingidos.Add(primeiroInimigo);
                        acertou = true;
                    }
                }
                else
                {
                    foreach (var inimigo in ctx.Inimigos.Where(i => i.Vivo && tiles.Contains(i.Estado.Posicao)))
                    {
                        inimigo.ReceberDano(atk.Dano);
                        alvosAtingidos.Add(inimigo);
                        acertou = true;
                    }
                }
            }

            // Efeito 3: Escudo (ignora o primeiro dano que o jogador receber)
            if (atk.PossuiEfeito(3))
            {
                ctx.EscudoAtivo = true;
            }

            // Efeito 1: Maldição (alvo recebe o dobro de dano do próximo ataque)
            if (atk.PossuiEfeito(1))
            {
                var alvosMaldicao = alvosAtingidos.Where(i => i.Vivo).ToList();
                if (alvosMaldicao.Count == 0)
                {
                    var frente = ctx.Inimigos.FirstOrDefault(i => i.Vivo && i.Estado.Posicao == ctx.Jogador.PosicaoPersonagem + ctx.Jogador.DirecaoOlhar);
                    if (frente != null) alvosMaldicao.Add(frente);
                }
                foreach (var ini in alvosMaldicao)
                {
                    ini.Amaldicoado = true;
                }
            }

            ctx.NotificarVisual?.Invoke();

            if (acertou)
                ctx.TocarSomHit?.Invoke(ehFlecha);

            // Efeito 2: Espelho (quadrado azul->verde sobre o alvo, vira ele e some)
            if (atk.PossuiEfeito(2))
            {
                var alvosEspelho = nome.Contains("Espelho", StringComparison.OrdinalIgnoreCase)
                    ? ctx.Inimigos.Where(i => i.Vivo).ToList()
                    : alvosAtingidos.Where(i => i.Vivo).ToList();

                if (alvosEspelho.Count == 0)
                {
                    var frente = ctx.Inimigos.FirstOrDefault(i => i.Vivo && i.Estado.Posicao == ctx.Jogador.PosicaoPersonagem + ctx.Jogador.DirecaoOlhar);
                    if (frente != null) alvosEspelho.Add(frente);
                }

                if (alvosEspelho.Count > 0)
                {
                    foreach (var ini in alvosEspelho)
                        ini.EfeitoEspelhoAtivo = true;
                    ctx.NotificarVisual?.Invoke();

                    await Task.Delay(150);

                    foreach (var ini in alvosEspelho)
                    {
                        int dirAtual = ini.Estado.Direcao == 0 ? 1 : ini.Estado.Direcao;
                        ini.Estado.Direcao = -dirAtual;
                    }
                    ctx.NotificarVisual?.Invoke();

                    await Task.Delay(170);

                    foreach (var ini in alvosEspelho)
                        ini.EfeitoEspelhoAtivo = false;
                }
            }

            carta.PosicaoFila = null;
            carta.QtdUsosNaRun++;
            carta.CooldownRestante = atk.Cooldown > 0
                ? atk.Cooldown + 1
                : 0;

            await Task.Delay(260);
            ctx.EfeitoVisual = null;
            ctx.Inimigos.RemoveAll(i => !i.Vivo);
            ctx.NotificarVisual?.Invoke();

            if (ctx.Inimigos.Count == 0)
            {
                await Task.Delay(200);
                break;
            }
        }
    }
}
