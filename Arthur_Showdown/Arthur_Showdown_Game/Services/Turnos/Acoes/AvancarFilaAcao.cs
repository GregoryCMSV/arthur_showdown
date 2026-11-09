using Arthur_Showdown_Game.Components.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Services;

public class AvancarFilaAcao(List<CartaAtaque> cartas) : IAcaoDeTurno
{
    public bool GastaTurno => false;
    public async Task ExecutarAsync(BatalhaContexto ctx)
    {
        foreach (var c in cartas.Where(c => c.EmFila)) c.CooldownRestante--;
        await Task.Delay(400);
    }
}
