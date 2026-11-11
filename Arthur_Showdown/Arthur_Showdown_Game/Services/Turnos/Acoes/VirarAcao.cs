using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Services;

public class VirarAcao : IAcaoDeTurno
{
    public bool GastaTurno => true;
    public async Task ExecutarAsync(BatalhaContexto ctx)
    {
        ctx.QtdMudancaDirecao++;
        await ctx.Jogador.VirarAsync();
    }
}
