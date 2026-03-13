using Microsoft.Data.SqlClient;

namespace Claytree.Risk.Functions.Services;

internal static class SqlHelper
{
    public static string FixSqlConnectionString(string cs)
    {
        var b = new SqlConnectionStringBuilder(cs);
        if (b.ConnectTimeout < 60) b.ConnectTimeout = 90;
        if (b.MaxPoolSize < 50) b.MaxPoolSize = 50;
        if (!b.ContainsKey("ConnectRetryCount") || b.ConnectRetryCount < 3) b.ConnectRetryCount = 3;
        if (!b.ContainsKey("ConnectRetryInterval") || b.ConnectRetryInterval < 10) b.ConnectRetryInterval = 10;
        return b.ConnectionString;
    }
}
