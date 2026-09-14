using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Services
{
    public class InputManager
    {
        public event Action<int>? AoSolicitarMovimento;

        public event Action? AoSolicitarInteracao;

        public void MoverEsquerda() => AoSolicitarMovimento?.Invoke(-1);
        public void MoverDireita() => AoSolicitarMovimento?.Invoke(1);
        public void Interagir() => AoSolicitarInteracao?.Invoke();

        public void ProcessarTeclado(KeyboardEventArgs e)
        {
            if (e.Key == "a" || e.Key == "A" || e.Key == "ArrowLeft")
                MoverEsquerda();
            else if (e.Key == "d" || e.Key == "D" || e.Key == "ArrowRight")
                MoverDireita();
            else if (e.Key == "Enter" || e.Key == " ")
                Interagir();
        }
    }
}
