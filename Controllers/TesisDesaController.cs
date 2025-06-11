using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using tesisAPI.Exceptions;
using tesisAPI.Services;

namespace tesisAPI.Controllers
{
    [ApiController]
    [Route("api/v1/TesisDesa/{proceso}/{modo}")]
    [Authorize]
    public class TesisDesaController : ControllerBase
    {
        private readonly TesisDesaService _oracle;

        public TesisDesaController(TesisDesaService oracle)
        {
            _oracle = oracle;
        }

        private static readonly Dictionary<string, string> Procedimientos = new()
        {
            { "CUR", "TESIMGR.KTS02_Generar_Tesis.KTS_02GT_TSRDESA_CUR" },
            { "DESH", "TESIMGR.KTS03_DESARROLLO_TESIS.KTS_02GT_TSRDESH_CUR" },
            { "DESA", "TESIMGR.KTS03_DESARROLLO_TESIS.KTS_02GT_TSRDESA_AUD" },
           
        };

        ////private static readonly HashSet<string> ModosValidos = new()
        ////{
        ////    "UNI", "LFP", "LTF", "LIS", "LDA", "APD", "ACT"
        ////};

        /// <summary>
        /// Ejecuta el procedimiento para el desarrollo de la tesis.
        /// </summary>
        /// <param name="proceso">Código del procedimiento (ej: PROC6).</param>
        /// <param name="modo">Modo de operación: UNI, LFP, LTF, LIS.</param>
        /// <param name="codigo">(no usado) - Dejar vacío o ignorar.</param>
        /// <param name="payload">Body JSON con filtros necesarios.</param>
        /// <returns>Listado o detalle de tesis en desarrollo.</returns>
        [HttpPost]
        public async Task<IActionResult> Ejecutar(string proceso, string modo, [FromQuery] string? codigo, [FromBody] object? payload)
        {
            if (!Procedimientos.TryGetValue(proceso, out var nombreProc))
                return BadRequest($"Procedimiento '{proceso}' no encontrado.");

            // if (!ModosValidos.Contains(modo))
            if (!Utilidades.Acciones.DesaModosValidos.Contains(modo))
                return BadRequest($"Modo '{modo}' no permitido.");

            if (payload == null)
                return BadRequest("Debe enviar un JSON válido en el body.");

            // 🔥 Cambiar aquí: identificar si el procedimiento usa 3 cursores
            switch (proceso)
            {
                case "CUR":
                    var resultado1 = await _oracle.EjecutarCUR(nombreProc, modo, payload);
                    return Ok(resultado1);

                case "DESH":
                    var resultado2 = await _oracle.EjecutarCUR(nombreProc, modo, payload);
                    return Ok(resultado2);
               
                //default: 
                case "DESA":
                    var msg = "";
                    switch (modo)
                    {
                        case "APD":

                            var jsonElement = (JsonElement)payload!;
                            var estado = jsonElement.GetProperty("estado").GetString();
                            var comentario = jsonElement.GetProperty("estado").GetString().Length;

                            if ((estado?.ToString() == "A" || estado?.ToString() == "O") && comentario == 0)
                                throw new BusinessException("Debe agregar un comentario");

                            if (estado?.ToString() == "A" || estado?.ToString() == "O")
                                await ValidarAdjunto(payload);

                            msg = await _oracle.EjecutarAUD(nombreProc, modo, payload);
                            return Ok(new { mensaje = msg });
                        default:
                            msg = await _oracle.EjecutarAUD(nombreProc, modo, payload);
                            return Ok(new { mensaje = msg });

                    }                    

                default:
                    return BadRequest("Proceso no reconocido.");
            }
        }

        private async Task ValidarAdjunto(object? payload)
        {
            string jsonData = JsonSerializer.Serialize(payload);

            if (jsonData is not string jsonString)
                throw new BusinessException("El dato JSON no tiene el formato esperado.");

            var resultado = await _oracle.EjecutarCUR(
                "TESIMGR.KTS_CONFIGURACION.KTS_CONFIGURACION_GEN_CUR",
                "ADJ",
                JsonDocument.Parse(jsonData).RootElement.Clone()  // PASA EL JSON COMO STRING YA SERIALIZADO
            );
            if (resultado.Count == 0)
                //BadRequest("No se encontró configuración para el archivo.");
                throw new BusinessException("No se encontró configuración para el archivo.");

            var config = resultado.First();
            
            if (!config.TryGetValue("PERMITE", out var permite) &&  (permite?.ToString() == "S" || permite?.ToString() == "N"))
                //throw new Exception("Este subproceso no permite adjuntar archivos.");
                throw new BusinessException("Variable de permiso no reconocida.");

            if (permite?.ToString() == "S" )
                //throw new Exception("Este subproceso no permite adjuntar archivos.");
                if(!config.TryGetValue("CANT_ADJ", out var adj) || adj?.ToString() == "0")
                throw new BusinessException("Para aprobar el nivel es necesario que la tarea tenga un adjunto.");

        }

    }
}
