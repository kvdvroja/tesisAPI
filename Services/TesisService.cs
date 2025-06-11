using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using tesisAPI.DTOs;

namespace tesisAPI.Services
{
    public class TesisService
    {
        private readonly OConnection _factory;

        public TesisService(OConnection factory)
        {
            _factory = factory;
        }

        public async Task<Dictionary<string, List<Dictionary<string, object>>>> EjecutarProcedureCUR3(string nombreProc, string modo, object filtro)
        {
            var resultado = new Dictionary<string, List<Dictionary<string, object>>>();

            using var con = await _factory.CreateConnectionAsync();
            using var cmd = new OracleCommand(nombreProc, (OracleConnection)con)
            {
                CommandType = CommandType.StoredProcedure,
                BindByName = true // ¡Esto es importante! Sin esto, Oracle usa el ORDEN, no el nombre
            };

            string jsonData = JsonSerializer.Serialize(filtro);

            // RESPETA EL ORDEN Y NOMBRES
            cmd.Parameters.Add(new OracleParameter("P_ACCION", OracleDbType.Varchar2, 50, modo, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("P_DATA", OracleDbType.Clob, jsonData, ParameterDirection.Input));
            cmd.Parameters.Add(new OracleParameter("CURSOR_OUT", OracleDbType.RefCursor) { Direction = ParameterDirection.Output });
            cmd.Parameters.Add(new OracleParameter("CURSOR_OUT1", OracleDbType.RefCursor) { Direction = ParameterDirection.Output });
            cmd.Parameters.Add(new OracleParameter("CURSOR_OUT2", OracleDbType.RefCursor) { Direction = ParameterDirection.Output });

            using var reader = await cmd.ExecuteReaderAsync();

            if (reader != null)
            {
                var tesis = await ReadCursor(reader);
                resultado.Add("tesis", tesis);

                if (await reader.NextResultAsync())
                {
                    var desarrollo = await ReadCursor(reader);
                    resultado.Add("desarrollo", desarrollo);
                }

                if (await reader.NextResultAsync())
                {
                    var integrantes = await ReadCursor(reader);
                    resultado.Add("integrantes", integrantes);
                }
            }

            return resultado;
        }

        //private async Task<List<Dictionary<string, object>>> ReadCursor(OracleDataReader reader)
        private async Task<List<Dictionary<string, object>>> ReadCursor(DbDataReader reader)
        {
            var lista = new List<Dictionary<string, object>>();

            while (await reader.ReadAsync())
            {
                var fila = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    fila[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                lista.Add(fila);
            }

            return lista;
        }


        public async Task<List<Dictionary<string, object>>> EjecutarProcedure(string nombreProc, string modo, object? filtro)
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

        //public async Task<string> EjecutarAUD(string nombreProc, string modo, object? filtro)
        //public async Task<Dictionary<string, object>> EjecutarAUD(string nombreProc, string modo, object? filtro)
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
