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
        public int Direcao { get; set; } = 1;
        public bool Preparado { get; set; }
        public bool Telegrafando { get; set; }
        public bool RecuoPendente { get; set; }   // lado para o qual olha (-1 esquerda, 1 direita)
        public List<InimigoAcaoDto> Acoes { get; set; } = new();   // fila de ações, vem do banco
    }
}
