using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.Game
{
    public partial class AtaquesContainer
    {
        [Parameter] public PersonagemDto? Personagem { get; set; }
    }
}
