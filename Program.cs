using Microsoft.EntityFrameworkCore;
using BetterX_API.Data;
using BetterX_API.Models;
using BetterX_API.Services;

// Records declared before the main code
namespace BetterX_API;

public record UserCreate(string Username, string Token);
public record AppUpdate(string Version, bool UsedMoreThanHour);
public record LocationUpdate(string Country);
public record StatusUpdate(UserStatus Status);

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Configure Kestrel to listen on all interfaces
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(5000);
        });

        // Configure SQLite with absolute path to ensure location
        var folder = Path.Combine(Environment.CurrentDirectory, "Data");
        Directory.CreateDirectory(folder);
        var dbPath = Path.Combine(folder, "betterx.db");
        
        builder.Services.AddDbContext<ApiDbContext>(opt => 
            opt.UseSqlite($"Data Source={dbPath}"));

        // Add heartbeat service
        builder.Services.AddHostedService<HeartbeatService>();

        // Add TwitterAuthService to DI container
        builder.Services.AddScoped<TwitterAuthService>();

        var app = builder.Build();

        // Add support for static files
        app.UseStaticFiles();

        // Create database on startup if it doesn't exist
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApiDbContext>();
            db.Database.EnsureCreated();
        }

        // Check if user exists
        app.MapGet("/users/{username}", async (string username, ApiDbContext db) =>
            await db.Users.AnyAsync(u => u.Username == username)
                ? Results.Ok(new { exists = true })
                : Results.Ok(new { exists = false }));

        // Add a new user
        app.MapPost("/users", async (UserCreate user, ApiDbContext db) =>
        {
            if (await db.Users.AnyAsync(u => u.Username == user.Username))
                return Results.BadRequest("User already exists");

            var newUser = new User { Username = user.Username, Token = user.Token };
            db.Users.Add(newUser);
            await db.SaveChangesAsync();
            return Results.Created($"/users/{user.Username}", newUser);
        });

        // Get user status
        app.MapGet("/users/{username}/status", async (string username, ApiDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            return user == null ? Results.NotFound() : Results.Ok(new { status = user.Status });
        });

        // Update user status
        app.MapPut("/users/{username}/status", async (string username, string token, StatusUpdate update, ApiDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Results.NotFound();
            if (user.Token != token) return Results.Unauthorized();
            
            user.Status = update.Status;
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        // Update app version and usage
        app.MapPut("/users/{username}/app-update", async (string username, string token, AppUpdate update, ApiDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Results.NotFound();
            if (user.Token != token) return Results.Unauthorized();
            
            user.AppVersion = update.Version;
            user.UsedMoreThanHour = update.UsedMoreThanHour;
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        // Save installation country
        app.MapPut("/users/{username}/location", async (string username,  string token, LocationUpdate location, ApiDbContext db) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return Results.NotFound();
            if (user.Token != token) return Results.Unauthorized();

            user.Country = location.Country;
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        // Add heartbeat endpoint
        app.MapPost("/users/{username}/heartbeat", async (string username, string token, ApiDbContext db) =>
        {
            var user = await db.Users
                .AsNoTracking()  // Optimisation pour la lecture seule
                .FirstOrDefaultAsync(u => u.Username == username);
                
            if (user == null) return Results.NotFound();
            if (user.Token != token) return Results.Unauthorized();

            // Mise à jour avec suivi explicite
            var userToUpdate = await db.Users.FindAsync(user.Id);
            if (userToUpdate != null)
            {
                userToUpdate.LastHeartbeat = DateTime.UtcNow;
                await db.SaveChangesAsync();
            }

            return Results.Ok();
        });

        // Add callback endpoint
        app.MapGet("/callback", (string oauth_token, string oauth_verifier) =>
        {
            return Results.Content(
                File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "wwwroot", "callback.html"))
                    .Replace("{{VERIFIER}}", oauth_verifier),
                "text/html");
        });

        // Get connect request URL
        app.MapGet("/connect-request", async (TwitterAuthService twitter) =>
        {
            var result = await twitter.GetRequestTokenAsync();
            return result == null 
                ? Results.BadRequest("Failed to get authorization URL") 
                : Results.Ok(new { auth_url = result.Value.AuthUrl, token = result.Value.Token });
        });

        // Get access token
        app.MapGet("/get-token", async (string oauth_token, string oauth_verifier, TwitterAuthService twitter) =>
        {
            var accessToken = await twitter.GetAccessTokenAsync(oauth_token, oauth_verifier);
            return accessToken == null 
                ? Results.BadRequest("Failed to get access token") 
                : Results.Ok(accessToken);
        });

        app.Run();
    }
}
