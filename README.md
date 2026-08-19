# Reporting & Query Optimisation Module

A hands-on project demonstrating how to identify and fix EF Core performance problems using
real query logging — not guesswork. Built on ASP.NET Core 8 + EF Core + SQL Server, seeded with
2,000 customers, 300 products, 12,000 orders, and ~30,000 order items.

## Problem: The N+1 Query Problem

A single API endpoint that looks completely reasonable — "give me the 20 most recent orders with
their customer names" — silently generated **21 separate database round-trips** instead of 1.

### The bad implementation

```csharp
[HttpGet("recent-bad")]
public async Task<IActionResult> GetRecentOrdersBad()
{
    var orders = await _db.Orders
        .OrderByDescending(o => o.OrderDate)
        .Take(20)
        .ToListAsync();

    var result = new List<object>();

    foreach (var order in orders)
    {
        // This line triggers a NEW database query on every loop iteration
        var customer = await _db.Customers.FindAsync(order.CustomerId);

        result.Add(new
        {
            order.Id,
            order.OrderDate,
            order.Status,
            CustomerName = customer?.FullName
        });
    }

    return Ok(result);
}
```

### What EF Core's query logging revealed

With `.LogTo(Console.WriteLine, LogLevel.Information)` enabled in `Program.cs`, the console showed:

1. **1 query** to fetch the 20 orders:
   ```sql
   SELECT TOP(@__p_0) [o].[Id], [o].[CustomerId], [o].[OrderDate], [o].[Status]
   FROM [Orders] AS [o]
   ORDER BY [o].[OrderDate] DESC
   ```

2. **20 additional queries**, one per order, each fetching a single customer:
   ```sql
   SELECT TOP(1) [c].[Id], [c].[City], [c].[CreatedAt], [c].[Email], [c].[FullName]
   FROM [Customers] AS [c]
   WHERE [c].[Id] = @__p_0
   ```

**Total: 21 database round-trips for one HTTP request.** At production scale (thousands of
orders on a dashboard, for example), this pattern turns a single-digit-millisecond page load
into a multi-second one — and it's invisible unless you're actually watching the generated SQL.

## Fix: Eager Loading + Projection

```csharp
[HttpGet("recent-good")]
public async Task<IActionResult> GetRecentOrdersGood()
{
    var result = await _db.Orders
        .AsNoTracking()
        .Include(o => o.Customer)
        .OrderByDescending(o => o.OrderDate)
        .Take(20)
        .Select(o => new
        {
            o.Id,
            o.OrderDate,
            o.Status,
            CustomerName = o.Customer.FullName
        })
        .ToListAsync();

    return Ok(result);
}
```

Three changes, each solving a different piece of the problem:

| Technique | What it does |
|---|---|
| `.Include(o => o.Customer)` | Eagerly loads the related `Customer` in the *same* query via a SQL `JOIN`, instead of lazily fetching it later |
| `.Select(o => new {...})` | Projects only the four fields the response actually needs, rather than materialising full `Order` and `Customer` entities |
| `.AsNoTracking()` | Skips EF Core's change-tracking overhead, appropriate here since the data is read-only |

### Resulting SQL — one query, one round-trip

```sql
SELECT [o0].[Id], [o0].[OrderDate], [o0].[Status], [c].[FullName] AS [CustomerName]
FROM (
    SELECT TOP(@__p_0) [o].[Id], [o].[CustomerId], [o].[OrderDate], [o].[Status]
    FROM [Orders] AS [o]
    ORDER BY [o].[OrderDate] DESC
) AS [o0]
INNER JOIN [Customers] AS [c] ON [o0].[CustomerId] = [c].[Id]
ORDER BY [o0].[OrderDate] DESC
```

## Measured Results

| Metric | `recent-bad` | `recent-good` |
|---|---|---|
| Database queries executed | **21** | **1** |
| Query reduction | — | **~95%** |
| Response payload | Identical (20 orders, order id/date/status + customer name) | Identical |

Both endpoints return byte-for-byte equivalent JSON. The only difference is what happens between
the API and the database — proving the fix is a pure performance improvement with zero behaviour
change, which is exactly the kind of change that's safe to ship.

## How to Reproduce

1. Run the API in Development mode (`dotnet run`) — sensitive data logging and SQL logging are
   enabled via `Program.cs`.
2. Hit `GET /api/orders/recent-bad` and watch the console: 21 `Executed DbCommand` blocks appear.
3. Hit `GET /api/orders/recent-good` and watch the console: exactly 1 `Executed DbCommand` block
   appears, containing the `INNER JOIN`.

## Tech Stack

- ASP.NET Core 8 (Web API, Controllers)
- Entity Framework Core 9 (Code-First, Migrations)
- SQL Server
- Bogus (realistic seed data generation)
