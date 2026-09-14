using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.Game
{
    public partial class Tile
    {
        [Parameter] public int Indice { get; set; }
        [Parameter] public bool IsEspecial { get; set; } 
        [Parameter] public bool IsPerigo { get; set; }   
        [Parameter] public bool IsTremendo { get; set; } 

        private string ObterClassesVisuais()
        {
            string classes = "";
            if (IsEspecial) classes += " tile-special ";
            if (IsPerigo) classes += " tile-danger ";
            if (IsTremendo) classes += " tile-shake ";
            return classes;
        }
    }
}
