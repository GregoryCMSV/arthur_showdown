using System.Collections.Generic;

namespace Arthur_Showdown_Shared.Dtos.Game
{
    public class InimigoEstadoDto
    {
        public int PersonagemId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string? ImagemUrl { get; set; }
        public int VidaAtual { get; set; }
        public int Posicao { get; set; }
        public int CursorAcao { get; set; }
        public List<InimigoAcaoDto> Acoes { get; set; } = new();   // fila de ações, vem do banco
    }
}
