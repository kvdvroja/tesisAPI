using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using tesisAPI.Services;

namespace tesisAPI.Controllers
{
    [ApiController]
    [Route("api/v1/LineaInv/{proceso}/{modo}")]
    [Authorize]
    public class LineaInvController : ControllerBase
    {
        private readonly LineaInvService _oracle;

        public LineaInvController(LineaInvService oracle)
        {
            _oracle = oracle;
        }

        private static readonly Dictionary<string, string> Procedimientos = new()
        {
            { "CUR", "TESIMGR.KTS02_Generar_Tesis.KTS_02GT_TSVLIIN_CUR" },
            { "AUD", "TESIMGR.OTRO_PROCEDIMIENTO_CUR" }
        };

        //private static readonly HashSet<string> ModosValidos = new()
        //{
        //    "UNI", "LCO", "LTO", "LML", "LIS"
        //};

        /// <summary>
        /// Ejecuta un procedimiento dinámico para Líneas de Investigación.
        /// </summary>
        /// <param name="proceso">Código del procedimiento configurado (ej: PROC1, PROC2).</param>
        /// <param name="modo">Modo de ejecución: UNI (único), LCO (combo), LTO (todos), LIS (listado paginado).</param>
        /// <param name="codigo">Código único solo cuando modo es UNI (ejemplo: ID de línea).</param>
        /// <param name="payload">JSON solo cuando modo es LIS (ejemplo: { "nre": "10", "pag": "1", "b": "" }).</param>
        /// <returns>Retorna un listado o un registro dependiendo del modo.</returns>
        [HttpPost]
        public async Task<IActionResult> Ejecutar(string proceso, string modo, [FromQuery] string? codigo, [FromBody] object? payload)
        {
            if (!Procedimientos.TryGetValue(proceso, out var nombreProc))
                return BadRequest($"Procedimiento '{proceso}' no encontrado.");

            if (!Utilidades.Acciones.LineasModosValidos.Contains(modo))
                return BadRequest($"Modo '{modo}' no permitido.");

            if (modo == "UNI" && string.IsNullOrWhiteSpace(codigo))
                return BadRequest("Debe proporcionar un código válido para la acción 'UNI'.");

            if (modo == "LIS" && payload == null)
                return BadRequest("Debe proporcionar un JSON válido en el body para la acción 'LIS'.");

            object? data = (modo == "UNI" || modo == "LCO" || modo == "LTO")
                ? codigo
                : payload;

            var result = await _oracle.EjecutarProcedure(nombreProc, modo, data);
            return Ok(result);
        }
    }
}
