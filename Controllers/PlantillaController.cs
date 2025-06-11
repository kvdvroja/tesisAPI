using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisAPI.Services;

namespace tesisAPI.Controllers
{
    [ApiController]
    [Route("api/v1/Plantilla/{proceso}/{modo}")]
    [Authorize]
    public class PlantillaController : ControllerBase
    {
        private readonly PlantillaService _oracle;

        public PlantillaController(PlantillaService oracle)
        {
            _oracle = oracle;
        }

        private static readonly Dictionary<string, string> Procedimientos = new()
        {
            { "CUR", "TESIMGR.KTS02_Generar_Tesis.KTS_02GT_TSRPLAN_CUR" }
        };

        private static readonly HashSet<string> ModosValidos = new()
        {
            "UNI", "LCO", "LTO", "LIS"
        };

        /// <summary>
        /// Ejecuta un procedimiento dinámico para Plantillas de trabajo.
        /// </summary>
        /// <param name="proceso">Código del procedimiento (ej: PROC3).</param>
        /// <param name="modo">Modo: UNI, LCO, LTO, LIS.</param>
        /// <param name="codigo">Código único para UNI (ejemplo: 0049).</param>
        /// <param name="payload">JSON para LIS (listado paginado).</param>
        /// <returns>Listado o único registro de plantilla.</returns>
        [HttpPost]
        public async Task<IActionResult> Ejecutar(string proceso, string modo, [FromQuery] string? codigo, [FromBody] object? payload)
        {
            if (!Procedimientos.TryGetValue(proceso, out var nombreProc))
                return BadRequest($"Procedimiento '{proceso}' no encontrado.");

            if (!ModosValidos.Contains(modo))
                return BadRequest($"Modo '{modo}' no permitido.");

            if (modo == "UNI" && string.IsNullOrWhiteSpace(codigo))
                return BadRequest("Debe proporcionar un código válido para la acción 'UNI'.");

            if (modo == "LIS" && payload == null)
                return BadRequest("Debe proporcionar un JSON válido para la acción 'LIS'.");

            object? data = (modo == "UNI" || modo == "LCO" || modo == "LTO")
                ? codigo
                : payload;

            var result = await _oracle.EjecutarProcedure(nombreProc, modo, data);
            return Ok(result);
        }
    }
}
