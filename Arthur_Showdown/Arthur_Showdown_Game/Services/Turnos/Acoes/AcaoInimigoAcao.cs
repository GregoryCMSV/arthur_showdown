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
        int rumo = InimigoIA.Rumo(inimigo, posJogador);
        var decisao = InimigoIA.Decidir(inimigo, posJogador, ctx.Inimigos, ctx.TilesTotal);

        switch (decisao)
        {
            case DecisaoInimigo.Virar:
                inimigo.Estado.Direcao = rumo;
                inimigo.Telegrafando = false;
                break;

            case DecisaoInimigo.Preparar:
                inimigo.Preparado = true;
                inimigo.Telegrafando = false;
                break;

            case DecisaoInimigo.Mover:
                inimigo.Estado.Posicao += rumo;
                inimigo.Telegrafando = false;
                ctx.TocarSomMovimento?.Invoke();
                break;

            case DecisaoInimigo.Recuar:
                int passos = inimigo.RecuoPendente ? inimigo.RecuoAposAtaque : inimigo.RecuoSeProximo;
                inimigo.RecuoPendente = false;
                inimigo.Telegrafando = false;
                bool moveu = false;
                for (int i = 0; i < Math.Max(1, passos); i++)
                {
                    if (!InimigoIA.PodeAndar(inimigo, -rumo, posJogador, ctx.Inimigos, ctx.TilesTotal)) break;
                    inimigo.Estado.Posicao -= rumo;
                    moveu = true;
                }
                if (moveu) ctx.TocarSomMovimento?.Invoke();
                break;

            case DecisaoInimigo.Telegrafar:
                inimigo.Telegrafando = true;
                break;

            case DecisaoInimigo.Esperar:
                inimigo.RecuoPendente = false;
                inimigo.Telegrafando = false;
                break;

            case DecisaoInimigo.Atacar:
                var hitbox = HitboxHelper.Ler(inimigo.Ataque);
                var tiles = HitboxHelper.Tiles(hitbox, inimigo.Estado.Posicao, inimigo.Estado.Direcao, ctx.TilesTotal);
                bool ehFlecha = hitbox.AlcanceMax > 2
                             || inimigo.Estado.Nome.Contains("Arqueiro", StringComparison.OrdinalIgnoreCase)
                             || (inimigo.Ataque?.Nome.Contains("Flecha", StringComparison.OrdinalIgnoreCase) ?? false);

                ctx.EfeitoVisual = new EfeitoVisualAtaque
                {
                    TilesImpacto = ehFlecha ? new HashSet<int> { posJogador } : tiles,
                    EhFlecha = ehFlecha,
                    OrigemTile = inimigo.Estado.Posicao,
                    DestinoTile = posJogador,
                    Direcao = inimigo.Estado.Direcao == 0 ? rumo : inimigo.Estado.Direcao
                };
                ctx.NotificarVisual?.Invoke();

                bool acertou = false;
                if (InimigoIA.PodeAtacar(inimigo, posJogador, ctx.Inimigos, ctx.TilesTotal))
                {
                    if (ctx.EscudoAtivo)
                    {
                        ctx.EscudoAtivo = false;
                    }
                    else
                    {
                        ctx.VidaAtual = Math.Max(0, ctx.VidaAtual - inimigo.Dano);
                        ctx.QtdHitsSofridos++;
                    }
                    acertou = true;
                }

                // Fogo amigo nos tiles atingidos (apenas para ataques fisicos de area/linha; flecha já exige caminho livre)
                if (!ehFlecha)
                {
                    foreach (var outro in ctx.Inimigos.Where(o => o != inimigo && o.Vivo && tiles.Contains(o.Estado.Posicao)))
                    {
                        outro.ReceberDano(inimigo.Dano);
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

    private static void InvocarInimigosDoChefe(BatalhaContexto ctx, int quantidade)
    {
        var posJogador = ctx.Jogador.PosicaoPersonagem;
        var livres = Enumerable.Range(0, ctx.TilesTotal)
            .Where(t => t != posJogador && !ctx.Inimigos.Any(o => o.Vivo && o.Estado.Posicao == t))
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        if (livres.Count == 0 || ctx.PoolInimigosInvocacao.Count == 0) return;

        int total = Math.Min(quantidade, livres.Count);
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
        }
    }
}
