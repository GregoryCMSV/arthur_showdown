using Arthur_Showdown_Game.Models;
using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.Game
{
    public partial class Player : IDisposable
    {
        [Inject] public PlayerService _PlayerService { get; set; } = default!;
        [Parameter] public int StartPosition { get; set; } = 0;

        private CharacterModel _currentCharacter;

        override protected void OnInitialized()
        {
            base.OnInitialized();
            _currentCharacter = _PlayerService.CurrentCharacter;
            _PlayerService.SetStartPosition(StartPosition);
            _PlayerService.CurrentCharacter.OnChange += AtualizarPlayer;
        }

        private CharacterModel CurrentCharacter()
        {
            return _PlayerService.CurrentCharacter;
        }

        private string ClasseAnimacao => CurrentCharacter().EstaEmMovimento ? ObterAnimacaoPorPeso() : "";

        private string ObterAnimacaoPorPeso()
        {
            return CurrentCharacter().CategoriaPeso switch
            {
                0 => "pulo-leve",
                2 => "pulo-pesado",
                _ => "pulo-normal"
            };
        }

        public void Dispose()
        {
            if (_PlayerService.CurrentCharacter != null)
            {
                _PlayerService.CurrentCharacter.OnChange -= AtualizarPlayer;
            }
        }

        private void AtualizarPlayer()
        {
            InvokeAsync(StateHasChanged);
        }

        private string ClasseGiro => CurrentCharacter().EstaVirando ? (CurrentCharacter().DirecaoOlhar == -1 ? "virando-esquerda" : "virando-direita") : "";
    }
}
