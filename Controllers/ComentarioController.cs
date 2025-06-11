using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using tesisAPI.Services;

namespace tesisAPI.Controllers
{
    [ApiController]
    [Route("api/v1/Comentario/{proceso}/{modo}")]
    [Authorize]

    public class ComentarioController : ControllerBase
    {

        private readonly TesisService _oracle;

        public ComentarioController(TesisService oracle)
        {
            _oracle = oracle;
        }

        private static readonly Dictionary<string, string> Procedimientos = new()
        {
            { "CUR", "TESIMGR.KTS_GENERAL.KTS_GENE_TSRCOME_CUR" },
            { "CUR3", "TESIMGR.KTS_GENERAL.KTS_GENE_TSRPERS_CUR3" },
            { "AUD", "TESIMGR.KTS_GENERAL.KTS_GENE_TSRPERS_AUD" },
        };



        [HttpPost]
        public async Task<IActionResult> Ejecutar(string proceso, string modo, [FromQuery] string? codigo, [FromBody] object? payload)
        {
            if (!Procedimientos.TryGetValue(proceso, out var nombreProc))
                return BadRequest($"Procedimiento '{proceso}' no encontrado.");

            // if (!ModosValidos.Contains(modo))
            if (!Utilidades.Acciones.ComentarioModosValidos.Contains(modo))
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
                    return Ok(new { mensaje = msg });

                case "CUR":
                    var resultado = await _oracle.EjecutarProcedure(nombreProc, modo, payload);
                    return Ok(resultado);

                default:
                    return BadRequest("Proceso no reconocido.");
            }


        }
    }
}
