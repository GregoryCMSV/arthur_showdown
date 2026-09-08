using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Arthur__Showdown_API.Services
{
    public class TokenService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TokenService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? ObterUsuarioId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? user?.FindFirstValue("sub");
        }

        public string? ObterEmail()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirstValue(ClaimTypes.Email) ?? user?.FindFirstValue("email");
        }

        public string DecodificarTokenParaDebug(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return "O formato do token é inválido.";

                var jwtToken = handler.ReadJwtToken(token);
                var debugInfo = $"Expiração: {jwtToken.ValidTo.ToLocalTime()}\nClaims:\n";

                foreach (var claim in jwtToken.Claims)
                {
                    debugInfo += $"- {claim.Type}: {claim.Value}\n";
                }

                return debugInfo;
            }
            catch (Exception ex)
            {
                return $"Erro ao decodificar: {ex.Message}";
            }
        }
    }
}
