using Microsoft.EntityFrameworkCore;

namespace SqlSever.IntegrationTests.DataAccess
{
    public class ApplicationDataContext : DbContext
    {
        private const string ConnectionString = "Server=tcp:localhost,14334;Initial Catalog=TestDb;User ID=sa;Password=MyEdition2017!;Connection Timeout=10;";
        
        public DbSet<Product> Products { get; set; }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(ConnectionString);
            
            base.OnConfiguring(optionsBuilder);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(e =>
            {
                e.HasKey(p => p.Id);

                e.Property(p => p.No)
                    .IsRequired();

                e.Property(p => p.Description)
                    .HasMaxLength(100)
                    .IsRequired()
                    .IsUnicode();
            });
        }
    }
}