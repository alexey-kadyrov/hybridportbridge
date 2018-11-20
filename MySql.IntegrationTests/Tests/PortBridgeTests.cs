using System;
using System.Threading.Tasks;
using FluentAssertions;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace MySql.IntegrationTests.Tests
{
    [TestFixture]
    public class PortBridgeTests
    {
        private const string ConnectionString = "Server=127.0.0.1;Port=3305;Protocol=TCP;Username=root;Password=password;Database=test-db";
        private const string CreateTableClause = "CREATE TABLE Records(Id int NOT NULL, RecordValue int NOT NULL)";

        private const string DropTableClause = "DROP TABLE Records";


        [Test]
        public async Task Should_execute_queries_against_mysql_behind_port_bridge()
        {
            using (var connection = new MySqlConnection(ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    await CreateTable(connection);

                    await AddRecord(connection, 1, 11);
                    await AddRecord(connection, 2, 12);
                    await AddRecord(connection, 3, 13);
                    await AddRecord(connection, 4, 14);
                    await AddRecord(connection, 5, 15);

                    var command = new MySqlCommand("SELECT Id, RecordValue FROM Records ORDER BY Id", connection);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        var id = 0;

                        while (await reader.ReadAsync())
                        {
                            id++;

                            reader["Id"].Should().Be(id);
                            reader["RecordValue"].Should().Be(10 + id);
                        }

                        id.Should().Be(5);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                finally
                {
                    await DropTable(connection);
                }
            }
        }

        private static async Task CreateTable(MySqlConnection connection)
        {
            using (var command = new MySqlCommand(CreateTableClause, connection))
            {
                await command.ExecuteNonQueryAsync();
            }
        }

        private static async Task DropTable(MySqlConnection connection)
        {
            try
            {
                using (var command = new MySqlCommand(DropTableClause, connection))
                {
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch
            {
                // intentional
            }
        }

        private static async Task AddRecord(MySqlConnection connection, int id, int recordValue)
        {
            using (var command = new MySqlCommand("INSERT INTO Records (Id, RecordValue) VALUES(@Id, @RecordValue)", connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@RecordValue", recordValue);

                await command.ExecuteNonQueryAsync();
            }
        }
    }
}
