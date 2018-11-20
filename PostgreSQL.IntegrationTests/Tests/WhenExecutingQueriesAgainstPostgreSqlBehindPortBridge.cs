using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using DocaLabs.Qa;
using FluentAssertions;
using Npgsql;
using NUnit.Framework;

namespace PostgreSQL.IntegrationTests.Tests
{
    [TestFixture]
    public class WhenExecutingQueriesAgainstPostgreSqlBehindPortBridge : BehaviorDrivenTest
    {
        private static readonly List<(int Id, int RecordValue)> Result = new List<(int, int)>();

        protected override async Task Given()
        {
            using (var connection = new NpgsqlConnection(DbUtils.ConnectionString))
            {
                await connection.OpenAsync();

                await connection.CreateTable();
            }
        }

        protected override async Task When()
        {
            using (var connection = new NpgsqlConnection(DbUtils.ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    await connection.AddRecord(1, 11);
                    await connection.AddRecord(2, 12);
                    await connection.AddRecord(3, 13);
                    await connection.AddRecord(4, 14);
                    await connection.AddRecord(5, 15);

                    var command = new NpgsqlCommand("SELECT Id, RecordValue FROM Records ORDER BY Id", connection);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var id = (int) reader["Id"];

                            var recordValue = (int) reader["RecordValue"];

                            Result.Add((id, recordValue));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
                finally
                {
                    await connection.DropTable();
                }
            }
        }

        [Then]
        public void It_should_store_all_records()
        {
            Result.Should().HaveCount(5);
        }

        [Then]
        public void It_should_store_correct_values()
        {
            Result[0].Id.Should().Be(1);
            Result[0].RecordValue.Should().Be(11);

            Result[1].Id.Should().Be(2);
            Result[1].RecordValue.Should().Be(12);

            Result[2].Id.Should().Be(3);
            Result[2].RecordValue.Should().Be(13);

            Result[3].Id.Should().Be(4);
            Result[3].RecordValue.Should().Be(14);

            Result[4].Id.Should().Be(5);
            Result[4].RecordValue.Should().Be(15);
        }
    }
}
