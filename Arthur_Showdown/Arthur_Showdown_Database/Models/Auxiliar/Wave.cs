namespace Arthur_Showdown_Database.Models.Auxiliar;

public class Wave
{
    public int Id { get; set; }
    public int QtdInimigos { get; set; }
    public int SessaoId { get; set; }
    public Sessao? Sessao { get; set; }
}
