using Microsoft.JSInterop;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Services
{
    public class SoundService
    {
        private readonly IJSRuntime _js;
        private double _ultimoPitchHit = 1.0;

        public bool MusicaAtiva { get; private set; }
        public bool EfeitosAtivos { get; private set; }

        public SoundService(IJSRuntime js)
        {
            _js = js;
            MusicaAtiva = Preferences.Default.Get("som_musica_ativa", true);
            EfeitosAtivos = Preferences.Default.Get("som_efeitos_ativos", true);
        }

        public async Task IniciarMusicaAsync()
        {
            try
            {
                await _js.InvokeVoidAsync("arthurAudio.setConfig", MusicaAtiva, EfeitosAtivos);
                await _js.InvokeVoidAsync("arthurAudio.startBgm");
            }
            catch { }
        }

        public async Task PararMusicaAsync()
        {
            try
            {
                await _js.InvokeVoidAsync("arthurAudio.stopBgm");
            }
            catch { }
        }

        public async Task AlternarMusicaAsync()
        {
            MusicaAtiva = !MusicaAtiva;
            Preferences.Default.Set("som_musica_ativa", MusicaAtiva);
            try
            {
                await _js.InvokeVoidAsync("arthurAudio.setConfig", MusicaAtiva, EfeitosAtivos);
            }
            catch { }
        }

        public async Task AlternarEfeitosAsync()
        {
            EfeitosAtivos = !EfeitosAtivos;
            Preferences.Default.Set("som_efeitos_ativos", EfeitosAtivos);
            try
            {
                await _js.InvokeVoidAsync("arthurAudio.setConfig", MusicaAtiva, EfeitosAtivos);
            }
            catch { }
        }

        public async Task TocarHitAsync(bool ehFlecha = false)
        {
            if (!EfeitosAtivos) return;
            try
            {
                // Gera um pitch aleatorio entre 0.86 e 1.16 garantindo diferenca perceptivel em relacao ao ultimo golpe
                double pitch;
                int tentativas = 0;
                do
                {
                    pitch = 0.86 + Random.Shared.NextDouble() * 0.30;
                    tentativas++;
                } while (Math.Abs(pitch - _ultimoPitchHit) < 0.06 && tentativas < 5);

                _ultimoPitchHit = pitch;
                await _js.InvokeVoidAsync("arthurAudio.playHit", Math.Round(pitch, 3), ehFlecha);
            }
            catch { }
        }

        public async Task TocarMovimentoAsync()
        {
            if (!EfeitosAtivos) return;
            try
            {
                double pitch = 0.92 + Random.Shared.NextDouble() * 0.16;
                await _js.InvokeVoidAsync("arthurAudio.playMove", Math.Round(pitch, 3));
            }
            catch { }
        }
    }
}
