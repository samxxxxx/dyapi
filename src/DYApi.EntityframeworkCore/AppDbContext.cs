using DYApi.Infrastructure;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DYApi.EntityframeworkCore
{
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public DbSet<WebUrl> WebUrls { get; set; }
        public DbSet<UserParseRecord> UserParseRecords { get; set; }
        public AppDbContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserParseRecord>().Property(r => r.Id)
                //自增长
                .UseIdentityColumn();
        }
    }

    //Unable to create an object of type 'AppDbContext'. For the different patterns supported at design time, see https://go.microsoft.com/fwlink/?linkid=851728
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var connectionString = configuration.GetConnectionString("Default");

            optionsBuilder.UseMySql(connectionString, MySqlServerVersion.AutoDetect(connectionString), optionsBuilder => optionsBuilder.EnableStringComparisonTranslations(true));

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
