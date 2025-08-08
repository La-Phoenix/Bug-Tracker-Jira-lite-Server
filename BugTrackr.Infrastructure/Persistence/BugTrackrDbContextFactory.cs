using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BugTrackr.Infrastructure.Persistence;
public class BugTrackrDbContextFactory : IDesignTimeDbContextFactory<BugTrackrDbContext>
{
    public BugTrackrDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BugTrackrDbContext>();

        // Use your actual database connection string
        optionsBuilder.UseNpgsql("Host=db;Port=5432;Database=bugtrackr;Username=postgres;Password=postgres");
        //optionsBuilder.UseNpgsql("Host=bugtrackr_db;Port=5432;Database=bugtrackr;Username=postgres;Password=postgres");
        //optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=bugtrackr;Username=postgres;Password=postgres");

        return new BugTrackrDbContext(optionsBuilder.Options);
    }
}