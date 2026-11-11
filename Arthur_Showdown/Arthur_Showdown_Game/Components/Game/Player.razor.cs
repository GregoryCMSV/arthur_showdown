using Arthur_Showdown_Game.Models;
using Arthur_Showdown_Game.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Components.Game
{
    public partial class Player : IDisposable
    {
        [Inject] public PlayerService _PlayerService { get; set; } = default!;
        [Inject] public TurnManager _Turnos { get; set; } = default!;
        [Parameter] public int StartPosition { get; set; } = 0;

        private CharacterModel _currentCharacter = default!;

        private List<CartaAtaque> FilaJogador => _Turnos.Cartas
            .Where(c => c.EmFila)
            .OrderBy(c => c.PosicaoFila)
            .ToList();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            _currentCharacter = _PlayerService.CurrentCharacter!;
            _PlayerService.SetStartPosition(StartPosition);
            if (_PlayerService.CurrentCharacter != null)
                _PlayerService.CurrentCharacter.OnChange += AtualizarPlayer;
            _Turnos.OnMudou += AtualizarPlayer;
        }

        private CharacterModel CurrentCharacter() => _PlayerService.CurrentCharacter!;

        private string ClasseAnimacao => CurrentCharacter().EstaEmMovimento ? ObterAnimacaoPorPeso() : "";

        private string ObterAnimacaoPorPeso() => CurrentCharacter().CategoriaPeso switch
        {
            0 => "pulo-leve",
            2 => "pulo-pesado",
            _ => "pulo-normal"
        };

        private void MoverFila(CartaAtaque c, int delta) => _Turnos.MoverNaFila(c, delta);

        private async Task RemoverDaFila(CartaAtaque c)
        {
            if (_Turnos.Fase != FaseTurno.Jogador) return;
            await _Turnos.JogadorAgirAsync(new RemoverDaFilaAcao(c, _Turnos.Cartas));
        }

        public void Dispose()
        {
            if (_PlayerService.CurrentCharacter != null)
                _PlayerService.CurrentCharacter.OnChange -= AtualizarPlayer;
            _Turnos.OnMudou -= AtualizarPlayer;
        }

        private void AtualizarPlayer() => InvokeAsync(StateHasChanged);

        private string ClasseGiro => CurrentCharacter().EstaVirando ? (CurrentCharacter().DirecaoOlhar == -1 ? "virando-esquerda" : "virando-direita") : "";
    }
}
