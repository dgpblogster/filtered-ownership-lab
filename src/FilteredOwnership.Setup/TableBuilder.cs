using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace FilteredOwnership.Setup;

/// <summary>
/// Creates the filtered-ownership table. The entire feature is one property:
/// OwnershipType = OwnershipTypes.Filtered. Everything else is an ordinary custom table.
/// </summary>
internal static class TableBuilder
{
    public static void Create(IOrganizationService service, LabOptions o)
    {
        var request = new CreateEntityRequest
        {
            // Associates the new component with an unmanaged solution as it is created.
            // Worth setting: every object in this feature is a solution component, and you
            // would rather not go hunting for them later.
            SolutionUniqueName = o.SolutionUniqueName,
            Entity = new EntityMetadata
            {
                SchemaName = $"{o.Prefix}_Store",
                DisplayName = new Label("Store", 1033),
                DisplayCollectionName = new Label("Stores", 1033),
                Description = new Label("Stores organized by city.", 1033),

                // The one line that matters. Immutable after creation: you cannot convert
                // a user-owned or organization-owned table to Filtered afterwards.
                OwnershipType = OwnershipTypes.Filtered,
                IsActivity = false
            },
            PrimaryAttribute = new StringAttributeMetadata
            {
                SchemaName = $"{o.Prefix}_Name",
                DisplayName = new Label("Name", 1033),
                RequiredLevel = new AttributeRequiredLevelManagedProperty(
                    AttributeRequiredLevel.None),
                MaxLength = 100,
                FormatName = StringFormatName.Text
            }
        };

        service.Execute(request);
        Console.WriteLine($"Created table {o.Prefix}_Store with OwnershipType=Filtered.");
    }

    /// <summary>Adds the City column the filters will key on.</summary>
    public static void AddCityColumn(IOrganizationService service, LabOptions o)
    {
        service.Execute(new CreateAttributeRequest
        {
            SolutionUniqueName = o.SolutionUniqueName,
            EntityName = $"{o.Prefix}_store",
            Attribute = new StringAttributeMetadata
            {
                SchemaName = $"{o.Prefix}_City",
                DisplayName = new Label("City", 1033),
                RequiredLevel = new AttributeRequiredLevelManagedProperty(
                    AttributeRequiredLevel.None),
                MaxLength = 100,
                FormatName = StringFormatName.Text
            }
        });

        Console.WriteLine($"Added column {o.Prefix}_city.");
    }
}
