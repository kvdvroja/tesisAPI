using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text.Json;

namespace tesisAPI.Services
{
    public class LineaInvService
    {
        private readonly OConnection _factory;

        public LineaInvService(OConnection factory)
        {
            _factory = factory;
        }

        public async Task<List<Dictionary<string, object>>> EjecutarProcedure(string nombreProc, string modo, object? filtro)
        {
            var resultado = new List<Dictionary<string, object>>();
            string dataFinal;

            if (modo == "UNI" || modo == "LCO" || modo == "LTO")
                dataFinal = filtro?.ToString() ?? "";
            else
                dataFinal = filtro != null ? JsonSerializer.Serialize(filtro) : "";

            using var con = await _factory.CreateConnectionAsync();
            using var cmd = new OracleCommand(nombreProc, (OracleConnection)con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add("P_ACCION", OracleDbType.Varchar2).Value = modo;
            cmd.Parameters.Add("P_DATA", OracleDbType.Varchar2).Value = dataFinal;
            cmd.Parameters.Add("CURSOR_OUT", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var fila = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                    fila[reader.GetName(i)] = reader.GetValue(i);
                resultado.Add(fila);
            }

            return resultado;
        }
    }
}
