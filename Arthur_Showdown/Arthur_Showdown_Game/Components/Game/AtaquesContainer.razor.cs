using Arthur_Showdown_Game.Services;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Components.Game
{
    public partial class AtaquesContainer : IDisposable
    {
        const int SlotW = 90, SlotH = 90, Gap = 10;
        [Inject] public PlayerService _PlayerService { get; set; } = default!;
        [Inject] public TurnManager _Turnos { get; set; } = default!;
        private List<CartaAtaque> _cartas => _Turnos.Cartas;

        protected override void OnInitialized()
        {
            if (!_Turnos.Cartas.Any())
            {
                var ataques = _PlayerService.CurrentCharacter?.GetAtaques() ?? new();
                _Turnos.Cartas = ataques.Select(a => new CartaAtaque { Ataque = a }).ToList();
            }
            _Turnos.OnMudou += Atualizar;
        }

        (int x, int y) Posicao(CartaAtaque c)
            => (_cartas.IndexOf(c) * (SlotW + Gap), 0);

        async Task Enfileirar(CartaAtaque c)
        {
            if (c.EmFila || c.EmCooldown || _Turnos.Fase != FaseTurno.Jogador) return;
            await _Turnos.JogadorAgirAsync(new AdicionarNaFilaAcao(c, _cartas));
        }

        void Atualizar() => InvokeAsync(StateHasChanged);
        public void Dispose() => _Turnos.OnMudou -= Atualizar;
    }
}
