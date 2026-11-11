using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;

namespace Arthur_Showdown_Game.Components.Game
{
    public partial class AtaqueSlot
    {
        [Parameter, EditorRequired] public AtaqueDto Ataque { get; set; } = default!;
        [Parameter] public int Numero { get; set; }
        [Parameter] public int CooldownRestante { get; set; }
        [Parameter] public bool Mini { get; set; }
    }
}
