using BugTrackr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(BugTrackrDbContext context, ILogger logger)
    {
        const int maxRetries = 10;
        const int delaySeconds = 5;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                logger.LogInformation("⏳ Attempt {Attempt} - Applying migrations...", attempt);
                await context.Database.MigrateAsync();

                // Seed Statuses
                if (!await context.Statuses.AnyAsync())
                {
                    context.Statuses.AddRange(
                        new Status { Name = "Todo" },
                        new Status { Name = "In Progress" },
                        new Status { Name = "Done" },
                        new Status { Name = "Closed" }
                    );
                    logger.LogInformation("✅ Seeded Statuses");
                }

                // Seed Priorities
                if (!await context.Priorities.AnyAsync())
                {
                    context.Priorities.AddRange(
                        new Priority { Name = "Low" },
                        new Priority { Name = "Medium" },
                        new Priority { Name = "High" },
                        new Priority { Name = "Critical" }
                    );
                    logger.LogInformation("✅ Seeded Priorities");
                }

                await context.SaveChangesAsync();
                logger.LogInformation("🎉 Database ready and seeded.");
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "❌ Attempt {Attempt} failed.", attempt);
                if (attempt == maxRetries) throw;
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }
}
