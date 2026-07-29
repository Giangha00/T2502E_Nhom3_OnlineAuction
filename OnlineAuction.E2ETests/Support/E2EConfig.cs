using System.Text.Json;

namespace OnlineAuction.E2ETests.Support;

public sealed record E2EConfig
{
    public string BaseUrl { get; init; } = "http://localhost:5006";
    public string UserEmail { get; init; } = "user1@auctionhouse.local";
    public string UserPassword { get; init; } = "User@123";
    public string AdminEmail { get; init; } = "admin@auctionhouse.com";
    public string AdminPassword { get; init; } = "User@123";
    public string InactiveUserEmail { get; init; } = "user4@auctionhouse.local";
    public string InactiveUserPassword { get; init; } = "User@123";

    public static E2EConfig Load()
    {
        var fromEnv = Environment.GetEnvironmentVariable("E2E_BASE_URL");
        var path = Path.Combine(AppContext.BaseDirectory, "e2e.settings.json");
        var config = File.Exists(path)
            ? JsonSerializer.Deserialize<E2EConfig>(File.ReadAllText(path)) ?? new E2EConfig()
            : new E2EConfig();

        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            config = config with { BaseUrl = fromEnv.TrimEnd('/') };
        }

        return config with { BaseUrl = config.BaseUrl.TrimEnd('/') };
    }
}
