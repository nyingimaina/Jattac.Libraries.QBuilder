# Jattac.Libraries.QBuilder

A **fully-fledged C# dialect of SQL** — a fluent, type-safe query builder for .NET that covers every clause in ANSI SQL plus the SQL Server and MariaDB/MySQL dialects.

[![NuGet](https://img.shields.io/nuget/v/Rocket.Libraries.QBuilder.svg)](https://www.nuget.org/packages/Rocket.Libraries.QBuilder)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## Why QBuilder?

Writing raw SQL strings in application code is fragile — typos are runtime errors, refactors are grep-and-pray, and parameterization is tedious to get right. ORMs solve some of this but are heavy and leak abstractions.

QBuilder sits in the middle: it is **purely a query builder**. It produces SQL strings (optionally parameterized) that you execute yourself with Dapper, ADO.NET, or any micro-ORM. There is no tracking, no migrations, no change detection — just clean, tested SQL.

**Key properties:**
- Zero boilerplate — one fluent chain, no sub-builder ceremonies
- Parameterized by default — injection-safe without extra effort
- Full SQL coverage — SELECT, JOIN (all 5 types), WHERE, GROUP BY, HAVING, ORDER BY, paging (ROW_NUMBER, OFFSET/FETCH, LIMIT), UNION/INTERSECT/EXCEPT, CTEs, CASE WHEN, EXISTS, BETWEEN, IS NULL
- Every public member has XML doc comments — your IDE guides you at every step
- 100% unit-tested — 97 tests, 0 failures

---

## Installation

```bash
dotnet add package Rocket.Libraries.QBuilder
```

---

## Quick start

```csharp
using Rocket.Libraries.Qurious;         // Q, QBuilder
using Rocket.Libraries.Qurious.Enums;  // FilterOperator, AggregateFunction

// 1. Build a parameterized query (safe by default)
var result = Q.Build()                                   // parameterize: true
    .Select<User, Guid>(u => u.Id)
    .Select<User, string>(u => u.Name, alias: "UserName")
    .Where<User, bool>(u => u.Active, FilterOperator.EqualTo, true)
    .OrderBy<User, string>(u => u.Name)
    .BuildWithParameters();

// result.ParameterizedSql — the SQL string with @Name0 placeholders
// result.Parameters       — Dictionary<string, object> { "@Active0": true }

// 2. Execute with Dapper
var users = await connection.QueryAsync<User>(
    result.ParameterizedSql, result.Parameters);
```

---

## Core concepts

### Entry point — `Q.Build()`

The static `Q` class is the single entry point for all queries.

```csharp
// Parameterized (default) — call BuildWithParameters() at the end
var qb = Q.Build();

// Non-parameterized — call Build() at the end
var qb = Q.Build(parameterize: false);

// Custom table-name resolver — map CLR types to SQL table names
var qb = Q.Build(t => "dbo." + t.Name + "s");
// User  → "dbo.Users"
// Order → "dbo.Orders"
```

The custom resolver is useful when table names differ from CLR type names (plural names, schema prefixes, legacy naming conventions).

### Table aliases

Every table gets an alias derived from its CLR type name: `User` → `tUser`, `Order` → `tOrder`.

When a custom resolver maps to schema-qualified names (`dbo.Users`), the schema prefix is stripped automatically — `tUsers` not `tdbo.Users`.

### Parameterized vs literal mode

| Mode | Entry | Build call | Use when |
|---|---|---|---|
| Parameterized | `Q.Build()` | `BuildWithParameters()` | Production code, user-supplied values |
| Literal | `Q.Build(false)` | `Build()` | Reporting, internal tooling, testing |

**Always use parameterized mode with any user-supplied value.** `WhereExplicitly()` (raw SQL injection point) throws in parameterized mode as a guard rail.

---

## Step-by-step walkthrough

### 1. SELECT

```csharp
Q.Build(false)
    .Select<User, Guid>(u => u.Id)                        // single column
    .Select<User, string>(u => u.Name, alias: "UserName") // with alias
    .Select<Order, decimal>(o => o.Amount)                // from another table (after joining)
    .Build();
```

```csharp
// Aggregate functions
.Aggregate<Order, decimal>(o => o.Amount, "Total",   AggregateFunction.Sum)
.Aggregate<Order, Guid>  (o => o.Id,     "Count",   AggregateFunction.Count)
.Aggregate<Order, decimal>(o => o.Amount, "MaxAmt",  AggregateFunction.Max)
.Aggregate<Order, decimal>(o => o.Amount, "MinAmt",  AggregateFunction.Min)
.Aggregate<Order, decimal>(o => o.Amount, "Average", AggregateFunction.Avg)
.Aggregate<Order, Guid>  (o => o.Id,     "Unique",  AggregateFunction.CountDistinct)
```

```csharp
// Modifiers
.Top(100)       // SELECT TOP 100
.Distinct()     // SELECT DISTINCT
```

### 2. JOINS

```csharp
// INNER JOIN (most common)
.InnerJoin<User, Order, Guid, Guid>(u => u.Id, o => o.UserId)

// LEFT JOIN — all users, even those with no orders
.LeftJoin<User, Order, Guid, Guid>(u => u.Id, o => o.UserId)

// RIGHT JOIN
.RightJoin<Order, Product, int, int>(o => o.ProductId, p => p.Id)

// FULL OUTER JOIN
.FullOuterJoin<User, Order, Guid, Guid>(u => u.Id, o => o.UserId)

// CROSS JOIN — Cartesian product, no ON clause
.CrossJoin<Product, Region>()
```

### 3. WHERE

```csharp
// First predicate
.Where<User, string>(u => u.Name, FilterOperator.EqualTo, "Alice")

// Additional predicates
.AndWhere<User, int>(u => u.Age, FilterOperator.GreaterThan, 18)
.OrWhere<User, string>(u => u.Name, FilterOperator.StartsWith, "A")

// NULL checks
.WhereIsNull<Order, DateTime?>(o => o.DeletedAt)
.AndWhereIsNotNull<User, string>(u => u.Name)

// Range check
.WhereBetween<User, int>(u => u.Age, 18, 65)
.AndWhereBetween<Order, decimal>(o => o.Amount, 100, 1000)

// Set membership
.WhereIn<Order, string, string>(o => o.Status, new[] { "new", "processing" })
.WhereNotIn<Order, string, string>(o => o.Status, new[] { "cancelled" })

// Subquery existence
.WhereExists(subQueryBuilder)
.AndWhereExists(subQueryBuilder)
.WhereNotExists(subQueryBuilder)

// Parenthesis groups (nestable)
.Where<Order, string>(o => o.Status, FilterOperator.EqualTo, "new")
.OpenGroup()
    .OrWhere<Order, decimal>(o => o.Amount, FilterOperator.GreaterThan, 100)
    .OrWhere<Order, decimal>(o => o.Amount, FilterOperator.LessThan, 5)
.CloseGroup()
```

#### All `FilterOperator` values

| Operator | SQL |
|---|---|
| `EqualTo` | `=` |
| `NotEqualTo` | `<>` |
| `LessThan` | `<` |
| `LessThanOrEqualTo` | `<=` |
| `GreaterThan` | `>` |
| `GreaterThanOrEqualTo` | `>=` |
| `StartsWith` | `Like 'value%'` |
| `Contains` | `Like '%value%'` |
| `EndsWith` | `Like '%value'` |
| `IsNull` | `IS NULL` |
| `IsNotNull` | `IS NOT NULL` |
| `Between` | `BETWEEN` (use `WhereBetween`) |
| `NotBetween` | `NOT BETWEEN` (use `WhereNotBetween`) |

### 4. GROUP BY and HAVING

```csharp
Q.Build(false)
    .Aggregate<Order, decimal>(o => o.Amount, "Total", AggregateFunction.Sum)
    .InnerJoin<User, Order, Guid, Guid>(u => u.Id, o => o.UserId)
    .GroupBy<Order, Guid>(o => o.UserId)
    .Having<Order, decimal>(o => o.Amount, FilterOperator.GreaterThan, 100)
    .Build();
```

### 5. ORDER BY

```csharp
.OrderBy<User, string>(u => u.Name)                      // ASC
.OrderByDescending<User, DateTime>(u => u.CreatedAt)     // DESC

// Multiple columns — call in priority order
.OrderBy<User, string>(u => u.LastName)
.ThenBy<User, string>(u => u.FirstName)
.ThenByDescending<User, DateTime>(u => u.CreatedAt)
```

### 6. Paging

Choose the flavor for your database:

```csharp
// SQL Server (ROW_NUMBER — works on SQL Server 2005+)
.PageSqlServer<User, string>(u => u.Name, page: 1, pageSize: 20)

// SQL Server 2012+ / ANSI SQL (OFFSET FETCH)
.PageOffsetFetch<User, string>(u => u.Name, page: 2, pageSize: 20)

// MySQL / MariaDB (LIMIT OFFSET)
.PageMySql<User, string>(u => u.Name, page: 1, pageSize: 20)
```

Pages are **1-based** — page 1 is the first page.

### 7. CASE WHEN

```csharp
var statusLabel = CaseWhenBuilder.For<Order>()
    .When<Order, string>(o => o.Status, FilterOperator.EqualTo, "active").Then("Active")
    .When<Order, string>(o => o.Status, FilterOperator.EqualTo, "closed").Then("Closed")
    .Else("Unknown");

Q.Build(false)
    .Select<Order, Guid>(o => o.Id)
    .SelectCaseWhen(statusLabel, alias: "StatusLabel")
    .Build();
```

### 8. UNION / UNION ALL / INTERSECT / EXCEPT

```csharp
var activeUsers = Q.Build(false).Select<User, Guid>(u => u.Id)
    .Where<User, bool>(u => u.Active, FilterOperator.EqualTo, true);

var premiumUsers = Q.Build(false).Select<User, Guid>(u => u.Id)
    .Where<User, string>(u => u.Tier, FilterOperator.EqualTo, "premium");

// Set operations — chain on the left-hand query before calling Build()
var sql = activeUsers
    .UnionAll(premiumUsers)   // or .Union() .Intersect() .Except()
    .Build();
```

**Important:** Call `Build()` on the combined query, not on the individual sub-queries beforehand.

### 9. Common Table Expressions (CTEs)

```csharp
var activeOrders = Q.Build(false)
    .Select<Order, Guid>(o => o.Id)
    .Where<Order, string>(o => o.Status, FilterOperator.EqualTo, "active");

var sql = Q.Build(false)
    .WithCte("ActiveOrders", activeOrders)
    .Select<User, Guid>(u => u.Id)
    .Build();

// Emits:
// With ActiveOrders As (
//   ...inner query...
// )
// Select * from (...) as t
```

Multiple CTEs are comma-separated automatically:
```csharp
Q.Build(false)
    .WithCte("CTE1", query1)
    .WithCte("CTE2", query2)
    .Select<User, string>(u => u.Name)
    .Build();
```

---

## Complete real-world example

```csharp
// "Get paged list of active users with their total order amounts,
//  filtered to users with total > 500, ordered by total descending"

var result = Q.Build()
    .Select<User, Guid>(u => u.Id)
    .Select<User, string>(u => u.Name, alias: "UserName")
    .Aggregate<Order, decimal>(o => o.Amount, "TotalAmount", AggregateFunction.Sum)
    .LeftJoin<User, Order, Guid, Guid>(u => u.Id, o => o.UserId)
    .Where<User, bool>(u => u.Active, FilterOperator.EqualTo, true)
    .AndWhereIsNull<User, DateTime?>(u => u.DeletedAt)
    .GroupBy<Order, Guid>(o => o.UserId)
    .Having<Order, decimal>(o => o.Amount, FilterOperator.GreaterThan, 500)
    .OrderByDescending<Order, decimal>(o => o.Amount)
    .PageSqlServer<Order, decimal>(o => o.Amount, page: 1, pageSize: 25)
    .BuildWithParameters();

var rows = await connection.QueryAsync(result.ParameterizedSql, result.Parameters);
```

---

## Pitfalls and edge cases

### Build() is single-use

```csharp
var qb = Q.Build(false).Select<User, Guid>(u => u.Id);
var sql1 = qb.Build();   // OK
var sql2 = qb.Build();   // throws InvalidOperationException — create a new QBuilder
```

Create a new `QBuilder` for each query execution.

### WhereIn / WhereNotIn with null or empty collections

```csharp
// Both of these silently no-op — no WHERE clause is emitted
.WhereIn<Order, string, string>(o => o.Status, null)
.WhereIn<Order, string, string>(o => o.Status, new string[0])
```

This is intentional — it makes it safe to pass optional filter lists. If you need an impossible condition (`1=0`), emit it explicitly.

### WhereExplicitly is blocked in parameterized mode

```csharp
var qb = Q.Build();   // parameterize: true
qb.UseFilter().WhereExplicitly("Status = 'active'");  // throws InvalidOperationException
```

Use a typed `Where` overload instead, which binds values as parameters.

### OpenGroup / CloseGroup must be balanced

Every `OpenGroup()` must be paired with a `CloseGroup()`. QBuilder validates this and throws at `Build()` time:

```csharp
qb.OpenGroup()
    .OrWhere<Order, string>(o => o.Status, FilterOperator.EqualTo, "a")
// Missing CloseGroup() → Build() throws "An unclosed parentheses was found"
```

### Set operations consume sub-queries eagerly

When you call `Union(other)`, `other.Build()` is called immediately. Any changes made to `other` after this call are ignored.

### Parameterized mode and BuildWithParameters

```csharp
// Wrong — Build() throws if parameterize: true
Q.Build().Select<User, Guid>(u => u.Id).Build();

// Right
Q.Build().Select<User, Guid>(u => u.Id).BuildWithParameters();

// Also wrong — BuildWithParameters() throws if parameterize: false
Q.Build(false).Select<User, Guid>(u => u.Id).BuildWithParameters();
```

---

## Best practices

1. **Always use parameterized mode with user-provided values.** `Q.Build()` defaults to `parameterize: true` for a reason.

2. **Define domain models as plain classes**, not EF entities. QBuilder only uses the type name and property names — no attributes needed.

3. **Use a custom resolver for non-trivial table naming:**
   ```csharp
   // Register once at application start
   var resolver = (Type t) => $"dbo.{t.Name}s";
   var qb = Q.Build(resolver);
   ```

4. **Inject `Q.Build(resolver)` factories** in DI-based apps rather than calling `Q.Build()` directly everywhere — this avoids scattering the resolver logic.

5. **For complex queries, build sub-queries first:**
   ```csharp
   var existsCheck = Q.Build(false)
       .Select<Order, Guid>(o => o.Id)
       .Where<Order, Guid>(o => o.UserId, FilterOperator.EqualTo, userId);

   var main = Q.Build(false)
       .Select<User, Guid>(u => u.Id)
       .WhereExists(existsCheck)
       .Build();
   ```

6. **Do not reuse a `QBuilder` instance.** Build, execute, discard.

7. **Test your queries in non-parameterized mode first** to inspect the SQL, then switch to parameterized mode for production.

---

## Backward compatibility

The v7.0 extension API coexists with the original `Use*()/Then()` API — all existing code compiles without changes. The extension methods are purely additive.

```csharp
// Old API — still works
new QBuilder("t", parameterize: false)
    .UseTableBoundSelector<User>()
    .Select(u => u.Id)
    .Then()
    .UseFilter()
    .Where<User>("Name", FilterOperator.EqualTo, "Alice")
    .Then().Build();

// New API — same result
Q.Build(false)
    .Select<User, Guid>(u => u.Id)
    .Where<User, string>(u => u.Name, FilterOperator.EqualTo, "Alice")
    .Build();
```

---

## Window functions (v7.1 roadmap)

LAG, LEAD, RANK, DENSE_RANK, ROW_NUMBER OVER (PARTITION BY), SUM OVER — these are planned for v7.1. The ROW_NUMBER() paging infrastructure already exists; the full window-function surface will be layered on top.

---

## License

MIT — see [LICENSE](LICENSE).
