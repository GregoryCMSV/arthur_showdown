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
            await Task.Delay(150);
            return;
        }

        foreach (var carta in fila)
        {
            var hitbox = HitboxHelper.Ler(carta.Ataque);
            var tiles = HitboxHelper.Tiles(hitbox, ctx.Jogador.PosicaoPersonagem, ctx.Jogador.DirecaoOlhar, ctx.TilesTotal);
            bool ehFlecha = hitbox.AlcanceMax > 2 || carta.Ataque.Nome.Contains("Flecha") || carta.Ataque.Nome.Contains("Arco");

            int destino = ctx.Jogador.PosicaoPersonagem + ctx.Jogador.DirecaoOlhar * System.Math.Min(hitbox.AlcanceMax, ctx.TilesTotal - 1);
            var primeiroInimigo = ctx.Inimigos
                .Where(i => i.Vivo && tiles.Contains(i.Estado.Posicao))
                .OrderBy(i => System.Math.Abs(i.Estado.Posicao - ctx.Jogador.PosicaoPersonagem))
                .FirstOrDefault();
            if (primeiroInimigo != null) destino = primeiroInimigo.Estado.Posicao;

            ctx.EfeitoVisual = new EfeitoVisualAtaque
            {
                TilesImpacto = ehFlecha ? new HashSet<int> { destino } : tiles,
                EhFlecha = ehFlecha,
                OrigemTile = ctx.Jogador.PosicaoPersonagem,
                DestinoTile = destino,
                Direcao = ctx.Jogador.DirecaoOlhar
            };
            ctx.NotificarVisual?.Invoke();

            foreach (var inimigo in ctx.Inimigos.Where(i => i.Vivo && tiles.Contains(i.Estado.Posicao)))
                inimigo.ReceberDano(carta.Ataque.Dano);

            carta.PosicaoFila = null;
            carta.QtdUsosNaRun++;
            carta.CooldownRestante = carta.Ataque.Cooldown > 0
                ? carta.Ataque.Cooldown + 1
                : 0;

            await Task.Delay(280);
            ctx.EfeitoVisual = null;
            ctx.NotificarVisual?.Invoke();
        }
    }
}
