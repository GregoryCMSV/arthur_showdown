using Arthur_Showdown_Game.Components.Game;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Services;

public class RemoverDaFilaAcao(CartaAtaque carta, List<CartaAtaque> todas) : IAcaoDeTurno
{
    public bool GastaTurno => !carta.Ataque.IsFastAction;
    public Task ExecutarAsync(BatalhaContexto ctx)
    {
        if (!carta.EmFila) return Task.CompletedTask;
        carta.PosicaoFila = null;
        carta.QtdVezesRemovidoFila++;
        int idx = 0;
        foreach (var c in todas.Where(x => x.EmFila).OrderBy(x => x.PosicaoFila))
            c.PosicaoFila = idx++;
        return Task.CompletedTask;
    }
}
