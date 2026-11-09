using System;

namespace Arthur_Showdown_Game.Services;

public class AcaoInimigoAcao(InimigoEmCombate inimigo) : IAcaoDeTurno
{
    public bool GastaTurno => false;
    public async Task ExecutarAsync(BatalhaContexto ctx)
    {
        var a = inimigo.Proxima;
        if (a == null) return;

        var posJogador = ctx.Jogador.PosicaoPersonagem;
        int rumo = Math.Sign(posJogador - inimigo.Estado.Posicao);   // direcao do jogador (-1, 0, 1)
        if (rumo == 0) rumo = 1;

        switch (a.Tipo)
        {
            case "Mover":
                int passo = Math.Sign(a.Valor) * rumo;
                for (int i = 0; i < Math.Abs(a.Valor); i++)
                {
                    int novo = inimigo.Estado.Posicao + passo;
                    bool bloqueado = novo < 0 || novo >= ctx.TilesTotal
                        || novo == posJogador
                        || ctx.Inimigos.Any(o => o != inimigo && o.Vivo && o.Estado.Posicao == novo);
                    if (bloqueado) break;
                    inimigo.Estado.Posicao = novo;
                }
                break;

            case "Atacar":
                var hitbox = HitboxHelper.Ler(a.Ataque);
                var tiles = HitboxHelper.Tiles(hitbox, inimigo.Estado.Posicao, rumo, ctx.TilesTotal);
                if (tiles.Contains(posJogador))
                    ctx.VidaAtual = Math.Max(0, ctx.VidaAtual - (a.Ataque?.Dano ?? a.Valor));
                break;

            case "Preparar":   //  o jogador ve o ataque chegando
            case "Esperar":
            default:
                break;
        }

        inimigo.Avancar();
        await Task.Delay(300);
    }
}

