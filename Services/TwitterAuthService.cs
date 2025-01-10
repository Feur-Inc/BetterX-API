using BetterX_API.Data;
using BetterX_API.Models;
using Microsoft.EntityFrameworkCore;

namespace BetterX_API.Services;

public class HeartbeatService : BackgroundService
{
    private readonly IServiceProvider _services;
    private const int HEARTBEAT_TIMEOUT_SECONDS = 60;

    public HeartbeatService(IServiceProvider services)
    {
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _services.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
                    var now = DateTime.UtcNow;
                    var timeoutThreshold = now.AddSeconds(-HEARTBEAT_TIMEOUT_SECONDS);

                    var users = await db.Users.ToListAsync(stoppingToken);
                    foreach (var user in users)
                    {
                        var timeSinceLastHeartbeat = now - user.LastHeartbeat;
                        var shouldBeActive = timeSinceLastHeartbeat.TotalSeconds > HEARTBEAT_TIMEOUT_SECONDS;

                        Console.WriteLine($"User {user.Username}: Last heartbeat: {user.LastHeartbeat:yyyy-MM-dd HH:mm:ss}, Time since: {timeSinceLastHeartbeat.TotalSeconds:F1}s, Should be active: {shouldBeActive}");

                        if (shouldBeActive && user.Status != UserStatus.Active)
                        {
                            user.Status = UserStatus.Active;
                            await db.SaveChangesAsync(stoppingToken);
                            Console.WriteLine($"Updated {user.Username} to Active");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in HeartbeatService: {ex.Message}");
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}
