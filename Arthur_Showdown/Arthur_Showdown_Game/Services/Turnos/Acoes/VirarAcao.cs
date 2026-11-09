namespace Arthur_Showdown_Game.Services;

public class VirarAcao : IAcaoDeTurno
{
    public bool GastaTurno => true;
    public async Task ExecutarAsync(BatalhaContexto ctx) => await ctx.Jogador.VirarAsync();
}