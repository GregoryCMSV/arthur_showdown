using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arthur_Showdown_Game.Components.Game
{
    public partial class Arena : IDisposable
    {
        [Parameter] public int QuantidadeTiles { get; set; } = 5;
        [Parameter] public bool IsLobby { get; set; }
        [Parameter] public int TileTremendoId { get; set; } = -1;
        [Parameter] public PersonagemDto? InimigoAtual { get; set; }
        [Parameter] public int PosicaoInimigo { get; set; } = 4;

        [Inject] public PlayerService _PlayerService { get; set; } = default!;
        [Inject] public TurnManager _Turnos { get; set; } = default!;
        [Parameter] public int PlayerStartPosition { get; set; } = 2;
        [Parameter] public List<InimigoEmCombate> Inimigos { get; set; } = new();
        [Parameter] public bool MostrarOrdemInimigos { get; set; }

        private bool TremendoTela => _Turnos.Ctx?.TremendoTela == true;

        private List<DanoFlutuante> DanosFlutuantes
        {
            get
            {
                if (_Turnos.Ctx == null) return new();
                lock (_Turnos.Ctx.DanosFlutuantes)
                    return _Turnos.Ctx.DanosFlutuantes.ToList();
            }
        }

        private List<RaioInvocacao> RaiosInvocacao
        {
            get
            {
                if (_Turnos.Ctx == null) return new();
                lock (_Turnos.Ctx.RaiosInvocacao)
                    return _Turnos.Ctx.RaiosInvocacao.ToList();
            }
        }

        protected override void OnInitialized()
        {
            _Turnos.OnMudou += AtualizarArena;
        }

        private void AtualizarArena() => InvokeAsync(StateHasChanged);

        public void Dispose()
        {
            _Turnos.OnMudou -= AtualizarArena;
        }

        private int Rumo(InimigoEmCombate i)
            => i.Estado.Direcao == 0 ? 1 : i.Estado.Direcao;

        private int OrdemAcao(InimigoEmCombate i)
        {
            var vivos = Inimigos
                .Where(x => x.Vivo)
                .OrderBy(x => x.OrdemTurno > 0 ? x.OrdemTurno : int.MaxValue)
                .ThenBy(x => Math.Abs(x.Estado.Posicao - (_PlayerService.CurrentCharacter?.PosicaoPersonagem ?? 0)))
                .ThenBy(x => x.Estado.Posicao)
                .ToList();
            return vivos.IndexOf(i) + 1;
        }

        private string SetaMovimento(InimigoEmCombate i)
        {
            int posJ = _PlayerService.CurrentCharacter?.PosicaoPersonagem ?? 0;
            var decisao = i.IntencaoPlanejada ?? InimigoIA.Decidir(i, posJ, Inimigos, QuantidadeTiles);
            int rumo = i.DirecaoIntencao != 0 ? i.DirecaoIntencao : InimigoIA.Rumo(i, posJ);
            return decisao switch
            {
                DecisaoInimigo.Mover => rumo > 0 ? "→" : "←",
                DecisaoInimigo.Recuar => rumo > 0 ? "←" : "→",
                _ => ""
            };
        }
    }
}
