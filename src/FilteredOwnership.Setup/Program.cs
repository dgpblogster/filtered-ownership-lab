using Microsoft.PowerPlatform.Dataverse.Client;

namespace FilteredOwnership.Setup;

/// <summary>
/// Filtered record ownership lab.
///
/// PREVIEW: filtered record ownership is a preview feature. Point this at a developer
/// environment. Never production.
///
/// Verbs:
///   create-table    create the Store table (OwnershipType=Filtered) and its City column
///   create-filters  create one record filter per city and activate each one
///   inspect         list the filters currently in force on the table
///   teardown        delete the filters created by this lab
/// </summary>
internal static class Program
{
    private static readonly string[] Cities = ["Redmond", "Kirkland", "Sammamish"];

    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine(
                "Usage: dotnet run -- <create-table|create-filters|inspect|teardown>");
            return 1;
        }

        try
        {
            var options = LabOptions.FromEnvironment();
            using var client = new ServiceClient(options.ConnectionString);

            if (!client.IsReady)
                throw new InvalidOperationException(
                    $"Could not connect: {client.LastError}");

            Console.WriteLine($"Connected to {client.ConnectedOrgFriendlyName}.");

            switch (args[0])
            {
                case "create-table":
                    TableBuilder.Create(client, options);
                    TableBuilder.AddCityColumn(client, options);
                    break;

                case "create-filters":
                    foreach (var city in Cities)
                    {
                        var filterId = FilterBuilder.CreateCityFilter(client, options, city);
                        FilterBuilder.Associate(client, options, filterId, $"Stores in {city}");
                    }
                    Console.WriteLine();
                    Console.WriteLine(
                        "Now grant these filters on security roles. Access is the UNION of every "
                        + "filter granted through every role the user or their teams hold.");
                    break;

                case "inspect":
                    FilterBuilder.Inspect(client, options);
                    break;

                case "teardown":
                    Teardown(client, options);
                    break;

                default:
                    Console.Error.WriteLine($"Unknown verb '{args[0]}'.");
                    return 1;
            }

            return 0;
        }
        catch (Exception ex)
        {
            // Fail loudly. A setup script that swallows an error leaves you with a
            // half-configured security model, which is worse than no security model.
            Console.Error.WriteLine($"FAILED: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Removes this lab's record filters. Deleting a record filter also deletes its
    /// entity record filter associations, so the table is left with no predicates and
    /// nobody but an administrator can see its rows.
    ///
    /// The table itself is left in place: it cannot be converted back to another
    /// ownership type, so deleting it is a decision you should make deliberately in the
    /// maker portal rather than from a script.
    /// </summary>
    private static void Teardown(IOrganizationServiceAsync client, LabOptions options)
    {
        foreach (var city in Cities)
        {
            var unique = $"{options.Prefix}_storesin{city.ToLowerInvariant()}";
            var id = FilterBuilder.FindFilterByUniqueName(client, unique);

            if (id is null)
            {
                Console.WriteLine($"No filter named {unique}; nothing to remove.");
                continue;
            }

            FilterBuilder.DeleteFilter(client, id.Value);
        }
    }
}
