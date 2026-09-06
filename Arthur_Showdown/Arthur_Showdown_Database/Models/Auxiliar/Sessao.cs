using Arthur_Showdown_Database.Models.Base;

namespace Arthur_Showdown_Database.Models.Auxiliar;

public class Sessao
{
    public int Id { get; set; }
    public int NumeroSessao { get; set; }
    public int TamanhoMapa { get; set; }
    public int LocalId { get; set; }
    public Local? Local { get; set; }
}

public class Wave
{
    public int Id { get; set; }
    public int QtdInimigos { get; set; }
    public int SessaoId { get; set; }
    public Sessao? Sessao { get; set; }
}
