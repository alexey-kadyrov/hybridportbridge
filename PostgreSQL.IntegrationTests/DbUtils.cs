using System.Threading.Tasks;
using Npgsql;

namespace PostgreSQL.IntegrationTests
{
    public static class DbUtils
    {
        public const string ConnectionString = "Host=localhost;port=5041;username=postgres;Password=password;Database=postgres";

        private const string CreateTableClause = "CREATE TABLE Records(Id int NOT NULL, RecordValue int NOT NULL)";
        private const string DropTableClause = "DROP TABLE Records";

        public static async Task CreateTable(this NpgsqlConnection connection)
        {
            using (var command = new NpgsqlCommand(CreateTableClause, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }

        public static async Task DropTable(this NpgsqlConnection connection)
        {
            try
            {
                using (var command = new NpgsqlCommand(DropTableClause, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // intentional
            }
        }

        public static async Task AddRecord(this NpgsqlConnection connection, int id, int recordValue)
        {
            using (var command = new NpgsqlCommand("INSERT INTO Records (Id, RecordValue) VALUES(@Id, @RecordValue)", connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@RecordValue", recordValue);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
