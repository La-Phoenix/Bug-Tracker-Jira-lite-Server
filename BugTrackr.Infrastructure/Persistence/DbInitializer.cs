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

                // Only run migrations if not using InMemory
                if (context.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
                {
                    await context.Database.MigrateAsync();
                }
                else
                {
                    await context.Database.EnsureCreatedAsync();
                }

                // Seed Admin User ONLY if no admin exists
                var adminExists = await context.Users.AnyAsync(u => u.Role == "Admin");
                if (!adminExists)
                {
                    var adminUser = new User
                    {
                        Name = "System Administrator",
                        Email = "admin@bugtrackr.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!@#"),
                        Role = "Admin",
                        CreatedAt = DateTime.UtcNow
                    };

                    await context.Users.AddAsync(adminUser);
                    await context.SaveChangesAsync();
                    logger.LogInformation("✅ Admin user created successfully - Email: {Email}", adminUser.Email);
                }
                else
                {
                    logger.LogInformation("ℹ️ Admin user already exists, skipping admin creation");
                }

                // Seed Statuses
                if (!await context.Statuses.AnyAsync())
                {
                    var statuses = new[]
                    {
                        new Status { Name = "Todo" },
                        new Status { Name = "In Progress" },
                        new Status { Name = "Done" },
                        new Status { Name = "Closed" }
                    };

                    await context.Statuses.AddRangeAsync(statuses);
                    await context.SaveChangesAsync();
                    logger.LogInformation("✅ Statuses seeded successfully");
                }

                // Seed Priorities
                if (!await context.Priorities.AnyAsync())
                {
                    var priorities = new[]
                    {
                        new Priority { Name = "Low" },
                        new Priority { Name = "Medium" },
                        new Priority { Name = "High" },
                        new Priority { Name = "Critical" }
                    };

                    await context.Priorities.AddRangeAsync(priorities);
                    await context.SaveChangesAsync();
                    logger.LogInformation("✅ Priorities seeded successfully");
                }

                // Seed Projects (AFTER users are saved)
                if (!await context.Projects.AnyAsync())
                {
                    var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");

                    if (adminUser != null)
                    {
                        var projects = new[]
                        {
                            new Project
                            {
                                Name = "Sample Project",
                                Description = "A sample project for testing",
                                CreatedById = adminUser.Id,
                                CreatedAt = DateTime.UtcNow
                            },
                            new Project
                            {
                                Name = "Bug Tracker Development",
                                Description = "Development of the bug tracking system",
                                CreatedById = adminUser.Id,
                                CreatedAt = DateTime.UtcNow
                            }
                        };

                        await context.Projects.AddRangeAsync(projects);
                        await context.SaveChangesAsync();
                        logger.LogInformation("✅ Projects seeded successfully");
                    }
                    else
                    {
                        logger.LogWarning("⚠️ No admin user found, skipping project creation");
                    }
                }

                // Seed Labels (optional)
                if (!await context.Labels.AnyAsync())
                {
                    var labels = new[]
                    {
                        new Label { Name = "Bug", Color = "#ff0000" },
                        new Label { Name = "Feature", Color = "#00ff00" },
                        new Label { Name = "Enhancement", Color = "#0000ff" },
                        new Label { Name = "Documentation", Color = "#ffff00" }
                    };

                    await context.Labels.AddRangeAsync(labels);
                    await context.SaveChangesAsync();
                    logger.LogInformation("✅ Labels seeded successfully");
                }

                // ENHANCED: Add ALL existing users to Sample Project
                await EnsureAllUsersInSampleProject(context, logger);

                logger.LogInformation("🎉 Database ready and seeded.");
                return;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "❌ Attempt {Attempt} failed: {Message}", attempt, ex.Message);
                if (attempt == maxRetries) throw;
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
            }
        }
    }

    /// <summary>
    /// Ensures ALL existing users are added to the Sample Project if they're not already members
    /// </summary>
    private static async Task EnsureAllUsersInSampleProject(BugTrackrDbContext context, ILogger logger)
    {
        try
        {
            // Get Sample Project
            var sampleProject = await context.Projects
                .FirstOrDefaultAsync(p => p.Name == "Sample Project");

            if (sampleProject == null)
            {
                logger.LogWarning("⚠️ Sample Project not found, skipping user assignment");
                return;
            }

            // Get all users
            var allUsers = await context.Users.ToListAsync();

            if (!allUsers.Any())
            {
                logger.LogWarning("⚠️ No users found in database");
                return;
            }

            // Get existing ProjectUser relationships for Sample Project
            var existingProjectUsers = await context.ProjectUsers
                .Where(pu => pu.ProjectId == sampleProject.Id)
                .Select(pu => pu.UserId)
                .ToListAsync();

            // Find users not yet in Sample Project
            var usersNotInProject = allUsers
                .Where(u => !existingProjectUsers.Contains(u.Id))
                .ToList();

            if (!usersNotInProject.Any())
            {
                logger.LogInformation("ℹ️ All users already in Sample Project ({Count} users)", allUsers.Count);
                return;
            }

            // Create ProjectUser entries for users not in the project
            var newProjectUsers = new List<ProjectUser>();

            foreach (var user in usersNotInProject)
            {
                var roleInProject = user.Role == "Admin" ? "Owner" : "Member";

                newProjectUsers.Add(new ProjectUser
                {
                    ProjectId = sampleProject.Id,
                    UserId = user.Id,
                    RoleInProject = roleInProject
                });
            }

            // Add all new ProjectUser relationships
            await context.ProjectUsers.AddRangeAsync(newProjectUsers);
            await context.SaveChangesAsync();

            logger.LogInformation("✅ Added {NewCount} users to Sample Project. Total users in project: {TotalCount}",
                newProjectUsers.Count,
                existingProjectUsers.Count + newProjectUsers.Count);

            // Log details of added users
            foreach (var projectUser in newProjectUsers)
            {
                var user = allUsers.First(u => u.Id == projectUser.UserId);
                logger.LogInformation("   → Added {UserName} ({Email}) as {Role}",
                    user.Name, user.Email, projectUser.RoleInProject);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error adding users to Sample Project: {Message}", ex.Message);
            throw;
        }
    }
}

