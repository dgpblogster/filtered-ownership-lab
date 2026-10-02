using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;

namespace FilteredOwnership.Setup;

/// <summary>
/// Creates record filters and associates them with the table.
///
/// Both use CreateRequest rather than the more convenient service.Create(entity),
/// and that is deliberate: CreateRequest is what lets you set the SolutionUniqueName
/// optional parameter, so the components land in your solution.
/// </summary>
internal static class FilterBuilder
{
    /// <summary>
    /// Creates one record filter selecting a single city. The FetchXML root entity name
    /// must match the table this filter will be associated with.
    /// </summary>
    public static Guid CreateCityFilter(IOrganizationService service, LabOptions o, string city)
    {
        var fetchXml =
            $"""
            <fetch>
              <entity name="{o.Prefix}_store">
                <filter type="and">
                  <condition attribute="{o.Prefix}_city" operator="eq" value="{city}" />
                </filter>
              </entity>
            </fetch>
            """;

        var recordFilter = new Entity("recordfilter")
        {
            ["displayname"] = $"Stores in {city}",
            ["uniquename"] = $"{o.Prefix}_storesin{city.ToLowerInvariant()}",
            ["fetchxml"] = fetchXml
        };

        var request = new CreateRequest { Target = recordFilter };
        request["SolutionUniqueName"] = o.SolutionUniqueName;

        var id = ((CreateResponse)service.Execute(request)).id;
        Console.WriteLine($"Created record filter for {city}: {id}");
        return id;
    }

    /// <summary>
    /// Associates a filter with the table. THIS is the row that activates the predicate.
    /// A table can carry several, and access is the UNION of everything granted.
    /// </summary>
    public static Guid Associate(IOrganizationService service, LabOptions o, Guid recordFilterId, string label)
    {
        var entityRecordFilter = new Entity("entityrecordfilter")
        {
            ["name"] = label,
            // Logical name as a string, not the display name and not the entity set name.
            ["objecttypecode"] = $"{o.Prefix}_store",
            ["recordfilterid"] = new EntityReference("recordfilter", recordFilterId)
        };

        var request = new CreateRequest { Target = entityRecordFilter };
        request["SolutionUniqueName"] = o.SolutionUniqueName;

        var id = ((CreateResponse)service.Execute(request)).id;
        Console.WriteLine($"Associated '{label}' with {o.Prefix}_store: {id}");
        return id;
    }

    /// <summary>
    /// Reads back what is actually in force. The equivalent of http/04-inspect-filters.http,
    /// and the first thing to run when somebody reports seeing the wrong rows.
    /// </summary>
    public static void Inspect(IOrganizationService service, LabOptions o)
    {
        var query = new QueryExpression("entityrecordfilter")
        {
            ColumnSet = new ColumnSet("name", "objecttypecode", "statecode"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("objecttypecode", ConditionOperator.Equal, $"{o.Prefix}_store"),
                    // Active associations only. An inactive one grants nothing.
                    new ConditionExpression("statecode", ConditionOperator.Equal, 0)
                }
            },
            LinkEntities =
            {
                new LinkEntity("entityrecordfilter", "recordfilter", "recordfilterid", "recordfilterid", JoinOperator.Inner)
                {
                    EntityAlias = "f",
                    Columns = new ColumnSet("displayname", "uniquename", "fetchxml")
                }
            }
        };

        var results = service.RetrieveMultiple(query);
        Console.WriteLine($"{results.Entities.Count} active filter(s) on {o.Prefix}_store:");

        foreach (var row in results.Entities)
        {
            var name = row.GetAttributeValue<string>("name");
            var unique = row.GetAliasedValue<string>("f.uniquename");
            var fetch = row.GetAliasedValue<string>("f.fetchxml");
            Console.WriteLine($"  {name}  [{unique}]");
            Console.WriteLine($"    {fetch?.Replace("\n", " ").Replace("\r", "")}");
        }
    }

    /// <summary>
    /// Deleting a record filter also deletes its associated entity record filters.
    /// Review every role that grants a filter BEFORE removing it.
    /// </summary>
    public static void DeleteFilter(IOrganizationService service, Guid recordFilterId)
    {
        service.Delete("recordfilter", recordFilterId);
        Console.WriteLine($"Deleted record filter {recordFilterId} (and its associations).");
    }

    /// <summary>Looks up a filter by its unique name. Returns null when it is not there.</summary>
    public static Guid? FindFilterByUniqueName(IOrganizationService service, string uniqueName)
    {
        var query = new QueryExpression("recordfilter")
        {
            ColumnSet = new ColumnSet("recordfilterid"),
            Criteria = new FilterExpression
            {
                Conditions = { new ConditionExpression("uniquename", ConditionOperator.Equal, uniqueName) }
            },
            TopCount = 2
        };

        var found = service.RetrieveMultiple(query).Entities;

        // Refuse to guess. Deleting the wrong predicate is a security change.
        if (found.Count > 1)
            throw new InvalidOperationException(
                $"Expected at most one filter named '{uniqueName}', found {found.Count}.");

        return found.Count == 1 ? found[0].Id : null;
    }

    private static T? GetAliasedValue<T>(this Entity e, string key)
        => e.Contains(key) && e[key] is AliasedValue a && a.Value is T t ? t : default;
}
