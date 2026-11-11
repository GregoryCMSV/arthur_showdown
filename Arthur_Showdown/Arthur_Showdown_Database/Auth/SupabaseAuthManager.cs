using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Arthur_Showdown_Database.Auth
{
    public class SupabaseAuthManager
    {
        private readonly Supabase.Client _supabase;

        public SupabaseAuthManager(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        public async Task<Session?> RegistrarUsuario(string email, string senha, string tipoUsuario, string nome)
        {
            var tiposValidos = new[] { "Jogador", "Tester", "Admin" };
            if (!tiposValidos.Contains(tipoUsuario))
                throw new ArgumentException("Tipo de usuário inválido.");

            var options = new SignUpOptions
            {
                Data = new Dictionary<string, object>
                {
                    { "tipo_usuario", tipoUsuario },
                    { "display_name", nome }
                }
            };

            var session = await _supabase.Auth.SignUp(email, senha, options);
            return session;
        }

        public async Task<Session?> FazerLogin(string email, string senha)
        {
            try
            {
                var session = await _supabase.Auth.SignIn(email, senha);
                return session;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao fazer login: " + ex.Message);
            }
        }

        public async Task<Session?> RenovarSessao(string accessToken, string refreshToken)
        {
            try
            {
                await _supabase.Auth.SetSession(accessToken, refreshToken, true);
                var session = _supabase.Auth.CurrentSession;
                if (session == null || session.AccessToken == accessToken)
                {
                    session = await _supabase.Auth.RefreshSession();
                }
                return session;
            }
            catch (Exception ex)
            {
                throw new Exception("Erro ao renovar sessão: " + ex.Message);
            }
        }
    }
}
