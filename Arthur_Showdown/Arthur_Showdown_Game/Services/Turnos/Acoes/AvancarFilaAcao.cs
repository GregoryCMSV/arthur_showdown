using Arthur_Showdown_Game.Components.Game;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Services;

public class AvancarFilaAcao(List<CartaAtaque> cartas) : IAcaoDeTurno
{
    public bool GastaTurno => false;
    public async Task ExecutarAsync(BatalhaContexto ctx)
    {
        foreach (var c in cartas.Where(c => c.CooldownRestante > 0))
            c.CooldownRestante--;
        await Task.Delay(150);
    }
}
