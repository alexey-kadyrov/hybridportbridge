using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using DocaLabs.Qa;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SqlSever.IntegrationTests.DataAccess;

namespace SqlSever.IntegrationTests.Tests
{
    [TestFixture]
    public class WhenExecutingEntityFrameworkQueriesAgainstSqlServerBehindPortBridgeInCancelledTransaction : BehaviorDrivenTest
    {
        private static List<Product> _result;

        protected override async Task Cleanup()
        {
            using (var context = new ApplicationDataContext())
            {
                await context.Database.EnsureDeletedAsync();
            }
        }

        protected override async Task Given()
        {
            using (var context = new ApplicationDataContext())
            {
                await context.Database.EnsureCreatedAsync();
            }
        }

        protected override async Task When()
        {
            using (new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            using (var context = new ApplicationDataContext())
            {
                try
                {
                    context.Add(new Product
                    {
                        Id = Guid.NewGuid(),
                        No = 1,
                        Description = "Record 1"
                    });
                    context.Add(new Product
                    {
                        Id = Guid.NewGuid(),
                        No = 2,
                        Description = "Record 2"
                    });
                    context.Add(new Product
                    {
                        Id = Guid.NewGuid(),
                        No = 3,
                        Description = "Record 3"
                    });
                    context.Add(new Product
                    {
                        Id = Guid.NewGuid(),
                        No = 4,
                        Description = "Record 4"
                    });
                    context.Add(new Product
                    {
                        Id = Guid.NewGuid(),
                        No = 5,
                        Description = "Record 5"
                    });

                    await context.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
            
            using (var context = new ApplicationDataContext())
            {
                try
                {
                    _result = await context.Products
                        .OrderBy(e => e.No)
                        .ToListAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }

        [Then]
        public void It_should_not_store_any_records()
        {
            _result.Should().BeEmpty();
        }
    }
}