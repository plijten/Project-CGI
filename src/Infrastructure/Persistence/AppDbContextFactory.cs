using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }

    private static string ResolveConnectionString()
    {
        var environmentConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(environmentConnectionString))
        {
            return environmentConnectionString;
        }

        var appSettingsPath = ResolveAppSettingsPath();
        if (!File.Exists(appSettingsPath))
        {
            throw new InvalidOperationException($"Could not find appsettings.json at '{appSettingsPath}'.");
        }

        using var document = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
        if (document.RootElement.TryGetProperty("ConnectionStrings", out var connectionStrings)
            && connectionStrings.TryGetProperty("DefaultConnection", out var defaultConnection)
            && !string.IsNullOrWhiteSpace(defaultConnection.GetString()))
        {
            return defaultConnection.GetString()!;
        }

        throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
    }

    private static string ResolveAppSettingsPath()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var candidatePaths = new[]
        {
            Path.Combine(currentDirectory, "appsettings.json"),
            Path.GetFullPath(Path.Combine(currentDirectory, "..", "Web", "appsettings.json")),
            Path.GetFullPath(Path.Combine(currentDirectory, "src", "Web", "appsettings.json")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Web", "appsettings.json"))
        };

        return candidatePaths.FirstOrDefault(File.Exists) ?? candidatePaths[1];
    }
}
