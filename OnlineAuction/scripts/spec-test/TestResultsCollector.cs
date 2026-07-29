using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;

record TestRunSummary(
    int Total,
    int Passed,
    int Failed,
    int Skipped,
    bool Success,
    DateTime RunAtUtc,
    IReadOnlyDictionary<string, ClassTestResult> Classes);

record ClassTestResult(string ClassName, int Passed, int Failed, int Skipped, bool Success);

static class TestResultsCollector
{
    public static TestRunSummary Collect(string testProjectPath)
    {
        var trxPath = Path.Combine(Path.GetTempPath(), $"OnlineAuction_spec_test_{Guid.NewGuid():N}.trx");
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"test \"{testProjectPath}\" --logger \"trx;LogFileName={trxPath}\" --verbosity quiet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start dotnet test.");
        process.WaitForExit();

        if (!File.Exists(trxPath))
        {
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException(
                $"dotnet test did not produce TRX.\nExit: {process.ExitCode}\n{output}\n{error}");
        }

        return ParseTrx(trxPath, process.ExitCode == 0);
    }

    static TestRunSummary ParseTrx(string trxPath, bool exitSuccess)
    {
        var doc = XDocument.Load(trxPath);
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        var counters = doc.Descendants(ns + "Counters").FirstOrDefault();
        var total = int.Parse(counters?.Attribute("total")?.Value ?? "0");
        var passed = int.Parse(counters?.Attribute("passed")?.Value ?? "0");
        var failed = int.Parse(counters?.Attribute("failed")?.Value ?? "0");
        var skipped = int.Parse(counters?.Attribute("notExecuted")?.Value ?? "0");

        var classMap = new Dictionary<string, ClassTestResult>(StringComparer.Ordinal);

        var testIdToClass = doc.Descendants(ns + "UnitTest")
            .Select(ut => new
            {
                Id = ut.Attribute("id")?.Value ?? ut.Attribute("testId")?.Value,
                Class = ut.Descendants(ns + "TestMethod").FirstOrDefault()?.Attribute("className")?.Value
            })
            .Where(x => !string.IsNullOrEmpty(x.Id) && !string.IsNullOrEmpty(x.Class))
            .ToDictionary(x => x.Id!, x => x.Class!, StringComparer.Ordinal);

        foreach (var unit in doc.Descendants(ns + "UnitTestResult"))
        {
            var outcome = unit.Attribute("outcome")?.Value ?? "Failed";
            var testId = unit.Attribute("testId")?.Value ?? string.Empty;
            var className = testIdToClass.TryGetValue(testId, out var cls)
                ? cls
                : unit.Attribute("testName")?.Value ?? string.Empty;
            var shortClass = ExtractClassName(className);
            if (string.IsNullOrEmpty(shortClass))
                continue;

            if (!classMap.TryGetValue(shortClass, out var current))
            {
                current = new ClassTestResult(shortClass, 0, 0, 0, true);
            }

            var p = current.Passed;
            var f = current.Failed;
            var s = current.Skipped;
            switch (outcome)
            {
                case "Passed":
                    p++;
                    break;
                case "NotExecuted":
                case "Skipped":
                    s++;
                    break;
                default:
                    f++;
                    break;
            }

            classMap[shortClass] = new ClassTestResult(shortClass, p, f, s, f == 0);
        }

        var success = exitSuccess && failed == 0;
        return new TestRunSummary(total, passed, failed, skipped, success, DateTime.UtcNow, classMap);
    }

    static string ExtractClassName(string className)
    {
        if (string.IsNullOrWhiteSpace(className))
            return string.Empty;

        var shortName = className.Split('.').LastOrDefault() ?? className;
        if (shortName.EndsWith("Tests", StringComparison.Ordinal))
            return shortName;

        var match = Regex.Match(className, @"\.(\w+Tests)\.");
        return match.Success ? match.Groups[1].Value : shortName;
    }

    public static bool TryGetClassResult(TestRunSummary summary, string className, out ClassTestResult result)
        => summary.Classes.TryGetValue(className, out result!);

    public static int CountFiltered(string testProjectPath, string filter)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"test \"{testProjectPath}\" --filter \"{filter}\" --verbosity quiet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start dotnet test.");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        var match = Regex.Match(output, @"Total:\s*(\d+).*Passed:\s*(\d+).*Failed:\s*(\d+)", RegexOptions.Singleline);
        if (!match.Success)
            return -1;

        var total = int.Parse(match.Groups[1].Value);
        var passed = int.Parse(match.Groups[2].Value);
        var failed = int.Parse(match.Groups[3].Value);
        return failed == 0 && passed == total ? passed : -1;
    }
}

static class AutomatedCoverageMap
{
    public static readonly (string Prefix, string[] Classes)[] Rules =
    [
        ("ADM-SYNC", ["AdminAuctionFormSyncTests"]),
        ("CAT", ["AuctionVisibilityTests", "ConfirmingStatusTests"]),
        ("BID-04", ["BidIncrementValidationTests"]),
        ("BID-02", ["BidServicePlaceBidTests"]),
        ("BID-03", ["BidServicePlaceBidTests"]),
        ("BID-05", ["BidServicePlaceBidTests"]),
        ("BID-06", ["BidServicePlaceBidTests"]),
        ("BID-07", ["BidServicePlaceBidTests"]),
        ("BID-08", ["BidServicePlaceBidTests"]),
        ("BID-09", ["BidServicePlaceBidTests"]),
        ("BID-10", ["BidServicePlaceBidTests"]),
        ("BID-11", ["ProductDetailCanBidTests"]),
        ("BID-01", []),
        ("FRAUD", ["BidRateLimitServiceTests"]),
        ("CONF", ["ConfirmingStatusTests"]),
        ("DOC", ["ProductDocumentDownloadTests", "ConfirmingStatusTests"]),
        ("DASH-05", ["DashboardFilterValidatorTests"]),
        ("FEE-04", ["ListingFeeCalculatorTests"]),
        ("FEE-01", ["MarketplaceFeeCalculatorTests"]),
        ("FEE-02", ["MarketplaceFeeCalculatorTests"]),
        ("FEE-03", ["MarketplaceFeeCalculatorTests", "OrderPayPathFeeTests"]),
        ("ORD-02", ["OrderCheckoutSelectionTests"]),
        ("ORD-03", ["OrderCheckoutSelectionTests"]),
        ("ORD-05", ["OrderPayPathFeeTests"]),
        ("PAY", ["PayPalCaptureFlowTests"]),
        ("AUCTION_REG-04", ["PayPalCaptureFlowTests"]),
        ("WNP", ["WinnerNonPaymentBidSelectorTests", "WinnerNonPaymentRecoveryIntegrationTests"]),
        ("AUTO", []),
    ];

    public static string[]? GetClassesForType(string type)
    {
        foreach (var (prefix, classes) in Rules)
        {
            if (type.StartsWith(prefix, StringComparison.Ordinal))
                return classes.Length > 0 ? classes : null;
        }

        return null;
    }

    public static bool AllClassesPassed(TestRunSummary summary, string[] classes)
    {
        foreach (var cls in classes)
        {
            if (!TestResultsCollector.TryGetClassResult(summary, cls, out var result) || !result.Success)
                return false;
        }

        return true;
    }
}
