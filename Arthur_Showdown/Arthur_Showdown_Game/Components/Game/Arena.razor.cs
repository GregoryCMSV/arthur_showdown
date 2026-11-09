using Arthur_Showdown_Game.Services;
using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.Game
{
    public partial class Arena
    {
        [Parameter] public int QuantidadeTiles { get; set; } = 5;
        [Parameter] public bool IsLobby { get; set; }
        [Parameter] public int TileTremendoId { get; set; } = -1;
        [Parameter] public PersonagemDto? InimigoAtual { get; set; }
        [Parameter] public int PosicaoInimigo { get; set; } = 4;

        [Inject] public PlayerService _PlayerService { get; set; } = default!;
        [Parameter] public int PlayerStartPosition { get; set; } = 2;
        [Parameter] public List<InimigoEmCombate> Inimigos { get; set; } = new();

        // Lado para o qual o inimigo olha 
        private int Rumo(InimigoEmCombate i)
        {
            var r = Math.Sign((_PlayerService.CurrentCharacter?.PosicaoPersonagem ?? 0) - i.Estado.Posicao);
            return r == 0 ? 1 : r;
        }

        // Intencao do inimigo: o que ele vai fazer no proximo turno
        private string Intencao(InimigoEmCombate i)
        {
            var a = i.Proxima;
            if (a == null) return "";
            return a.Tipo switch
            {
                "Mover" => Math.Sign(a.Valor) * Rumo(i) > 0 ? "->" : "<-",
                "Preparar" => "Carregar",
                "Atacar" => $"{(a.Ataque?.Dano ?? a.Valor)}",
                _ => "…"
            };
        }

    }
}
