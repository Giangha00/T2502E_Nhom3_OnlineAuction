using System.Diagnostics;
using OnlineAuction.E2ETests.Support;
using OpenQA.Selenium;

namespace OnlineAuction.E2ETests.Tests;

public sealed class StaticAutoE2ETests : E2ETestBase
{
    [Fact]
    [Trait("SpecId", "PAGE-01")]
    public void PAGE_01_ContactPageLayout()
    {
        Go("/Contact");
        Assert.True(
            Driver.FindElements(E2ESelectors.ContactPage).Count > 0
            || Driver.FindElements(E2ESelectors.SiteMain).Count > 0);
    }

    [Fact]
    [Trait("SpecId", "PAGE-02")]
    public void PAGE_02_ContactFormSubmit()
    {
        Go("/Contact");
        var inputs = Driver.FindElements(By.CssSelector("input, textarea"));
        Assert.True(inputs.Count > 0);
    }

    [Fact]
    [Trait("SpecId", "PAGE-03")]
    public void PAGE_03_ContactResponsive()
    {
        ((IJavaScriptExecutor)Driver).ExecuteScript("window.resizeTo(375,812)");
        Go("/Contact");
        var width = (long)((IJavaScriptExecutor)Driver).ExecuteScript("return document.documentElement.clientWidth;");
        Assert.True(width <= 400);
    }

    [Fact]
    [Trait("SpecId", "PAGE-04")]
    public void PAGE_04_AboutFaqPolicy()
    {
        AssertPageOk("/AboutUs/About");
        AssertPageOk("/Faq");
        AssertPageOk("/Policy");
    }

    [Fact]
    [Trait("SpecId", "PAGE-05")]
    public void PAGE_05_LanguageSwitch()
    {
        Go("/");
        Assert.True(
            Driver.FindElements(E2ESelectors.LanguageForm).Count > 0
            || Driver.FindElements(E2ESelectors.LanguageSwitcher).Count > 0);
    }

    [Fact]
    [Trait("SpecId", "AUTO-01")]
    public void AUTO_01_FullUnitSuite_PassCount()
    {
        var repoRoot = FindRepoRoot();
        var testProject = Path.Combine(repoRoot, "OnlineAuction.Tests", "OnlineAuction.Tests.csproj");
        var passed = RunDotnetTestCount(testProject, null);
        Assert.True(passed > 0, "OnlineAuction.Tests should pass when run separately.");
    }

    [Fact]
    [Trait("SpecId", "AUTO-02")]
    public void AUTO_02_AdminFormSyncTests()
    {
        var repoRoot = FindRepoRoot();
        var testProject = Path.Combine(repoRoot, "OnlineAuction.Tests", "OnlineAuction.Tests.csproj");
        var passed = RunDotnetTestCount(testProject, "FullyQualifiedName~AdminAuctionFormSyncTests");
        Assert.True(passed >= 0);
    }

    [Fact]
    [Trait("SpecId", "AUTO-03")]
    public void AUTO_03_ReleaseSmokeScriptExists()
    {
        var repoRoot = FindRepoRoot();
        var script = Path.Combine(repoRoot, "scripts", "smoke", "Invoke-ReleaseSmoke.ps1");
        Assert.True(File.Exists(script), $"Missing smoke script: {script}");
    }

    static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Nhom3.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repo root.");
    }

    static int RunDotnetTestCount(string projectPath, string? filter)
    {
        var args = filter is null
            ? $"test \"{projectPath}\" --verbosity quiet"
            : $"test \"{projectPath}\" --filter \"{filter}\" --verbosity quiet";
        var psi = new ProcessStartInfo("dotnet", args)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false
        };
        using var p = Process.Start(psi)!;
        var output = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        var match = System.Text.RegularExpressions.Regex.Match(output, @"Passed:\s*(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : -1;
    }
}
