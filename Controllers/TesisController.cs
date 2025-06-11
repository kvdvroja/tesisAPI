using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisAPI.Services;

namespace tesisAPI.Controllers
{
    [ApiController]
    [Route("api/v1/Tesis/{proceso}/{modo}")]
    [Authorize]
    public class TesisController : ControllerBase
    {
        private readonly TesisService _oracle;

        public TesisController(TesisService oracle)
        {
            _oracle = oracle;
        }

        private static readonly Dictionary<string, string> Procedimientos = new()
        {
            { "CUR", "TESIMGR.KTS02_Generar_Tesis.KTS_02GT_TSBTESI_CUR" },
            { "CUR3", "TESIMGR.KTS02_Generar_Tesis.KTS_02GT_TSBTESI_CUR3" },
            { "AUD", "TESIMGR.KTS02_Generar_Tesis.KTS_02GT_TSBTESI_AUD" },
             { "ESDO", "TESIMGR.KTS_GENERAL.KTS_GENE_TSRESDO_CUR" },
            { "COME", "TESIMGR.KTS_GENERAL.KTS_GENE_TSRCOME_CUR" },
        };

        //private static readonly HashSet<string> ModosValidos = new()
        //{
        //    "UNI", "LTA", "LTN", "LNR", "LAP", "TVA", "TUN", "ADI", "ACT", "AIN", "CRI"
        //};

        /// <summary>
        /// Ejecuta el procedimiento para manejar información de Tesis.
        /// </summary>
        /// <param name="proceso">Código del procedimiento (ej: PROC5).</param>
        /// <param name="modo">Modo: UNI, LTA.</param>
        /// <param name="codigo">(no usado aquí) - Dejar vacío o ignorar.</param>
        /// <param name="payload">Datos JSON requeridos para UNI y LTA.</param>
        /// <returns>Datos de tesis en formato JSON.</returns>
        
        /*[HttpPost]
        public async Task<IActionResult> Ejecutar(string proceso, string modo, [FromQuery] string? codigo, [FromBody] object? payload)
        {
            if (!Procedimientos.TryGetValue(proceso, out var nombreProc))
                return BadRequest($"Procedimiento '{proceso}' no encontrado.");

            if (!ModosValidos.Contains(modo))
                return BadRequest($"Modo '{modo}' no permitido.");

            if (payload == null)
                return BadRequest("Debe enviar un JSON válido en el body para esta acción.");

            var result = await _oracle.EjecutarProcedure(nombreProc, modo, payload);
            return Ok(result);
        }*/

        [HttpPost]        
        public async Task<IActionResult> Ejecutar(string proceso, string modo, [FromQuery] string? codigo, [FromBody] object? payload)
        {
            if (!Procedimientos.TryGetValue(proceso, out var nombreProc))
                return BadRequest($"Procedimiento '{proceso}' no encontrado.");

            // if (!ModosValidos.Contains(modo))
            if (!Utilidades.Acciones.TesisModosValidos.Contains(modo))
                return BadRequest($"Modo '{modo}' no permitido.");

            if (payload == null)
                return BadRequest("Debe enviar un JSON válido en el body.");

            // 🔥 Cambiar aquí: identificar si el procedimiento usa 3 cursores
            switch (proceso)
            {
                case "CUR3":
                    var resultadoCur3 = await _oracle.EjecutarProcedureCUR3(nombreProc, modo, payload);
                    return Ok(resultadoCur3);

                case "AUD":
                    var msg = await _oracle.EjecutarAUD(nombreProc, modo, payload);
                    return Ok(new { mensaje = msg  });

                case "CUR":
                    var resultado = await _oracle.EjecutarProcedure(nombreProc, modo, payload);
                    return Ok(resultado);

                case "ESDO":
                    var resultado3 = await _oracle.EjecutarProcedure(nombreProc, modo, payload);
                    return Ok(resultado3);
                case "COME":
                    var resultado4 = await _oracle.EjecutarProcedure(nombreProc, modo, payload);
                    return Ok(resultado4);

                default:
                    return BadRequest("Proceso no reconocido.");
            }


        }



    }
}
