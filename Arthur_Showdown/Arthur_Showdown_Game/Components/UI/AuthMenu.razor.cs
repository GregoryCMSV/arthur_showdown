using Arthur_Showdown_Shared.Dtos.Usuario;
using Microsoft.AspNetCore.Components;
using Microsoft.Maui.Storage;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Arthur_Showdown_Game.Components.UI
{
    public partial class AuthMenu : ComponentBase
    {
        [Inject] public HttpClient Http { get; set; } = default!;
        [Parameter] public EventCallback OnAutenticadoSucesso { get; set; }

        private bool IsModoLogin = true;
        private string Email = "";
        private string Senha = "";
        private string Nome = "";
        private string MensagemErro = "";

        private async Task FazerLogin()
        {
            MensagemErro = "Conectando...";
            var resposta = await Http.PostAsJsonAsync("/api/users/login", new { Email, Senha });

            if (resposta.IsSuccessStatusCode)
            {
                var dados = await resposta.Content.ReadFromJsonAsync<LoginResponse>();
                if (dados != null)
                {
                    await SecureStorage.Default.SetAsync("auth_token", dados.Token);
                    if (!string.IsNullOrEmpty(dados.RefreshToken))
                        await SecureStorage.Default.SetAsync("refresh_token", dados.RefreshToken);
                    await SecureStorage.Default.SetAsync("auth_email", Email);
                    await SecureStorage.Default.SetAsync("auth_password", Senha);
                }
                MensagemErro = "";
                await OnAutenticadoSucesso.InvokeAsync();
            }
            else
            {
                MensagemErro = "Credenciais inválidas!";
            }
        }

        private async Task FazerCadastro()
        {
            MensagemErro = "Criando conta...";
            var resposta = await Http.PostAsJsonAsync("/api/users/registrar", new UserRegistrationRequest { Nome = Nome, Email = Email, Senha = Senha, TipoUsuario = "Jogador" });
            if (resposta.IsSuccessStatusCode)
            {
                await FazerLogin();
            }
            else
            {
                MensagemErro = "Erro ao criar conta. Tente outro e-mail.";
            }
        }
    }
}
