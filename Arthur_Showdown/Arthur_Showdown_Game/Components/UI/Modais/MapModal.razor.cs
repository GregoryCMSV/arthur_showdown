using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.UI.Modais;

public partial class MapModal
{
    [Inject] public PlayerService _PlayerService { get; set; } = default!;
    [Parameter] public EventCallback OnClose { get; set; }
    [Inject] private NavigationService NavService { get; set; }

    private void IrParaCombate()
    {
        NavService.NavigateTo("/combat");
    }
}
