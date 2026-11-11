using System;

namespace Arthur_Showdown_Game.Services;

public class AcaoInimigoAcao(InimigoEmCombate inimigo) : IAcaoDeTurno
{
    public bool GastaTurno => false;
    public async Task ExecutarAsync(BatalhaContexto ctx)
    {
        var posJogador = ctx.Jogador.PosicaoPersonagem;
        int rumo = InimigoIA.Rumo(inimigo, posJogador);
        var decisao = InimigoIA.Decidir(inimigo, posJogador, ctx.Inimigos, ctx.TilesTotal);

        switch (decisao)
        {
            case DecisaoInimigo.Virar:
                inimigo.Estado.Direcao = rumo;
                break;

            case DecisaoInimigo.Preparar:
                inimigo.Preparado = true;
                break;

            case DecisaoInimigo.Mover:
                inimigo.Estado.Posicao += rumo;
                break;

            case DecisaoInimigo.Recuar:
                int passos = inimigo.RecuoPendente ? inimigo.RecuoAposAtaque : inimigo.RecuoSeProximo;
                inimigo.RecuoPendente = false;
                for (int i = 0; i < Math.Max(1, passos); i++)
                {
                    if (!InimigoIA.PodeAndar(inimigo, -rumo, posJogador, ctx.Inimigos, ctx.TilesTotal)) break;
                    inimigo.Estado.Posicao -= rumo;   // recua sem virar
                }
                break;

            case DecisaoInimigo.Atacar:
                ctx.VidaAtual = Math.Max(0, ctx.VidaAtual - inimigo.Dano);
                inimigo.Preparado = false;
                inimigo.RecuoPendente = inimigo.RecuoAposAtaque > 0;
                break;
        }

        await Task.Delay(300);
    }
}
