using Microsoft.AspNetCore.Components.Web;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System;
using System.Globalization;

namespace Arthur_Showdown_Game.Services
{
    public class InputManager
    {
        public event Action<int>? AoSolicitarMovimento;
        public event Action? AoSolicitarInteracao;
        public event Action? AoSolicitarTrocaDirecao;
        public event Action? AoSolicitarEsperar;
        public event Action? OnConfigMudou;

        public bool EhDispositivoMobile { get; }
        public bool MostrarControlesNaTela { get; private set; }
        public double EscalaHud { get; private set; } = 1.0;
        public int EscalaHudPorcentagem => (int)Math.Round(EscalaHud * 100);
        public string EscalaHudCss => EscalaHud.ToString("0.00", CultureInfo.InvariantCulture);

        private string ChaveEscalaHud => EhDispositivoMobile ? "escala_hud_mobile" : "escala_hud_pc";

        public InputManager()
        {
            EhDispositivoMobile = DeviceInfo.Current.Platform == DevicePlatform.Android
                               || DeviceInfo.Current.Platform == DevicePlatform.iOS;
            bool padrao = EhDispositivoMobile || true; // habilitado inicialmente para teste no PC
            MostrarControlesNaTela = Preferences.Default.Get("mostrar_controles_tela", padrao);
            double escalaPadrao = EhDispositivoMobile ? 0.65 : 1.0;
            EscalaHud = Math.Clamp(Preferences.Default.Get(ChaveEscalaHud, escalaPadrao), 0.4, 1.5);
        }

        public void DefinirMostrarControlesNaTela(bool mostrar)
        {
            MostrarControlesNaTela = mostrar;
            Preferences.Default.Set("mostrar_controles_tela", mostrar);
            OnConfigMudou?.Invoke();
        }

        public void AlternarControlesNaTela() => DefinirMostrarControlesNaTela(!MostrarControlesNaTela);

        public void DefinirEscalaHud(double escala)
        {
            EscalaHud = Math.Clamp(Math.Round(escala, 2), 0.4, 1.5);
            Preferences.Default.Set(ChaveEscalaHud, EscalaHud);
            OnConfigMudou?.Invoke();
        }

        public void AumentarHud() => DefinirEscalaHud(EscalaHud + 0.1);
        public void DiminuirHud() => DefinirEscalaHud(EscalaHud - 0.1);

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
