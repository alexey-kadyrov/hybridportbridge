using System;

namespace SqlSever.IntegrationTests.DataAccess
{
    public class Product
    {
        public Guid Id { get; set; }
        public int No { get; set; }
        public string Description { get; set; }
    }
}