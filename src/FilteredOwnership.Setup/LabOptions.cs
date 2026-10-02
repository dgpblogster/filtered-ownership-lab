namespace FilteredOwnership.Setup;

/// <summary>Everything the verbs need, read from the environment or the command line.</summary>
internal sealed record LabOptions(string ConnectionString, string SolutionUniqueName, string Prefix)
{
    public static LabOptions FromEnvironment()
    {
        var cs = Environment.GetEnvironmentVariable("DATAVERSE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "Set DATAVERSE_CONNECTION_STRING. See the README for the two supported forms.");

        var solution = Environment.GetEnvironmentVariable("LAB_SOLUTION") ?? "FilteredOwnershipLab";
        var prefix = Environment.GetEnvironmentVariable("LAB_PREFIX") ?? "new";

        return new LabOptions(cs, solution, prefix);
    }
}
