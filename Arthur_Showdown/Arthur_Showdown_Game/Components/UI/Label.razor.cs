using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.UI
{
    public partial class Label
    {
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter] public string Tamanho { get; set; } = "16px";
        [Parameter] public string Cor { get; set; } = "white";
        [Parameter] public string CssClass { get; set; } = "";
        [Parameter] public string Sombra { get; set; } = "2px 2px 4px rgba(0,0,0,0.8)";
        [Parameter] public string Margem { get; set; } = "0px";
        [Parameter] public string EspacamentoLetras { get; set; } = "normal";

        private string ObterEstilo()
        {
            string estilo = $"font-size: {Tamanho}; color: {Cor}; margin: {Margem}; letter-spacing: {EspacamentoLetras};";

            if (!string.IsNullOrWhiteSpace(Sombra))
                estilo += $" text-shadow: {Sombra};";

            return estilo;
        }

    }
}
