using Microsoft.AspNetCore.Components.Web;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System;

namespace Arthur_Showdown_Game.Services
{
    public class InputManager
    {
        public event Action<int>? AoSolicitarMovimento;
        public event Action? AoSolicitarInteracao;
        public event Action? AoSolicitarTrocaDirecao;
        public event Action? AoSolicitarEsperar;
        public event Action? OnConfigMudou;

        public bool MostrarControlesNaTela { get; private set; }

        public InputManager()
        {
            bool padrao = DeviceInfo.Current.Platform == DevicePlatform.Android
                       || DeviceInfo.Current.Platform == DevicePlatform.iOS
                       || true; // habilitado inicialmente para teste no PC
            MostrarControlesNaTela = Preferences.Default.Get("mostrar_controles_tela", padrao);
        }

        public void DefinirMostrarControlesNaTela(bool mostrar)
        {
            MostrarControlesNaTela = mostrar;
            Preferences.Default.Set("mostrar_controles_tela", mostrar);
            OnConfigMudou?.Invoke();
        }

        public void AlternarControlesNaTela() => DefinirMostrarControlesNaTela(!MostrarControlesNaTela);

        public void MoverEsquerda() => AoSolicitarMovimento?.Invoke(-1);
        public void MoverDireita() => AoSolicitarMovimento?.Invoke(1);
        public void Interagir() => AoSolicitarInteracao?.Invoke();
        public void TrocarDirecao() => AoSolicitarTrocaDirecao?.Invoke();
        public void Esperar() => AoSolicitarEsperar?.Invoke();

        public void ProcessarTeclado(KeyboardEventArgs e)
        {
            if (e.Key == "a" || e.Key == "A" || e.Key == "ArrowLeft")
                MoverEsquerda();
            else if (e.Key == "d" || e.Key == "D" || e.Key == "ArrowRight")
                MoverDireita();
            else if (e.Key == "Enter" || e.Key == " ")
                Interagir();
            else if (e.Key == "w" || e.Key == "W" || e.Key == "ArrowUp")
                TrocarDirecao();
            else if (e.Key == "s" || e.Key == "S" || e.Key == "ArrowDown")
                Esperar();
        }
    }
}
