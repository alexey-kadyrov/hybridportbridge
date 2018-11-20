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
    public class WhenExecutingQueriesAgainstPostgreSqlBehindPortBridgeInCancelledTransaction : BehaviorDrivenTest
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
            using (var trx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
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

                    trx.Dispose();

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
        public void It_should_not_store_any_records()
        {
            Result.Should().BeEmpty();
        }
    }
}