using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.UI
{
    public partial class Background : ComponentBase
    {
        [Parameter]
        public string ImagemUrl { get; set; } = "";
        [Parameter]
        public EventCallback OnClick { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        private string ObterEstiloFundo()
        {
            if (string.IsNullOrWhiteSpace(ImagemUrl))
                return "background-color: black;";

            return $"background-image: url('{ImagemUrl}'); background-color: black;";
        }
    }
}
