using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.UI.Modais
{
    public partial class ShopModal
    {
        [Parameter] public EventCallback OnClose { get; set; }
    }
}
