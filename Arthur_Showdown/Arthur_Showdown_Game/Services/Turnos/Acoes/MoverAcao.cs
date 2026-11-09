namespace Arthur_Showdown_Game.Services;

public class MoverAcao(int dir) : IAcaoDeTurno
{
    private bool _valido;
    public bool GastaTurno => _valido;   
    public async Task ExecutarAsync(BatalhaContexto ctx)
    {
        var destino = ctx.Jogador.PosicaoPersonagem + dir;
        bool bloqueado = destino < 0 || destino >= ctx.TilesTotal
            || ctx.Inimigos.Any(i => i.Vivo && i.Estado.Posicao == destino);
        if (bloqueado) return;
        _valido = true;
        await ctx.Jogador.MoverAsync(dir);
    }
}
