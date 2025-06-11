using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using tesisAPI.DTOs;
using tesisAPI.Services;

namespace tesisAPI.Controllers
{
    [Route("api/v1/authentication")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly AuthService _authService;

        public AuthController(IConfiguration configuration, AuthService authService)
        {
            _configuration = configuration;
            _authService = authService;
        }

        private static readonly Dictionary<string, string> Procedimientos = new()
        {
            { "LOGI", "TESIMGR.KTS_SEGURIDAD.KTS_SEG_TSRLOGI_CUR" },
            { "LOGA", "TESIMGR.KTS_SEGURIDAD.KTS_SEG_TSRLOGI_AUD" }
        };

        private static readonly HashSet<string> ModosValidos = new()
        {
            "LOG", "CSE", "EST"
        };

        
        [HttpPost("authenticate")]
        public ActionResult<string> Authenticate([FromBody] AuthenticationRequestBody request)
        {
            var user = ValidateUserCredentials(request.UserName, request.Password);
            if (user == null)
            {
                return Unauthorized();
            }

            var securityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_configuration["Authentication:SecretForKey"]!));
            var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claimsForToken = new List<Claim>
            {
                new Claim("sub", user.UserId.ToString()),
                new Claim("nickname", user.UserUpaoId),
                new Claim("name", user.DepartmentOffice)
            };

            var jwtSecurityToken = new JwtSecurityToken(
                _configuration["Authentication:Issuer"],
                _configuration["Authentication:Audience"],
                claimsForToken,
                DateTime.UtcNow,
                DateTime.UtcNow.AddMinutes(60),
                signingCredentials
            );

            var tokenToReturn = new JwtSecurityTokenHandler().WriteToken(jwtSecurityToken);

            return Ok(new { token = tokenToReturn });
        }

        // Simulación: reemplaza por consulta a Oracle si lo deseas
        private InfoUser? ValidateUserCredentials(string? userName, string? password)
        {
            if (userName == "Sys@dminMktBaNNer" && password == "fvc0FU8o9ZGKk0I8efzgCDD1gcQA7OqVc5Ju3pZ8B1OV2LjqdH")
                return new InfoUser(1, "sys", userName ?? "Sys@dminMktBaNNer", "sysFirst", "userLast", "MktOffice");
            else
                return null;
        }
        
        [HttpPost("{proceso}/{modo}")]
        public async Task<IActionResult> Ejecutar(string proceso, string modo, [FromQuery] string? codigo, [FromBody] object? payload)
        {
            if (!Procedimientos.TryGetValue(proceso, out var nombreProc))
                return BadRequest($"Procedimiento '{proceso}' no encontrado.");

            if (!ModosValidos.Contains(modo))
                return BadRequest($"Modo '{modo}' no permitido.");
            
            // 🔥 Cambiar aquí: identificar si el procedimiento usa 3 cursores
            switch (proceso)
            {
                case "LOGI":
                    //var token = payload?.ToString();
                    var jsonElement = (JsonElement)payload!;
                    var token = jsonElement.GetProperty("codigo").GetString();


                    if (string.IsNullOrWhiteSpace(token))
                        return BadRequest(new { error = "Token JWT requerido" });

                    // Desencriptar el token
                    var handler = new JwtSecurityTokenHandler();
                    var jwt = handler.ReadJwtToken(token);

                    // Comparar el client_id del JWT con el del appsettings
                    var expectedClientId = _configuration["Jwt:ClientId"];
                    var clientId = jwt.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value;

                    if (clientId != expectedClientId)
                        return BadRequest(new { error = "ClientId inválido." });

                    // Obtener el 'sub' para consultar en la BD
                    var usuarioId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                    if (string.IsNullOrWhiteSpace(usuarioId))
                        return BadRequest(new { error = "Campo 'sub' no encontrado en el JWT." });

                    // Obtener el 'iat' para enviar en la BD
                    var iat = jwt.Claims.FirstOrDefault(c => c.Type == "iat")?.Value;

                    if (string.IsNullOrWhiteSpace(iat))
                        return BadRequest(new { error = "Campo 'iat' no encontrado en el JWT." });

                    // Obtener el 'exp' para enviar en la BD - expiracion del token
                    var exp = jwt.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;

                    if (string.IsNullOrWhiteSpace(exp))
                        return BadRequest(new { error = "Campo 'exp' no encontrado en el JWT." });

                    // Armar JSON como espera el procedimiento
                    var inputJson = JsonSerializer.Serialize(new { usua_id = usuarioId, iat = iat, exp = exp });

                    var resultado = await _authService.Ejecutar(nombreProc, modo, inputJson);
                    
                    return Ok(resultado);


                case "LOGA":
                    var msg = await _authService.EjecutarAUD(nombreProc, modo, payload);
                    return Ok(new { mensaje = msg });


                default:
                    return BadRequest("Proceso no reconocido.");
            }


            
        }


    }
}
