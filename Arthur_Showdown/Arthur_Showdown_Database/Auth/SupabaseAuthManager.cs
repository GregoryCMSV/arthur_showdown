using Supabase.Gotrue;
using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
