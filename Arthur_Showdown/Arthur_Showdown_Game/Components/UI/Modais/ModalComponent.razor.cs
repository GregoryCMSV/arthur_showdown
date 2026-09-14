using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.UI.Modais
{
    public partial class ModalComponent
    {
        [Parameter] public string Titulo { get; set; } = "";
        [Parameter] public string CssClass { get; set; } = "generic-modal";
        [Parameter] public bool MostrarDicaFechar { get; set; } = true;
        [Parameter] public RenderFragment? ChildContent { get; set; }
    }
}
