# Filtered record ownership lab

Sample code for **filtered record ownership** in Microsoft Dataverse: a table whose rows
have no owner, where access is decided by a FetchXML predicate evaluated against the row's
own column values.

Companion to [Rows Without an Owner](https://www.theworkbench.blog/2026/10/power-apps-rows-without-owner.html)
on [The Workbench](https://www.theworkbench.blog).

> **Preview feature.** Filtered record ownership is in preview. Property names and
> behaviour can change before general availability. Point this at a **developer
> environment**, never production. The ownership type cannot be changed after a table is
> created, so there is no undo on the table itself.

> **The C# project in `src/` has not been compiled.** It is written against the shapes in
> the Microsoft documentation, and it is unverified. The `http/` files are the ones to
> trust first, because there is nothing between the request and the service. If you build
> the C# and hit a problem, the blog post comments are the place to say so.

## What is here

| Path | What it is |
|---|---|
| [`http/`](http/) | The raw Web API calls, one file per step. Start here. |
| [`src/FilteredOwnership.Setup/`](src/FilteredOwnership.Setup/) | A console app doing the same work through the SDK for .NET. |
| [`data/stores.csv`](data/stores.csv) | Twelve sample rows across Redmond, Kirkland and Sammamish. |
| [`docs/cumulative-access.md`](docs/cumulative-access.md) | The union rule, with the two-role walkthrough. |
| `solution/src/` | Empty on purpose. Export and unpack your own solution here. |

`solution/src/` ships empty deliberately. A pre-built solution containing a preview
ownership type would be a liability rather than a convenience, and you would have to
rebuild it against your own publisher prefix anyway. Create the components with the code
here, then export and unpack your solution into that folder so the artifacts land in
source control.

## The three objects

1. **The table**, created with `OwnershipType` set to `Filtered` (value `32`). Its rows
   have no owner. They cannot be assigned and they cannot be shared, and the ownership
   type is immutable after creation.
2. **The record filter**, a row in the `recordfilter` table holding a FetchXML query. Its
   root `<entity>` name must match the table it gets associated with.
3. **The entity record filter**, a row in `entityrecordfilter` associating a filter with a
   table. This is the row that *activates* the predicate. A table can carry several.

A security role then grants a filter for one or more of Create, Read, Write, Delete,
Append and Append to, and you assign the role to users or teams.

## Running the HTTP files

Set the five variables documented in [`http/_env.http`](http/_env.http), then run the
files in order. A token for your environment:

```bash
az account get-access-token --resource https://yourorg.crm.dynamics.com --query accessToken -o tsv
```

`04-inspect-filters.http` is the one you will keep. It answers "what predicates are live
on this table right now", pulling each filter's FetchXML into the same response. It is the
first thing to run when somebody reports seeing the wrong rows.

## Running the console app

Requires the .NET 8 SDK.

```bash
export DATAVERSE_CONNECTION_STRING="AuthType=OAuth;Url=https://yourorg.crm.dynamics.com;Username=you@yourorg.com;LoginPrompt=Auto"
export LAB_SOLUTION=FilteredOwnershipLab
export LAB_PREFIX=new
```

```bash
dotnet run --project src/FilteredOwnership.Setup -- create-table
```

Verbs: `create-table`, `create-filters`, `inspect`, `teardown`.

`teardown` deletes this lab's record filters, which also deletes their associations. It
leaves the table in place, because a filtered table cannot be converted to another
ownership type and deleting it should be a deliberate act in the maker portal rather than
a side effect of a script.

## Two things that will waste your afternoon

**You cannot test this as yourself.** The system administrator role carries a
system-provided **All records** filter, so an administrator sees every row and experiences
no restriction. Use a real second account with no administrator role. A demo given as an
administrator demonstrates nothing.

**Access is the union, never the intersection.** Grant one filter through one role and
another through a second role, and the user sees both sets. Nothing subtracts. See
[`docs/cumulative-access.md`](docs/cumulative-access.md).

## Contributing

This repository is a publishing surface for the blog rather than a community project, so it
does not accept pull requests. If something here does not work in your environment, the
comments on the blog post are the right place, and that is where the next version of this
code will come from.

## License

MIT. See [LICENSE](LICENSE).
