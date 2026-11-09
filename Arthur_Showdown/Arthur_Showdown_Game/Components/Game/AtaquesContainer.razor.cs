using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.Game
{
    public partial class AtaquesContainer : IDisposable
    {
        const int SlotW = 80, SlotH = 90, Gap = 10, FilaY = -(SlotH + 20);
        [Inject] public PlayerService _PlayerService { get; set; } = default!;
        [Inject] public TurnManager _Turnos { get; set; } = default!;   
        private List<CartaAtaque> _cartas = new();
        int MaxCooldown => _cartas.Count == 0 ? 1 : Math.Max(1, _cartas.Max(c => c.Ataque.Cooldown));
        protected override void OnInitialized()
        {
            var ataques = _PlayerService.CurrentCharacter?.GetAtaques() ?? new();
            // Se o CombatScreen ja montou as cartas, usa elas
            _cartas = _Turnos.Cartas.Any()
                ? _Turnos.Cartas
                : ataques.Select(a => new CartaAtaque { Ataque = a }).ToList();
            _Turnos.Cartas = _cartas;                       
            _Turnos.OnMudou += Atualizar;
        }
        (int x, int y) Posicao(CartaAtaque c)
        {
            if (!c.EmFila)                                  
                return (_cartas.IndexOf(c) * (SlotW + Gap), 0);
            int casa = c.CooldownRestante - 1;
            return (casa * (SlotW + Gap), FilaY);
        }
        async Task Usar(CartaAtaque c)
        {
            if (c.EmFila || _Turnos.Fase != FaseTurno.Jogador) return;
            await _Turnos.JogadorAgirAsync(new AtaqueAcao(c));
        }
        void Atualizar() => InvokeAsync(StateHasChanged);
        public void Dispose() => _Turnos.OnMudou -= Atualizar;
    }
}
