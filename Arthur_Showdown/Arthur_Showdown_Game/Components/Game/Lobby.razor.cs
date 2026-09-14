using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.Game
{
    public partial class Lobby
    {
        [Parameter] public EventCallback OnStartClicked { get; set; }
    }
}
