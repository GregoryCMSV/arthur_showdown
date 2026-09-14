using Arthur_Showdown_Shared.Dtos.Game;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Components.Game
{
    public partial class Player
    {
        [Parameter] public int PosicaoAtual { get; set; }
        [Parameter] public bool IsMovendo { get; set; }
        [Parameter] public PersonagemDto? Personagem { get; set; }
        // peso: 0 = Leve, 1 = Normal, 2 = Pesado
        [Parameter] public int CategoriaPeso { get; set; } = 1;

        private string ClasseAnimacao => IsMovendo ? ObterAnimacaoPorPeso() : "";

        private string ObterAnimacaoPorPeso()
        {
            return CategoriaPeso switch
            {
                0 => "pulo-leve",
                2 => "pulo-pesado",
                _ => "pulo-normal"
            };
        }
    }
}
