namespace OnlineAuction.E2ETests;

public sealed class E2ETestSettings
{
    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/') ?? "http://localhost:5006";

    public string UserEmail { get; } =
        Environment.GetEnvironmentVariable("E2E_USER_EMAIL") ?? "user1@auctionhouse.local";

    public string UserPassword { get; } =
        Environment.GetEnvironmentVariable("E2E_USER_PASSWORD") ?? "User@123";

    public string SignupPassword { get; } =
        Environment.GetEnvironmentVariable("E2E_SIGNUP_PASSWORD") ?? "User@123";

    public bool Headless { get; } =
        string.Equals(Environment.GetEnvironmentVariable("E2E_HEADLESS"), "true", StringComparison.OrdinalIgnoreCase);
}
