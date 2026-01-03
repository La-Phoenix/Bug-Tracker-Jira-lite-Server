using BugTrackr.Application.Services;
using BugTrackr.Application.Services.Email;
using BugTrackr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BugTrackr.Infrastructure.Services.Email;

//Background Service
public class WeeklyDigestService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WeeklyDigestService> _logger;

    public WeeklyDigestService(IServiceProvider serviceProvider, ILogger<WeeklyDigestService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run every Sunday at 9 AM
                var now = DateTime.UtcNow;
                var nextRun = GetNextSunday9AM(now);
                var delay = nextRun - now;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation("Next weekly digest will run at {NextRun}", nextRun);
                    await Task.Delay(delay, stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await SendWeeklyDigests();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WeeklyDigestService");
                // Wait an hour before retrying
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task SendWeeklyDigests()
    {
        using var scope = _serviceProvider.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
        var issueRepo = scope.ServiceProvider.GetRequiredService<IRepository<Issue>>();
        var projectRepo = scope.ServiceProvider.GetRequiredService<IRepository<Project>>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        try
        {
            var usersWithDigest = await userRepo.Query()
                .Where(u => u.WeeklyDigest)
                .ToListAsync();

            _logger.LogInformation("Sending weekly digest to {UserCount} users", usersWithDigest.Count);

            foreach (var user in usersWithDigest)
            {
                try
                {
                    var assignedIssues = await issueRepo.Query()
                        .Include(i => i.Status)
                        .Include(i => i.Priority)
                        .Where(i => i.AssigneeId == user.Id)
                        .ToListAsync();

                    var userProjects = await projectRepo.Query()
                        .Include(p => p.ProjectUsers)
                        .Where(p => p.ProjectUsers.Any(pu => pu.UserId == user.Id))
                        .ToListAsync();

                    await emailService.SendWeeklyDigestEmailAsync(user, assignedIssues, userProjects);
                    _logger.LogInformation("Weekly digest sent to {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send weekly digest to {Email}", user.Email);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending weekly digests");
        }
    }

    private static DateTime GetNextSunday9AM(DateTime now)
    {
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
        if (daysUntilSunday == 0 && now.Hour >= 9) // If it's Sunday but past 9 AM
            daysUntilSunday = 7;

        var nextSunday = now.Date.AddDays(daysUntilSunday);
        return nextSunday.AddHours(9);
    }
}