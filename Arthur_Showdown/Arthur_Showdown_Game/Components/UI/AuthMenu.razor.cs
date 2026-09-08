using Arthur_Showdown_Shared.Dtos.Usuario;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text;

namespace Arthur_Showdown_Game.Components.UI
{
    public partial class AuthMenu : ComponentBase
    {
        [Inject] public HttpClient Http { get; set; }
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
                await SecureStorage.Default.SetAsync("auth_token", dados.Token);
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
            var resposta = await Http.PostAsJsonAsync("/api/users/registrar", new UserRegistrationRequest { Nome = Nome, Email = Email,  Senha = Senha, TipoUsuario = "Jogador" });
            var content = await resposta.Content.ReadAsStringAsync();
            if (resposta.IsSuccessStatusCode)
            {
                FazerLogin();
            }
            else
            {
                MensagemErro = "Erro ao criar conta. Tente outro e-mail.";
            }
        }

       
    }
}
