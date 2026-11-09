using Arthur_Showdown_Game.Components.Game;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Services;

public class AtaqueAcao(CartaAtaque carta) : IAcaoDeTurno
{
    public bool GastaTurno => !carta.Ataque.IsFastAction;
    public async Task ExecutarAsync(BatalhaContexto ctx)
    {
        var tiles = HitboxHelper.Tiles(carta.Ataque, ctx.Jogador.PosicaoPersonagem, ctx.Jogador.DirecaoOlhar, ctx.TilesTotal);
        foreach (var inimigo in ctx.Inimigos.Where(i => tiles.Contains(i.Estado.Posicao)))
            inimigo.ReceberDano(carta.Ataque.Dano);
        await Task.Delay(300);
        carta.CooldownRestante = carta.Ataque.Cooldown > 0
            ? carta.Ataque.Cooldown + (GastaTurno ? 1 : 0)
            : 0;
    }
}
