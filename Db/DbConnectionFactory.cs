using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Claytree.Risk.Functions.Db
{
    public class DbConnectionFactory
    {
        private readonly IConfiguration _config;

        public DbConnectionFactory(IConfiguration config)
        {
            _config = config;
        }

        public IDbConnection Create()
        {
            var conn = _config.GetConnectionString("RiskDb");
            return new SqlConnection(conn);
        }
    }
}
