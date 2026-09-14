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
        [Parameter] public int PosicaoPersonagem { get; set; }
        [Parameter] public bool EstaEmMovimento { get; set; }
        [Parameter] public PersonagemDto? PersonagemAtual { get; set; }
    }
}
