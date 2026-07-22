using System.Diagnostics;
using System.Reflection;

namespace OnlineAuction.E2ETests.Tests;

public sealed class SpecCoverageTests
{
    [Fact]
    public void All147SpecIdsHaveE2ETests()
    {
        var specIds = Assembly.GetExecutingAssembly()
            .GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .SelectMany(m => m.GetCustomAttributesData())
            .Where(a => a.AttributeType.Name == "TraitAttribute"
                        && a.ConstructorArguments.Count >= 2
                        && a.ConstructorArguments[0].Value?.ToString() == "SpecId")
            .Select(a => a.ConstructorArguments[1].Value?.ToString() ?? string.Empty)
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(147, specIds.Count);
    }
}
