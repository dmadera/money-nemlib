using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

using money_nemlib;

namespace Services
{
    public static class Sql
    {
        public static SqlConnection CreateConnection()
        {
            var databaseConfig = Program.ServiceProvider.GetService<IOptions<DatabaseConfig>>().Value;
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder
            {
                DataSource = databaseConfig.Server,
                UserID = databaseConfig.User,
                Password = databaseConfig.Password,
                InitialCatalog = databaseConfig.Name,
                TrustServerCertificate = true
            };

            return new SqlConnection(builder.ConnectionString);
        }

    }
}
