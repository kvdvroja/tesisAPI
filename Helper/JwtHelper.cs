using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;

namespace tesisAPI.Helper
{


    public static class JwtHelper
    {
        public static string ObtenerIdDesdeToken(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].ToString();

            if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                return "";

            var tokenJwt = authHeader.Substring("Bearer ".Length);
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokenJwt);
            var userId = jwt.Claims.FirstOrDefault(c => c.Type == "usua_id")?.Value;

            return userId ?? "";
        }
    }

}
