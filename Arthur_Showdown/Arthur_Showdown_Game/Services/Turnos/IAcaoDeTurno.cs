namespace Arthur_Showdown_Game.Services;

public interface IAcaoDeTurno
{
    bool GastaTurno { get; }
    Task ExecutarAsync(BatalhaContexto ctx);
}
