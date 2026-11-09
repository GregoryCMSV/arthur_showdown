using Arthur_Showdown_Game.Services;
using Microsoft.AspNetCore.Components;

namespace Arthur_Showdown_Game.Components.UI;

public partial class Scene : ComponentBase
{
    [Inject] public LoadingService _LoadingService { get; set; } = default!;
    [Parameter] public string ImagemUrl { get; set; } = "";
    [Parameter] public EventCallback OnClick { get; set; }
    [Parameter] public RenderFragment? ParallaxLayer { get; set; }
    [Parameter] public RenderFragment? GameLayer { get; set; }
    [Parameter] public RenderFragment? EffectsLayer { get; set; }
    [Parameter] public RenderFragment? UILayer { get; set; }

    private string loadingMessage = "Carregando.";
    private bool _isAnimando = false;

    protected override void OnParametersSet()
    {
        if (_LoadingService.EstaCarregando && !_isAnimando)
        {
            _isAnimando = true;
            _ = AnimarTextoLoadingAsync();
        }
        else if (!_LoadingService.EstaCarregando)
        {
            _isAnimando = false;
        }
    }

    private async Task AnimarTextoLoadingAsync()
    {
        int quantidadePontos = 1;

        while (_isAnimando)
        {
            loadingMessage = "Carregando" + new string('.', quantidadePontos);
            await InvokeAsync(StateHasChanged);
            await Task.Delay(500);
            quantidadePontos = quantidadePontos >= 3 ? 1 : quantidadePontos + 1;
        }
    }

}