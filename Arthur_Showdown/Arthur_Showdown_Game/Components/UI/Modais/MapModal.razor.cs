using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.UI.Modais
{
    public partial class MapModal
    {
        [Parameter] public PersonagemDto? PersonagemAtual { get; set; }
    }
}
