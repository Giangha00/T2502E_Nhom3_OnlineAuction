namespace OnlineAuction.E2ETests.Support;

public static class E2ERuntime
{
    public static void RequireApp(E2EHttpHelper http, E2EConfig config)
    {
        if (!http.IsAppRunning())
        {
            throw new InvalidOperationException(
                $"E2E requires the app at {config.BaseUrl}. Run: dotnet run --project OnlineAuction --launch-profile http");
        }
    }
}
