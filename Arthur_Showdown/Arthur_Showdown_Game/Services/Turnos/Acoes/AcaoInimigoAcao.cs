using Arthur_Showdown_Shared.Dtos.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Services;

public class AcaoInimigoAcao(InimigoEmCombate inimigo) : IAcaoDeTurno
{
    public bool GastaTurno => false;
    public async Task ExecutarAsync(BatalhaContexto ctx)
    {
        if (!inimigo.Vivo) return;

        var posJogador = ctx.Jogador.PosicaoPersonagem;
        int rumoAtual = InimigoIA.Rumo(inimigo, posJogador);
        int dirIntencao = inimigo.DirecaoIntencao != 0 ? inimigo.DirecaoIntencao : rumoAtual;
        var decisao = inimigo.IntencaoPlanejada ?? InimigoIA.Decidir(inimigo, posJogador, ctx.Inimigos, ctx.TilesTotal);

        if (inimigo.Telegrafando)
        {
            decisao = DecisaoInimigo.Atacar;
        }
        else if (decisao != DecisaoInimigo.Recuar && decisao != DecisaoInimigo.Virar && inimigo.Estado.Direcao != dirIntencao)
        {
            // Caso o inimigo tenha sido virado por Espelho antes de telegrafar, ele gasta o turno virando
            decisao = DecisaoInimigo.Virar;
        }

        switch (decisao)
        {
            case DecisaoInimigo.Virar:
                inimigo.Estado.Direcao = rumoAtual;
                inimigo.Telegrafando = false;
                break;

            case DecisaoInimigo.Preparar:
                inimigo.Preparado = true;
                inimigo.Telegrafando = false;
                break;

            case DecisaoInimigo.Mover:
                if (InimigoIA.PodeAndar(inimigo, dirIntencao, posJogador, ctx.Inimigos, ctx.TilesTotal))
                {
                    inimigo.Estado.Posicao += dirIntencao;
                    ctx.TocarSomMovimento?.Invoke();
                }
                inimigo.Telegrafando = false;
                break;

            case DecisaoInimigo.Recuar:
                int passos = inimigo.RecuoPendente ? inimigo.RecuoAposAtaque : inimigo.RecuoSeProximo;
                inimigo.RecuoPendente = false;
                inimigo.Telegrafando = false;
                bool moveu = false;
                for (int i = 0; i < Math.Max(1, passos); i++)
                {
                    if (!InimigoIA.PodeAndar(inimigo, -dirIntencao, posJogador, ctx.Inimigos, ctx.TilesTotal)) break;
                    inimigo.Estado.Posicao -= dirIntencao;
                    moveu = true;
                }
                if (moveu) ctx.TocarSomMovimento?.Invoke();
                break;

            case DecisaoInimigo.Telegrafar:
                inimigo.Preparado = true;
                inimigo.Telegrafando = true;
                break;

            case DecisaoInimigo.Esperar:
                inimigo.RecuoPendente = false;
                inimigo.Telegrafando = false;
                break;

            case DecisaoInimigo.Atacar:
                int dirAtaque = inimigo.Estado.Direcao == 0 ? rumoAtual : inimigo.Estado.Direcao;
                var hitbox = HitboxHelper.Ler(inimigo.Ataque);
                var tiles = HitboxHelper.Tiles(hitbox, inimigo.Estado.Posicao, dirAtaque, ctx.TilesTotal);
                bool ehFlecha = hitbox.AlcanceMax > 2
                             || inimigo.Estado.Nome.Contains("Arqueiro", StringComparison.OrdinalIgnoreCase)
                             || (inimigo.Ataque?.Nome.Contains("Flecha", StringComparison.OrdinalIgnoreCase) ?? false);

                bool acertou = false;

                if (ehFlecha)
                {
                    int distJog = tiles.Contains(posJogador) ? Math.Abs(posJogador - inimigo.Estado.Posicao) : int.MaxValue;
                    var inimigoAlvo = ctx.Inimigos
                        .Where(o => o != inimigo && o.Vivo && tiles.Contains(o.Estado.Posicao))
                        .OrderBy(o => Math.Abs(o.Estado.Posicao - inimigo.Estado.Posicao))
                        .FirstOrDefault();
                    int distIni = inimigoAlvo != null ? Math.Abs(inimigoAlvo.Estado.Posicao - inimigo.Estado.Posicao) : int.MaxValue;

                    int destinoFlecha = Math.Clamp(inimigo.Estado.Posicao + dirAtaque * Math.Min(hitbox.AlcanceMax, ctx.TilesTotal - 1), 0, ctx.TilesTotal - 1);
                    if (distIni <= distJog && inimigoAlvo != null)
                        destinoFlecha = inimigoAlvo.Estado.Posicao;
                    else if (distJog < distIni)
                        destinoFlecha = posJogador;

                    ctx.EfeitoVisual = new EfeitoVisualAtaque
                    {
                        TilesImpacto = new HashSet<int> { destinoFlecha },
                        EhFlecha = true,
                        OrigemTile = inimigo.Estado.Posicao,
                        DestinoTile = destinoFlecha,
                        Direcao = dirAtaque
                    };
                    ctx.NotificarVisual?.Invoke();

                    if (distIni < distJog && inimigoAlvo != null)
                    {
                        int aplicado = inimigoAlvo.ReceberDano(inimigo.Dano);
                        ctx.RegistrarDanoFlutuante(inimigoAlvo.Estado.Posicao, aplicado);
                        acertou = true;
                    }
                    else if (distJog < distIni)
                    {
                        AplicarDanoNoJogador(ctx, inimigo.Dano);
                        acertou = true;
                    }
                }
                else
                {
                    ctx.EfeitoVisual = new EfeitoVisualAtaque
                    {
                        TilesImpacto = tiles,
                        EhFlecha = false,
                        OrigemTile = inimigo.Estado.Posicao,
                        DestinoTile = posJogador,
                        Direcao = dirAtaque
                    };
                    ctx.NotificarVisual?.Invoke();

                    if (tiles.Contains(posJogador))
                    {
                        AplicarDanoNoJogador(ctx, inimigo.Dano);
                        acertou = true;
                    }

                    foreach (var outro in ctx.Inimigos.Where(o => o != inimigo && o.Vivo && tiles.Contains(o.Estado.Posicao)))
                    {
                        int aplicado = outro.ReceberDano(inimigo.Dano);
                        ctx.RegistrarDanoFlutuante(outro.Estado.Posicao, aplicado);
                        acertou = true;
                    }
                }

                if (acertou)
                    ctx.TocarSomHit?.Invoke(ehFlecha);

                inimigo.Preparado = false;
                inimigo.Telegrafando = false;
                inimigo.RecuoPendente = inimigo.RecuoAposAtaque > 0;

                await Task.Delay(280);
                ctx.EfeitoVisual = null;
                ctx.TremendoTela = false;
                ctx.Inimigos.RemoveAll(i => !i.Vivo);

                // Mecanica do Chefe: apos atacar, se so tiver ele vivo na arena, invoca 2 inimigos
                if (inimigo.Vivo && inimigo.InvocarAposAtaque > 0 && ctx.VidaAtual > 0)
                {
                    if (ctx.Inimigos.Count(i => i.Vivo) == 1)
                    {
                        InvocarInimigosDoChefe(ctx, inimigo.InvocarAposAtaque);
                    }
                }

                ctx.NotificarVisual?.Invoke();
                return;
        }

        await Task.Delay(250);
    }

    private static void AplicarDanoNoJogador(BatalhaContexto ctx, int dano)
    {
        if (ctx.EscudoAtivo)
        {
            ctx.EscudoAtivo = false;
            return;
        }
        if (dano <= 0) return;

        ctx.VidaAtual = ctx.ImpedirMorteJogador
            ? Math.Max(1, ctx.VidaAtual - dano)
            : Math.Max(0, ctx.VidaAtual - dano);
        ctx.QtdHitsSofridos++;
        ctx.TremendoTela = true;
        ctx.UltimoDanoJogador = dano;
        ctx.DanoJogadorId++;
        ctx.RegistrarDanoFlutuante(ctx.Jogador.PosicaoPersonagem, dano);
    }

    private static void InvocarInimigosDoChefe(BatalhaContexto ctx, int quantidade)
    {
        var posJogador = ctx.Jogador.PosicaoPersonagem;
        var livres = Enumerable.Range(0, ctx.TilesTotal)
            .Where(t => t != posJogador && !ctx.Inimigos.Any(o => o.Vivo && o.Estado.Posicao == t))
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        if (livres.Count == 0 || ctx.PoolInimigosInvocacao.Count == 0) return;

        int total = Math.Min(quantidade, livres.Count);
        var posInvocados = new List<int>();
        for (int i = 0; i < total; i++)
        {
            var modelo = ctx.PoolInimigosInvocacao[Random.Shared.Next(ctx.PoolInimigosInvocacao.Count)];
            var novo = new InimigoEstadoDto
            {
                PersonagemId = modelo.PersonagemId,
                Nome = modelo.Nome,
                ImagemUrl = modelo.ImagemUrl,
                VidaAtual = modelo.VidaAtual > 0 ? modelo.VidaAtual : (modelo.Nome.Contains("Lanceiro") ? 4 : 3),
                Posicao = livres[i],
                CursorAcao = 0,
                Direcao = Random.Shared.Next(2) == 0 ? -1 : 1,
                Preparado = false,
                Telegrafando = false,
                RecuoPendente = false,
                Acoes = modelo.Acoes.Select(a => new InimigoAcaoDto
                {
                    Ordem = a.Ordem,
                    Tipo = a.Tipo,
                    Valor = a.Valor,
                    Ataque = a.Ataque
                }).ToList()
            };
            ctx.Inimigos.Add(new InimigoEmCombate(novo));
            posInvocados.Add(livres[i]);
        }
        ctx.RegistrarRaiosInvocacao(posInvocados);
    }
}
