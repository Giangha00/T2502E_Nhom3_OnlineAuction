namespace OnlineAuction.Helpers;

public static class GradeLabelHelper
{
    public const string Ungraded = "Ungraded";

    public static readonly IReadOnlyList<string> Authenticators =
    [
        "PSA",
        "BGS",
        "CGC",
        Ungraded
    ];

    public static readonly IReadOnlyList<string> GradeValues =
    [
        "8",
        "8.5",
        "9",
        "9.5",
        "10"
    ];

    public static string Compose(string? authenticator, string? gradeValue)
    {
        if (string.IsNullOrWhiteSpace(authenticator)
            || string.Equals(authenticator, Ungraded, StringComparison.OrdinalIgnoreCase))
        {
            return Ungraded;
        }

        if (string.IsNullOrWhiteSpace(gradeValue))
        {
            return authenticator.Trim();
        }

        return $"{authenticator.Trim()} {gradeValue.Trim()}";
    }

    public static string ResolveCondition(string? authenticator) =>
        string.Equals(authenticator, Ungraded, StringComparison.OrdinalIgnoreCase)
            ? "Ungraded"
            : "Graded";

    public static IReadOnlyList<string> GetAllGradeLabels()
    {
        var labels = new List<string> { Ungraded };

        foreach (var authenticator in Authenticators.Where(item => item != Ungraded))
        {
            foreach (var gradeValue in GradeValues)
            {
                labels.Add(Compose(authenticator, gradeValue));
            }
        }

        return labels;
    }

    public static void Parse(string? gradeLabel, out string authenticator, out string gradeValue)
    {
        authenticator = "PSA";
        gradeValue = "10";

        if (string.IsNullOrWhiteSpace(gradeLabel))
        {
            return;
        }

        if (string.Equals(gradeLabel, Ungraded, StringComparison.OrdinalIgnoreCase))
        {
            authenticator = Ungraded;
            gradeValue = string.Empty;
            return;
        }

        foreach (var option in Authenticators.Where(item => item != Ungraded))
        {
            if (!gradeLabel.StartsWith(option, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            authenticator = option;
            gradeValue = gradeLabel[option.Length..].Trim();
            if (string.IsNullOrWhiteSpace(gradeValue))
            {
                gradeValue = "10";
            }

            return;
        }

        authenticator = Ungraded;
        gradeValue = string.Empty;
    }
}
