using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Text.Json;
using tesisAPI.Exceptions;
using tesisAPI.Helper;
using tesisAPI.Services;

namespace tesisAPI.Controllers
{

    [Route("api/v1/[controller]")]
    [ApiController]
    [Authorize]
    public class TareaController : ControllerBase
    {
        private readonly TareaService _oracle;
        private readonly IConfiguration _config;
        private readonly ArchivoService _archivoService;

        public TareaController(TareaService oracle, IConfiguration config, ArchivoService archivoService)
        {
            _oracle = oracle;
            _config = config;
            _archivoService = archivoService;
        }

        private static readonly Dictionary<string, string> Procedimientos = new()
        {
            { "AUD", "TESIMGR.KTS03_DESARROLLO_TESIS.KTS_02GT_TSRDESH_AUD" }
        };

        private static readonly HashSet<string> ModosValidos = new()
        {
            "ADJ", "ADI"
        };

        [HttpPost("{proceso}/{modo}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Ejecutar(string proceso, string modo, [FromQuery] string? codigo, [FromForm] IFormFile? file, [FromForm] string? jsonData)
        {
            if (!Procedimientos.TryGetValue(proceso, out var nombreProc))
                return BadRequest($"Procedimiento '{proceso}' no encontrado.");

            //if (!ModosValidos.Contains(modo))
            if (!Utilidades.Acciones.TareaModosValidos.Contains(modo))
                return BadRequest($"Modo '{modo}' no permitido.");
            
            var userId = JwtHelper.ObtenerIdDesdeToken(HttpContext).ToString();

            switch (proceso)
            {
                case "AUD":
                    if (string.IsNullOrWhiteSpace(jsonData))
                        return BadRequest("Debe incluir los metadatos JSON en 'jsonData'.");

                    var root = JsonDocument.Parse(jsonData).RootElement;

                    switch (modo)
                    {
                        case "ADJ":
                            if (file == null)
                                return BadRequest("Debe adjuntar un archivo.");

                            // Subir archivo a API externa
                            var resultadoArchivo = await SubirArchivo(file, userId, jsonData);

                            // Armar nuevo JSON con campos adicionales
                            var payloadDict = new Dictionary<string, object>();
                            foreach (var prop in root.EnumerateObject())
                                payloadDict[prop.Name] = prop.Value.GetString();

                            payloadDict["nombre_archivo"] = resultadoArchivo?.GetType()?.GetProperty("nombreFinal")?.GetValue(resultadoArchivo) ?? "";
                            payloadDict["extension"] = resultadoArchivo?.GetType()?.GetProperty("extension")?.GetValue(resultadoArchivo) ?? "";
                            payloadDict["peso"] = resultadoArchivo?.GetType()?.GetProperty("peso")?.GetValue(resultadoArchivo).ToString() ?? "";
                            payloadDict["ruta_relativa"] = resultadoArchivo?.GetType()?.GetProperty("rutaRelativa")?.GetValue(resultadoArchivo) ?? "";
                            payloadDict["usua_reg"] = userId;
                            payloadDict["usua_id"] = userId;
                            payloadDict["ruta_base"] = _config["UploadConfig:RutaServisor"]?.ToString();

                            // Ejecutar procedimiento almacenado
                            var mensaje = await _oracle.EjecutarAUD(nombreProc, modo, payloadDict);

                            return Ok(new
                            {
                                mensaje,
                                archivo = resultadoArchivo
                            });

                        default:
                            var payload = new Dictionary<string, object>();
                            foreach (var prop in root.EnumerateObject())
                                payload[prop.Name] = prop.Value.GetString();

                            payload["usua_reg"] = userId;
                            payload["usua_id"] = userId;

                            var mensajeAct = await _oracle.EjecutarAUD(nombreProc, modo, payload);
                            return Ok(new { mensaje = mensajeAct });

                    }

                default:
                    return BadRequest("Proceso no reconocido.");
            }

        }

        private async Task<object> SubirArchivo(IFormFile file, string usuario, string jsonData, CancellationToken ct = default)
        {
            var rutaInterna = _config["UploadConfig:RutaInterna"];      // wwwroot/upload/tesis
            var rutaExterna = _config["UploadConfig:RutaExterna"];      // upload/tesis
            var dominioPublico = "https://static.upao.edu.pe";          // tu dominio final (reemplaza localhost)

            var anio = DateTime.Now.Year.ToString();
            var carpetaAnio = Path.Combine(rutaInterna, anio);          // Ej: wwwroot/upload/tesis/2025

            if (!Directory.Exists(carpetaAnio))
                Directory.CreateDirectory(carpetaAnio);

            var extension = Path.GetExtension(file.FileName);
            var pesoArchivo = file.Length;

            // 🔍 VALIDAR PRIMERO
            await ValidarAdjunto(jsonData, extension, pesoArchivo);

            // 📄 Crear nombre de archivo único
            var nombreFinal = $"{usuario}-{Guid.NewGuid():N}-{DateTime.Now:yyyyMMddHHmmss}{extension}";
            var rutaFinalLocal = Path.Combine(carpetaAnio, nombreFinal);

            // 💾 Guardar archivo en disco
            await using (var stream = new FileStream(rutaFinalLocal, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 2_048_576))
                await file.CopyToAsync(stream, ct);

            // 🌐 Construir ruta relativa y URL completa accesible públicamente
            var rutaRelativa = $"/{rutaExterna}/{anio}/{nombreFinal}";
            var urlCompleta = $"{dominioPublico}/{rutaRelativa}";

            return new
            {
                nombreFinal,
                extension,
                peso = pesoArchivo,
                url = urlCompleta,
                rutaRelativa
            };
        }

        private async Task ValidarAdjunto(string jsonData, string extensionArchivo, long pesoBytes)
        {
            var resultado = await _oracle.EjecutarCUR(
                "TESIMGR.KTS_CONFIGURACION.KTS_CONFIGURACION_GEN_CUR",
                "ADJ",
                JsonDocument.Parse(jsonData).RootElement.Clone()  // PASA EL JSON COMO STRING YA SERIALIZADO
            );
            if (resultado.Count == 0)
                //BadRequest("No se encontró configuración para el archivo.");
                throw new BusinessException("No se encontró configuración para el archivo.");


            var config = resultado.First();

            if (!config.TryGetValue("VAL_AUTOR", out var autor) || autor?.ToString() == "0")
                //throw new Exception("Este subproceso no permite adjuntar archivos.");
                throw new BusinessException("Esta tesis no tiene autores.");

            if (!config.TryGetValue("PERMITE", out var permite) || permite?.ToString() != "S")
                //throw new Exception("Este subproceso no permite adjuntar archivos.");
                throw new BusinessException("Esta tesis no permite adjuntar archivos.");

            // Validar formato
            if (config.TryGetValue("FORMATO", out var formato))
            {
                var extensionesPermitidas = formato?.ToString()?.ToLower().Split(',') ?? Array.Empty<string>();
                var extensionNormalizada = extensionArchivo.Trim().ToLower().TrimStart('.');

                if (!extensionesPermitidas.Contains(extensionNormalizada))
                    //throw new Exception($"Extensión de archivo '{extensionArchivo}' no permitida. Permitidas: {string.Join(", ", extensionesPermitidas)}");
                    throw new BusinessException($"Extensión de archivo '{extensionArchivo}' no permitida. Permitidas: {string.Join(", ", extensionesPermitidas)}");
            }

            // Validar peso máximo
            if (config.TryGetValue("PESO", out var pesoPermitidoRaw))
            {
                if (decimal.TryParse(pesoPermitidoRaw?.ToString(), out var pesoMaxMb))
                {
                    var pesoMaxBytes = pesoMaxMb * 1024 * 1024; // convertir MB a bytes
                    if (pesoBytes > pesoMaxBytes)
                        //throw new Exception($"El archivo excede el peso máximo permitido de {pesoMaxMb} MB.");
                        throw new BusinessException($"El archivo excede el peso máximo permitido de {pesoMaxMb} MB.");
                }
            }
        }

    }
}
