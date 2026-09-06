using System;
using System.Collections.Generic;
using System.Text;

namespace Arthur_Showdown_Shared.Dtos.Usuario
{
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Senha { get; set; } = string.Empty;
    }
}
