using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace tesisAPI.Services
{
    public class OConnection
    {
        private readonly IConfiguration _config;

        public OConnection(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IDbConnection> CreateConnectionAsync()
        {
            var connection = new OracleConnection(_config.GetConnectionString("DESAConnection"));
            await connection.OpenAsync();
            return connection;
        }
    }
}
