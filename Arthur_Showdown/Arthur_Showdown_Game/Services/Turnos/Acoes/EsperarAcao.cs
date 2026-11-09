namespace Arthur_Showdown_Game.Services;

public class EsperarAcao : IAcaoDeTurno
{
    public bool GastaTurno => true;
    public Task ExecutarAsync(BatalhaContexto ctx) => Task.Delay(200);
}
