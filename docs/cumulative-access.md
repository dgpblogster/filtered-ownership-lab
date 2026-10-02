# The union rule

Access under filtered record ownership is **cumulative**. The documentation states it
precisely: access is the union of the filters granted through all security roles assigned
to the user or their teams.

Nothing subtracts. There is no deny, no override, no "most restrictive wins". If a user
can see more rows than you expected, the answer is almost always a second role you forgot
about.

## The two-role walkthrough

This is the behaviour most likely to confuse somebody later, so it is worth proving to
yourself once.

1. Create two record filters, `Stores in Redmond` and `Stores in Kirkland`, and activate
   both against the Store table.
2. Create two security roles. Grant **Read** on the Redmond filter in the first, and
   **Read** on the Kirkland filter in the second.
3. Assign only the first role to a test user. Sign in as that user and open the Store
   table: four Redmond rows.
4. Now assign the second role to the same user and refresh.

The user now sees eight rows, Redmond and Kirkland together. Nobody edited a filter and
nobody edited the first role.

## Why this matters when you are debugging

A support ticket that says "this person can see data they should not" is, under this
model, a question about role assignment rather than a question about the filter. Start by
listing every role the user holds, directly and through every team, and every filter each
of those roles grants. The filter that is letting the rows through may be attached to a
role nobody remembers assigning.

`http/04-inspect-filters.http` tells you which predicates are active on the table. It does
not tell you who has been granted them, which is the other half of the picture.

## Administrators see everything

The system administrator role carries a system-provided **All records** filter. An
administrator therefore experiences no restriction at all, which means:

- You cannot test this feature with your own account.
- A demo given as an administrator demonstrates nothing.

Use a real second account with no administrator role. The documentation says this twice.
