using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocaLabs.Qa;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using SqlSever.IntegrationTests.DataAccess;

namespace SqlSever.IntegrationTests.Tests
{
    [TestFixture]
    public class WhenExecutingEntityFrameworkQueriesAgainstSqlServerBehindPortBridge : BehaviorDrivenTest
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
        public void It_should_store_all_records()
        {
            _result.Should().HaveCount(5);
        }

        [Then]
        public void It_should_store_correct_values()
        {
            _result[0].Id.Should().NotBeEmpty();
            _result[0].No.Should().Be(1);
            _result[0].Description.Should().Be("Record 1");

            _result[1].Id.Should().NotBeEmpty();
            _result[1].No.Should().Be(2);
            _result[1].Description.Should().Be("Record 2");

            _result[2].Id.Should().NotBeEmpty();
            _result[2].No.Should().Be(3);
            _result[2].Description.Should().Be("Record 3");

            _result[3].Id.Should().NotBeEmpty();
            _result[3].No.Should().Be(4);
            _result[3].Description.Should().Be("Record 4");

            _result[4].Id.Should().NotBeEmpty();
            _result[4].No.Should().Be(5);
            _result[4].Description.Should().Be("Record 5");
        }
    }
}
