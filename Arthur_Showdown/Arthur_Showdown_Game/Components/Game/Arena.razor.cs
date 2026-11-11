using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.Game
{
    public partial class Arena
    {
        [Parameter] public int QuantidadeTiles { get; set; } = 5;
        [Parameter] public bool IsLobby { get; set; }
        [Parameter] public int TileTremendoId { get; set; } = -1;
        [Parameter] public PersonagemDto? InimigoAtual { get; set; }
        [Parameter] public int PosicaoInimigo { get; set; } = 4;

        [Inject] public PlayerService _PlayerService { get; set; } = default!;
        [Parameter] public int PlayerStartPosition { get; set; } = 2;
        [Parameter] public List<InimigoEmCombate> Inimigos { get; set; } = new();

        // Lado para o qual o inimigo olha 
        private int Rumo(InimigoEmCombate i)
        {
            return i.Estado.Direcao == 0 ? 1 : i.Estado.Direcao;
        }

        // Intencao do inimigo: o que ele vai fazer no proximo turno
        private string Intencao(InimigoEmCombate i)
        {
            int posJ = _PlayerService.CurrentCharacter?.PosicaoPersonagem ?? 0;
            var decisao = InimigoIA.Decidir(i, posJ, Inimigos, QuantidadeTiles);
            int rumo = InimigoIA.Rumo(i, posJ);
            return decisao switch
            {
                DecisaoInimigo.Virar => "Virar",
                DecisaoInimigo.Preparar => "Carregar",
                DecisaoInimigo.Mover => rumo > 0 ? "->" : "<-",
                DecisaoInimigo.Recuar => rumo > 0 ? "<-" : "->",
                DecisaoInimigo.Atacar => i.Dano.ToString(),
                _ => "..."
            };
        }

    }
}
