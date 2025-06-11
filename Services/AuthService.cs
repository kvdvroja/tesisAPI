using Microsoft.AspNetCore.Connections;
using Microsoft.IdentityModel.Tokens;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Data.Common;

namespace tesisAPI.Services
{
    public class AuthService
    {
        private readonly IConfiguration _config;
        private readonly OConnection _dbFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(IConfiguration config, IHttpContextAccessor httpContextAccessor,OConnection dbFactory)
        {
            _config = config;
            _httpContextAccessor = httpContextAccessor;
            _dbFactory = dbFactory;
        }

        public async Task<object> Ejecutar(string nombreProc, string modo, object payload)
        {
            // Consultar al procedimiento PL/SQL
            using var con = await _dbFactory.CreateConnectionAsync();
            using var cmd = new OracleCommand(nombreProc, (OracleConnection)con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("P_ACCION", OracleDbType.Varchar2).Value = modo;
            cmd.Parameters.Add("P_DATA", OracleDbType.Clob).Value = payload;
            cmd.Parameters.Add("CURSOR_OUT", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows)
                return new { error = "Usuario no válido o sin datos" };

            await reader.ReadAsync();
            var resultado = new Dictionary<string, object>();
            for (int i = 0; i < reader.FieldCount; i++)
                resultado[reader.GetName(i)] = reader.GetValue(i);

            //extraccion del iat del token para registro
            string iat = "";
            string exp = "";

            if (payload is string rawJson)
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("iat", out var iatProp))
                {
                    iat = iatProp.GetString() ?? "";
                }
                if (root.TryGetProperty("exp", out var expProp))
                {
                    exp = expProp.GetString() ?? "";
                }
            }

            // Devolver los datos como nuevo JWT
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);
            
            //var claims = resultado.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() ?? "")).ToList();

            var claims = new[]
            {
                new Claim("usua_id", resultado["TSRCARG_USUA_ID"].ToString()!),
                new Claim("nom_completo", resultado["NOM_COMPLETO"].ToString()!),
                new Claim("apellidos", resultado["APELLIDOS"].ToString()!),
                new Claim("nombres", resultado["NOMBRES"].ToString()!),
                new Claim("rol", resultado["TSRCARG_ROL"].ToString()!),
                new Claim("facu", resultado["TSRCARG_FACU"].ToString()!),
                new Claim("majr", resultado["TSRCARG_MAJR"].ToString()!),
                new Claim("periodo", resultado["PERIODO"].ToString()!),
                new Claim("log_code", Guid.NewGuid().ToString()! ),
                new Claim("iat", iat, ClaimValueTypes.Integer64! )
                //new Claim("exp", iat, ClaimValueTypes.Integer64! )
            };

            var tokenFinal = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(1),
                    signingCredentials: creds
                );                      

            // 🔒 Registrar sesión en BD            
            var tokenString = new JwtSecurityTokenHandler().WriteToken(tokenFinal);
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(tokenString);
            await RegistrarSesionAsync(_httpContextAccessor.HttpContext!, jwt, tokenString);
            
            //return new { token = tokenString };
            return tokenString;
        
        }

        private async Task RegistrarSesionAsync(HttpContext context, JwtSecurityToken jwt, string token)
        {
            var iatUnix = jwt.Claims.FirstOrDefault(c => c.Type == "iat")?.Value;
            var expUnix = jwt.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;

            string fecha_inicio = "";
            string fecha_termino = "";

            if (long.TryParse(iatUnix, out long iatSeconds))
            {
                var iatDateTime = DateTimeOffset.FromUnixTimeSeconds(iatSeconds).UtcDateTime;
                fecha_inicio = iatDateTime.ToString("yyyy-MM-dd HH:mm:ss"); // formato tipo CHAR
            }

            if (long.TryParse(expUnix, out long expSeconds))
            {
                var expDateTime = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
                fecha_termino = expDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }



            var payload = new Dictionary<string, object?>
            {
                ["usua_id"] = jwt.Claims.FirstOrDefault(c => c.Type == "usua_id")?.Value,
                ["ip"] = context.Connection.RemoteIpAddress?.ToString(),
                ["rol"] = jwt.Claims.FirstOrDefault(c => c.Type == "rol")?.Value ?? "", // revisa el claim exacto si se llama distinto
                ["nombre"] = jwt.Claims.FirstOrDefault(c => c.Type == "nom_completo")?.Value ?? "",
                ["hostname"] = context.Request.Host.Host,
                ["header"] = context.Request.Headers["Authorization"].ToString(),
                ["token"] = token,
                ["fecha_termino"] = fecha_termino,
                ["fecha_emision"] = fecha_inicio,
                ["log_code"] = jwt.Claims.FirstOrDefault(c => c.Type == "log_code")?.Value ?? "",
            };

            await EjecutarAUD("TESIMGR.KTS_SEGURIDAD.KTS_SEG_TSRLOGI_AUD", "ADI", payload);
        }

        public async Task<string> EjecutarAUD(string nombreProc, string modo, object? filtro)
        {
            var resultado = new Dictionary<string, object>();
            string mensajeRespuesta = "";

            string jsonData = JsonSerializer.Serialize(filtro);

            using var con = await _dbFactory.CreateConnectionAsync();
            using var cmd = new OracleCommand(nombreProc, (OracleConnection)con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("P_ACCION", OracleDbType.Varchar2).Value = modo;
            cmd.Parameters.Add("P_DATA", OracleDbType.Clob).Value = jsonData;
            cmd.Parameters.Add("P_MSG", OracleDbType.Varchar2, 4000).Direction = ParameterDirection.Output;

            await cmd.ExecuteNonQueryAsync();

            mensajeRespuesta = cmd.Parameters["P_MSG"].Value?.ToString() ?? "Sin mensaje";
            //resultado["mensaje"] = mensajeRespuesta;

            return mensajeRespuesta;
        }

    }
}
