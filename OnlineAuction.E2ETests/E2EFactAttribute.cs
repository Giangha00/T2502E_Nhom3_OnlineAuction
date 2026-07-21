using Xunit;

namespace OnlineAuction.E2ETests;

// Attribute rieng cho E2E test.
// Muc dich: tranh de Selenium test chay mac dinh khi app chua duoc start.
public sealed class E2EFactAttribute : FactAttribute
{
    public E2EFactAttribute()
    {
        // Chi chay Selenium test khi terminal da set E2E_RUN=true.
        // Neu khong, xUnit se danh dau test la skipped thay vi fail.
        if (!string.Equals(Environment.GetEnvironmentVariable("E2E_RUN"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set E2E_RUN=true and start OnlineAuction before running Selenium E2E tests.";
        }
    }
}
