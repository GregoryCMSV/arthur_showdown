using Arthur_Showdown_Game.Components.Game;
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
            await Task.Delay(200);
            return;
        }

        foreach (var carta in fila)
        {
            var tiles = HitboxHelper.Tiles(carta.Ataque, ctx.Jogador.PosicaoPersonagem, ctx.Jogador.DirecaoOlhar, ctx.TilesTotal);
            foreach (var inimigo in ctx.Inimigos.Where(i => i.Vivo && tiles.Contains(i.Estado.Posicao)))
                inimigo.ReceberDano(carta.Ataque.Dano);

            carta.PosicaoFila = null;
            carta.CooldownRestante = carta.Ataque.Cooldown > 0
                ? carta.Ataque.Cooldown + 1
                : 0;

            await Task.Delay(300);
        }
    }
}
