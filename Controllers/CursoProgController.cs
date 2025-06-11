using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisAPI.Services;

namespace tesisAPI.Controllers
{
    [ApiController]
    [Route("api/v1/CursoProg/{proceso}/{modo}")]
    [Authorize]
    public class CursoProgController : ControllerBase
    {
        private readonly CursoProgService _oracle;

        public CursoProgController(CursoProgService oracle)
        {
            _oracle = oracle;
        }

        private static readonly Dictionary<string, string> Procedimientos = new()
        {
            { "CUR", "TESIMGR.KTS02_Generar_Tesis.KTS_02GT_TSBSECT_CUR" }
        };

        private static readonly HashSet<string> ModosValidos = new()
        {
            "UNI", "LCA", "LIN", "LIS"
        };

        /// <summary>
        /// Ejecuta el procedimiento para Cursos Programados.
        /// </summary>
        /// <param name="proceso">Nombre del procedimiento (ejemplo: PROC4).</param>
        /// <param name="modo">Modo: UNI, LCA, LIN, LIS.</param>
        /// <param name="codigo">Código único si modo es UNI.</param>
        /// <param name="payload">Datos JSON si modo es LCA, LIN o LIS.</param>
        /// <returns>Resultado del procedimiento en formato JSON.</returns>
        [HttpPost]
        public async Task<IActionResult> Ejecutar(string proceso, string modo, [FromQuery] string? codigo, [FromBody] object? payload)
        {
            if (!Procedimientos.TryGetValue(proceso, out var nombreProc))
                return BadRequest($"Procedimiento '{proceso}' no encontrado.");

            if (!ModosValidos.Contains(modo))
                return BadRequest($"Modo '{modo}' no permitido.");

            if (modo == "UNI" && string.IsNullOrWhiteSpace(codigo))
                return BadRequest("Debe proporcionar un código válido para 'UNI'.");

            if ((modo == "LCA" || modo == "LIN" || modo == "LIS") && payload == null)
                return BadRequest("Debe proporcionar un JSON válido en el body para esta acción.");

            object? data = (modo == "UNI")
                ? codigo
                : payload;

            var result = await _oracle.EjecutarProcedure(nombreProc, modo, data);
            return Ok(result);
        }
    }
}
