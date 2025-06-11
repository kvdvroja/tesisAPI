using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Text.Json;

namespace tesisAPI.Services
{
    public class TesisDesaService
    {
        private readonly OConnection _factory;

        public TesisDesaService(OConnection factory)
        {
            _factory = factory;
        }

        public async Task<List<Dictionary<string, object>>> EjecutarCUR(string nombreProc, string modo, object? filtro)
        {
            var resultado = new List<Dictionary<string, object>>();
            string dataFinal;

            if (modo == "UNI")
                dataFinal = filtro != null ? JsonSerializer.Serialize(filtro) : "";
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

        public async Task<string> EjecutarAUD(string nombreProc, string modo, object? filtro)
        {
            var resultado = new Dictionary<string, object>();
            string mensajeRespuesta = "";

            string jsonData = JsonSerializer.Serialize(filtro);

            using var con = await _factory.CreateConnectionAsync();
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
