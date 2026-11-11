using Arthur_Showdown_Game.Components.Game;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Services;

public class AdicionarNaFilaAcao(CartaAtaque carta, List<CartaAtaque> todas) : IAcaoDeTurno
{
    public bool GastaTurno => !carta.Ataque.IsFastAction;
    public Task ExecutarAsync(BatalhaContexto ctx)
    {
        if (carta.EmFila || carta.EmCooldown) return Task.CompletedTask;
        int prox = todas.Where(c => c.EmFila).Select(c => c.PosicaoFila!.Value).DefaultIfEmpty(-1).Max() + 1;
        carta.PosicaoFila = prox;
        carta.QtdVezesAdicionadoFila++;
        return Task.CompletedTask;
    }
}
