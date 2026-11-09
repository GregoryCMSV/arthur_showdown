using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Game.Services
{
    public class LoadingService
    {
        public event Action? AoCarregar;

        public bool EstaCarregando { get; private set; }

        public void IniciarCarregamento()
        {
            EstaCarregando = true;
        }

        public void FinalizarCarregamento()
        {
            EstaCarregando = false;
            AoCarregar?.Invoke();
        }

    }
}
