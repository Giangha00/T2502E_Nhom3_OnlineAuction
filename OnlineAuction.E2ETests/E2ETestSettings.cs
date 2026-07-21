namespace OnlineAuction.E2ETests;

// Noi tap trung cau hinh cho Selenium E2E tests.
// Co the doi gia tri bang environment variables ma khong can sua code test.
public sealed class E2ETestSettings
{
    // URL cua app OnlineAuction dang chay. Mac dinh lay profile http: localhost:5006.
    public string BaseUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_BASE_URL")?.TrimEnd('/') ?? "http://localhost:5006";

    // Tai khoan seed co san de test luong login.
    public string UserEmail { get; } =
        Environment.GetEnvironmentVariable("E2E_USER_EMAIL") ?? "user1@auctionhouse.local";

    // Password cua tai khoan seed.
    public string UserPassword { get; } =
        Environment.GetEnvironmentVariable("E2E_USER_PASSWORD") ?? "User@123";

    // Password dung khi test tao tai khoan moi.
    public string SignupPassword { get; } =
        Environment.GetEnvironmentVariable("E2E_SIGNUP_PASSWORD") ?? "User@123";

    // true: Chrome chay an; false: hien cua so Chrome de quan sat luong test.
    public bool Headless { get; } =
        string.Equals(Environment.GetEnvironmentVariable("E2E_HEADLESS"), "true", StringComparison.OrdinalIgnoreCase);
}
