using Xunit;

namespace OnlineAuction.E2ETests;

public sealed class E2EFactAttribute : FactAttribute
{
    public E2EFactAttribute()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("E2E_RUN"), "true", StringComparison.OrdinalIgnoreCase))
        {
            Skip = "Set E2E_RUN=true and start OnlineAuction before running Selenium E2E tests.";
        }
    }
}
