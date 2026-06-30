# Jattac.Libraries.QBuilder

A **fully-fledged C# dialect of SQL** — a fluent, type-safe query builder for .NET 6+ that covers SELECT (full ANSI SQL), DML (INSERT, UPDATE, DELETE), and full identifier quoting for SQL Server, MySQL/MariaDB, SQLite, PostgreSQL, and ANSI Generic dialects.

[![NuGet](https://img.shields.io/nuget/v/Jattac.Libraries.QBuilder.svg)](https://www.nuget.org/packages/Jattac.Libraries.QBuilder)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## Why QBuilder?

Raw SQL strings in application code are fragile — typos are runtime errors, refactors are grep-and-pray, and parameterization is manual. ORMs are heavy and leak abstractions.

QBuilder sits in the middle: it is **purely a query builder**. It produces SQL strings (optionally with named parameters) that you execute yourself with Dapper, ADO.NET, or any micro-ORM. No tracking, no migrations, no change detection — just clean, tested SQL.

**Key properties**
- Zero boilerplate — one fluent chain, every type argument inferred, no sub-builder ceremonies
- Parameterized by default — injection-safe without extra effort
- **Dialect-aware quoting** — configure SQL Server, MySQL/MariaDB, SQLite, PostgreSQL, or ANSI Generic once at startup; all identifiers are quoted correctly throughout every clause
- **Startup configuration** — `QBuilderConfig.ConfigureDefault` + named configs follow the `IHttpClientFactory` pattern; `Q.New()` / `Q.New("name")` resolve the right config automatically
- Full SQL coverage — SELECT, JOIN (5 types), WHERE, GROUP BY, HAVING, ORDER BY, paging (ROW_NUMBER, OFFSET/FETCH, LIMIT), UNION / INTERSECT / EXCEPT, CTEs, CASE WHEN, EXISTS, BETWEEN, IS NULL, IN
- Full DML coverage — type-safe INSERT, UPDATE (with SET), DELETE, all with parameterized output and optional SQL logger; bare-table UPDATE/DELETE blocked by default
- **POCO convenience** — `FromObject(poco)` on INSERT/UPDATE/DELETE reflects properties automatically; `[QKey]` / `[QIgnore]` / `[QColumn]` attributes control routing; `FromObjects(list)` for bulk multi-row insert; `QBatch` for multi-statement round-trips with automatic parameter collision renaming
- Every public member has XML doc comments — your IDE guides you at every step
- 255 unit + integration tests (including live SQLite), 0 failures

---

## Installation

```bash
dotnet add package Jattac.Libraries.QBuilder
```

---

## Quick start

```csharp
using Jattac.Libraries.QBuilder;
using Jattac.Libraries.QBuilder.Config;
using Jattac.Libraries.QBuilder.Enums;

// 1. Configure once at application startup (Program.cs / Startup.cs)
QBuilderConfig.ConfigureDefault(opt =>
{
    opt.Dialect = Dialect.SqlServer;
    opt.TableNameResolver = t => "dbo." + t.Name;
});

// 2. Use anywhere — no dialect argument needed
var built = Q.New()
    .UseTableBoundSelector<User>().Column(u => u.Id).Column(u => u.Name, "UserName").Then()
    .UseTableBoundFilter<User>()
        .WhereEqualTo(u => u.IsActive, true)
        .AndWhereIsNull(u => u.DeletedAt)
    .Then()
    .UseTableBoundOrderBy<User>().Ascending(u => u.Name).Then()
    .BuildWithParameters();

// 3. Execute with Dapper
var users = await connection.QueryAsync<User>(built.ParameterizedSql, built.Parameters);
// SQL: SELECT [dbo].[tUser].[Id], [dbo].[tUser].[Name] AS [UserName]
//      FROM [dbo].[User] [tUser]
//      WHERE [tUser].[IsActive] = @IsActive0 And [tUser].[DeletedAt] Is Null
//      ORDER BY [tUser].[Name] Asc
```

---

## Configuring a dialect

### Configure once at startup

```csharp
// Program.cs / Startup.cs
QBuilderConfig.ConfigureDefault(opt =>
{
    opt.Dialect           = Dialect.SqlServer;          // or MySql, MariaDb, Sqlite, Postgres, Generic
    opt.TableNameResolver = t => "dbo." + t.Name;       // optional: add schema prefix
    opt.AliasPrefix       = "t";                        // optional: default is "t"
});
```

After this call every `Q.New()` in the process uses the configured dialect.

### Register named configurations

Use named configs for multi-database scenarios — a write database, a read replica, a different schema:

```csharp
QBuilderConfig.ConfigureDefault(opt =>
{
    opt.Dialect           = Dialect.SqlServer;
    opt.TableNameResolver = t => "dbo." + t.Name;
});

QBuilderConfig.Configure("analytics", opt =>
{
    opt.Dialect           = Dialect.SqlServer;
    opt.TableNameResolver = t => "analytics." + t.Name;
});

QBuilderConfig.Configure("mysql-readonly", opt =>
{
    opt.Dialect           = Dialect.MySql;
    opt.TableNameResolver = t => t.Name.ToLower();
});
```

### Use named configurations

```csharp
var q = Q.New();                    // uses default config
var q = Q.New("analytics");         // uses "analytics" config
var q = Q.New("mysql-readonly");    // uses "mysql-readonly" config
var q = Q.New("analytics", false);  // named config, non-parameterized
```

### Supported dialects

| Dialect | Quote style | Paging strategy |
|---|---|---|
| `Dialect.None` | none (backward-compat default) | OFFSET/FETCH |
| `Dialect.SqlServer` / `MsSql` | `[identifier]` | ROW_NUMBER() |
| `Dialect.MySql` | `` `identifier` `` | LIMIT |
| `Dialect.MariaDb` | `` `identifier` `` | LIMIT |
| `Dialect.Sqlite` | `"identifier"` | OFFSET/FETCH |
| `Dialect.Postgres` | `"identifier"` | OFFSET/FETCH |
| `Dialect.Generic` | `"identifier"` | OFFSET/FETCH |

`MsSql` is a value alias for `SqlServer` — they are interchangeable.

Quoting is applied to every identifier in every clause: table names, table aliases, column names, column aliases, JOIN ON fields, WHERE fields, ORDER BY, GROUP BY, DML table names, INSERT column list, and UPDATE SET clause.

### Inline config (one-off / tests)

```csharp
// Bypass the registry for a single instance
var q = Q.New(new QBuilderOptions { Dialect = Dialect.MySql }, parameterize: false);

// Inline resolver only (backward-compatible, Dialect.None)
var q = Q.New(t => "schema." + t.Name, parameterize: false);
```

### Resetting configuration (tests)

```csharp
// Restore factory defaults — use in test teardown
QBuilderConfig.Reset();
```

### Recommendation

Configure dialect in `Program.cs` / `Startup.cs` for production apps. For libraries that wrap QBuilder, expose dialect configuration to the host app rather than hard-coding it. For tests, use `QBuilderConfig.Reset()` in `IDisposable.Dispose` or `try/finally` to avoid cross-test contamination.

---

## Core concepts

### Entry point — `Q.New()`

```csharp
// Default config (reads from QBuilderConfig.ConfigureDefault)
var qb = Q.New();
var qb = Q.New(parameterize: false);

// Named config
var qb = Q.New("analytics");
var qb = Q.New("reporting", parameterize: false);

// Inline options (one-off, bypasses registry)
var qb = Q.New(new QBuilderOptions { Dialect = Dialect.Postgres });

// Inline resolver (backward-compatible)
var qb = Q.New(t => "dbo." + t.Name + "s");
```

### The fluent chain pattern

Every clause entry point is a method on `QBuilder` returning a typed builder. Predicates return the same builder for chaining. `.Then()` exits back to `QBuilder` for the next clause.

```
SELECT / JOIN / WHERE / GROUP / ORDER chain:
  Q.New()
    .UseTableBoundSelector<T>()      → TableBoundSelectBuilder<T>     → .Then() → QBuilder
    .UseTableBoundFilter<T>()        → TableBoundWhereBuilder<T>      → .Then() → QBuilder
    .UseTableBoundHaving<T>()        → TableBoundHavingBuilder<T>     → .Then() → QBuilder
    .UseTableBoundOrderBy<T>()       → TableBoundOrderByBuilder<T>    → .Then() → QBuilder
    .UseTableBoundGrouper<T>()       → TableBoundGroupBuilder<T>      → GroupBy() returns QBuilder directly
    .UseTableBoundJoinBuilder<L,R>() → TableBoundJoinBuilder<L,R>     → InnerJoin/LeftJoin returns QBuilder directly
    .Build() / .BuildWithParameters()

DML terminal chains (do NOT chain back to QBuilder.Build()):
  Q.New()
    .UseTableBoundInsert<T>()  → TableBoundInsertBuilder<T>  → .Value() → ... → .BuildWithParameters()
    .UseTableBoundUpdate<T>()  → TableBoundUpdateBuilder<T>  → .Set() + .Where*() → .BuildWithParameters()
    .UseTableBoundDelete<T>()  → TableBoundDeleteBuilder<T>  → .Where*() → .BuildWithParameters()
```

### Table aliases

Every CLR type gets an alias prefix from its name: `User` → `tUser`, `Order` → `tOrder`. The schema prefix is stripped when using a custom resolver — `dbo.Users` → `tUsers`. With a dialect configured the alias is quoted: `[tUser]` (SQL Server), `` `tUser` `` (MySQL).

### Parameterized vs literal mode

| Mode | Entry | Build call | Use when |
|---|---|---|---|
| Parameterized | `Q.New()` | `BuildWithParameters()` | Production — user-supplied values |
| Literal | `Q.New(false)` | `Build()` | Reporting, internal tooling, debugging |

---

## SELECT

```csharp
Q.New(false)
    // Single column
    .UseTableBoundSelector<User>().Column(u => u.Id).Then()

    // Column with alias
    .UseTableBoundSelector<User>().Column(u => u.Name, "UserName").Then()

    // Modifiers — call before Column()
    .UseTableBoundSelector<User>().Distinct().Column(u => u.Email).Then()
    .UseTableBoundSelector<User>().Top(100).Column(u => u.Name).Then()

    // Multiple columns in one builder scope
    .UseTableBoundSelector<User>()
        .Column(u => u.Id)
        .Column(u => u.Name, "UserName")
        .Column(u => u.Email)
    .Then()

    // Aggregates
    .UseTableBoundSelector<Order>().Aggregate(o => o.Amount, "Total",  AggregateFunction.Sum).Then()
    .UseTableBoundSelector<Order>().Aggregate(o => o.Amount, "MaxAmt", AggregateFunction.Max).Then()
    .UseTableBoundSelector<Order>().Aggregate(o => o.Amount, "MinAmt", AggregateFunction.Min).Then()
    .UseTableBoundSelector<Order>().Aggregate(o => o.Amount, "Avg",    AggregateFunction.Avg).Then()
    .UseTableBoundSelector<Order>().Aggregate(o => o.Id,     "Cnt",    AggregateFunction.Count).Then()
    .UseTableBoundSelector<Order>().Aggregate(o => o.Id,     "Unique", AggregateFunction.CountDistinct).Then()

    .Build();
```

### CASE WHEN

```csharp
var statusLabel = CaseWhenBuilder.For<Order>()
    .When<Order, string>(o => o.Status, FilterOperator.EqualTo, "active").Then("Active")
    .When<Order, string>(o => o.Status, FilterOperator.EqualTo, "closed").Then("Closed")
    .Else("Unknown");

Q.New(false)
    .UseTableBoundSelector<Order>()
        .Column(o => o.Id)
        .CaseWhen(statusLabel, "StatusLabel")
    .Then()
    .Build();
```

---

## JOIN

```csharp
// All join methods return QBuilder directly — no .Then() needed

// INNER JOIN
Q.New(false)
    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
    .UseTableBoundSelector<Order>().Column(o => o.Amount).Then()
    .UseTableBoundJoinBuilder<User, Order>().InnerJoin(u => u.Id, o => o.UserId)
    .Build();

// LEFT JOIN
.UseTableBoundJoinBuilder<User, Order>().LeftJoin(u => u.Id, o => o.UserId)

// RIGHT JOIN
.UseTableBoundJoinBuilder<Order, Product>().RightJoin(o => o.ProductId, p => p.Id)

// FULL OUTER JOIN
.UseTableBoundJoinBuilder<User, Order>().FullJoin(u => u.Id, o => o.UserId)

// CROSS JOIN (Cartesian product — no ON clause)
.UseJoiner().CrossJoin<User, Product>().Then()

// Self-join — use explicit aliases to disambiguate
.UseTableBoundJoinBuilder<Employee, Employee>()
    .InnerJoin(e => e.ManagerId, m => m.Id, leftAlias: "tEmp", rightAlias: "tMgr")
```

---

## WHERE

Every predicate is a method on `TableBoundWhereBuilder<T>`. Call `.Then()` to exit back to `QBuilder`.

```csharp
.UseTableBoundFilter<User>()

    // ── equality ─────────────────────────────────────────────────────────────
    .WhereEqualTo(u => u.Name, "Alice")
    .WhereNotEqualTo(u => u.Status, "banned")

    // ── comparison ───────────────────────────────────────────────────────────
    .WhereGreaterThan(u => u.Age, 18)
    .WhereGreaterThanOrEqualTo(u => u.Age, 18)
    .WhereLessThan(u => u.Age, 65)
    .WhereLessThanOrEqualTo(u => u.Age, 65)

    // ── string search ─────────────────────────────────────────────────────────
    .WhereContains(u => u.Name, "ali")            // LIKE '%ali%'
    .WhereStartsWith(u => u.Name, "Al")           // LIKE 'Al%'
    .WhereEndsWith(u => u.Name, "ce")             // LIKE '%ce'

    // ── null checks ───────────────────────────────────────────────────────────
    .WhereIsNull(u => u.DeletedAt)
    .WhereIsNotNull(u => u.DeletedAt)

    // ── range ─────────────────────────────────────────────────────────────────
    .WhereBetween(u => u.Age, 18, 65)
    .WhereNotBetween(u => u.CreatedAt, cutoffStart, cutoffEnd)

    // ── set membership ────────────────────────────────────────────────────────
    .WhereIn<string, string>(u => u.Status, new[] { "active", "pending" })
    .WhereNotIn<string, string>(u => u.Status, new[] { "banned" })

    // ── subquery existence ────────────────────────────────────────────────────
    .WhereExists(subQueryBuilder)
    .WhereNotExists(subQueryBuilder)

.Then()
```

### AND / OR continuation

All predicates have `And*` and `Or*` variants. They all return the same builder instance — stay in scope until `.Then()`.

```csharp
.UseTableBoundFilter<User>()
    .WhereEqualTo(u => u.IsActive, true)
    .AndWhereIsNull(u => u.DeletedAt)
    .AndWhereGreaterThan(u => u.Age, 18)
    .OrWhereEqualTo(u => u.Role, "admin")
    .OrWhereIn<string, string>(u => u.Status, new[] { "premium", "vip" })
.Then()
```

Full families: `And/OrWhereEqualTo`, `And/OrWhereNotEqualTo`, `And/OrWhereLessThan`, `And/OrWhereLessThanOrEqualTo`, `And/OrWhereGreaterThan`, `And/OrWhereGreaterThanOrEqualTo`, `And/OrWhereContains`, `And/OrWhereStartsWith`, `And/OrWhereEndsWith`, `And/OrWhereIsNull`, `And/OrWhereIsNotNull`, `And/OrWhereIn`, `And/OrWhereNotIn`, `And/OrWhereBetween`, `And/OrWhereNotBetween`, `And/OrWhereExists`, `And/OrWhereNotExists`.

### Grouping predicates with parentheses

```csharp
.UseTableBoundFilter<Order>()
    .WhereEqualTo(o => o.UserId, currentUserId)
    .OpenGroup()
        .WhereEqualTo(o => o.Status, "new")
        .OrWhereEqualTo(o => o.Status, "processing")
    .CloseGroup()
.Then()
// SQL: WHERE tOrder.UserId = @UserId0 And ( tOrder.Status = @Status0 Or tOrder.Status = @Status1 )
```

Every `OpenGroup()` must be paired with a `CloseGroup()`. Nesting is supported.

### Conditional filter — `.If()`

```csharp
string nameFilter = Request.Query["name"];

.UseTableBoundFilter<User>()
    .If(!string.IsNullOrEmpty(nameFilter),
        fb => fb.WhereContains(u => u.Name, nameFilter))
    .If(showOnlyActive,
        fb => fb.AndWhereEqualTo(u => u.IsActive, true))
.Then()
```

When the condition is false the lambda is not invoked — the chain continues unbroken.

### Raw SQL escape hatch

```csharp
// Non-parameterized only — use for expressions the builder can't express
.UseTableBoundFilter<User>().WhereExplicitly("YEAR(tUser.CreatedAt) = 2024").Then()

// Parameterized with manual parameters
.UseTableBoundFilter<User>()
    .WhereExplicitly("YEAR(tUser.CreatedAt) = @year", new { year = 2024 })
.Then()
```

`WhereExplicitly(string)` (single-arg) throws in parameterized mode as a guard rail. Use the two-arg overload with an anonymous object to supply parameters safely.

---

## GROUP BY / HAVING

```csharp
Q.New(false)
    .UseTableBoundSelector<Order>()
        .Column(o => o.UserId)
        .Aggregate(o => o.Amount, "Total", AggregateFunction.Sum)
    .Then()
    .UseTableBoundJoinBuilder<User, Order>().InnerJoin(u => u.Id, o => o.UserId)
    .UseTableBoundGrouper<Order>().GroupBy(o => o.UserId)       // returns QBuilder
    .UseTableBoundHaving<Order>().HavingGreaterThan(o => o.Amount, 500).Then()
    .Build();
```

`GroupBy()` returns `QBuilder` directly (no `.Then()` needed). Chain `UseTableBoundHaving<T>()` directly after it.

### HAVING predicates

```csharp
.UseTableBoundHaving<Order>()
    .HavingEqualTo(o => o.Status, "active")
    .AndHavingGreaterThan(o => o.Amount, 100)
    .OrHavingLessThan(o => o.Amount, 10)
    .HavingIsNull(o => o.DeletedAt)
.Then()
```

Available: `Having/AndHaving/OrHaving` + `EqualTo`, `NotEqualTo`, `GreaterThan`, `GreaterThanOrEqualTo`, `LessThan`, `LessThanOrEqualTo`, `IsNull`, `IsNotNull`.

---

## ORDER BY

```csharp
.UseTableBoundOrderBy<User>()
    .Ascending(u => u.Name)              // single column ASC
.Then()

.UseTableBoundOrderBy<User>()
    .Descending(u => u.CreatedAt)        // single column DESC
.Then()

// Multi-column — chain ThenAscending / ThenDescending
.UseTableBoundOrderBy<User>()
    .Ascending(u => u.LastName)
    .ThenAscending(u => u.FirstName)
    .ThenDescending(u => u.CreatedAt)
.Then()
```

`T` does not have to be the root table — you can sort by any joined table's column using the same API:

```csharp
// Sort by a column from a joined table
Q.New()
    .UseTableBoundSelector<Item>().Column(i => i.Id).Then()
    .UseTableBoundJoinBuilder<ViewItemPurchaseSummary, Item>()
        .InnerJoin(v => v.ItemId, i => i.Id)
    .UseTableBoundOrderBy<ViewItemPurchaseSummary>()
        .Descending(v => v.LastTransactionDate)
    .Then()
    .Build();
// → ORDER BY tViewItemPurchaseSummary.LastTransactionDate Desc
```

---

## PAGING

### Automatic paging via dialect (recommended)

Configure the dialect once and call `UsePageBy` — it dispatches to the correct strategy automatically:

```csharp
// Program.cs
QBuilderConfig.ConfigureDefault(opt => opt.Dialect = Dialect.SqlServer);

// Query — no engine-specific call needed
var built = Q.New(false)
    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
    .UsePageBy<User, string>(u => u.Name, page: 1, pageSize: 25)
    .Build();
// → emits ROW_NUMBER for SqlServer, LIMIT for MySql/MariaDb, OFFSET/FETCH for everything else
```

Changing the dialect in one place (`QBuilderConfig.ConfigureDefault`) is enough — no query-level changes needed.

### Explicit paging builders (advanced / override)

Use the explicit variants when you need to override the dialect setting for a specific query, or when using a mix of engines in the same application:

```csharp
// SQL Server — ROW_NUMBER (SQL Server 2005+)
.UseSqlServerPagingBuilder<User>().PageBy(u => u.Name, page: 1, pageSize: 20).Build()

// SQL Server 2012+ / ANSI — OFFSET … ROWS FETCH NEXT … ROWS ONLY
.UseOffsetFetchPagingBuilder<User>().PageBy(u => u.Name, page: 2, pageSize: 20).Build()

// MySQL / MariaDB — LIMIT
.UseMySqlServerPagingBuilder<User>().PageBy(u => u.Name, page: 1, pageSize: 20).Build()
```

Pages are **1-based**. Page 1 = first page, rows 1–pageSize.

### Paging pitfall — combine with a prior WHERE

Paging is appended as a suffix. Any WHERE predicate must be declared before the paging builder:

```csharp
// CORRECT
Q.New()
    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
    .UseTableBoundFilter<User>().WhereEqualTo(u => u.IsActive, true).Then()
    .UsePageBy<User, string>(u => u.Name, page: 1, pageSize: 10)
    .Build();

// WRONG — filter after paging is ignored
Q.New()
    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
    .UsePageBy<User, string>(u => u.Name, page: 1, pageSize: 10)
    .UseTableBoundFilter<User>().WhereEqualTo(u => u.IsActive, true).Then()  // ← silently lost
    .Build();
```

---

## CTEs

```csharp
var activeOrders = Q.New(false)
    .UseTableBoundSelector<Order>().Column(o => o.Id).Column(o => o.Amount).Then()
    .UseTableBoundFilter<Order>().WhereEqualTo(o => o.Status, "active").Then();

var sql = Q.New(false)
    .WithCte("ActiveOrders", activeOrders)
    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
    .Build();
// Emits: With ActiveOrders As (...) Select * from (...) as t
```

Multiple CTEs are comma-separated automatically:

```csharp
Q.New(false)
    .WithCte("CTE1", q1)
    .WithCte("CTE2", q2)
    .UseTableBoundSelector<User>().Column(u => u.Name).Then()
    .Build();
```

---

## Derived table queries

### Selecting from a subquery (`UseDerivedTableSelector`)

When you need to project columns from a subquery placed in the FROM clause, wrap the inner `QBuilder` with `UseDerivedTableSelector`:

```csharp
// Inner — aggregate order lines per order
var lineTotals = Q.New(false)
    .UseTableBoundSelector<OrderLine>()
        .Column(ol => ol.OrderId)
        .Aggregate(ol => ol.Amount, "LineTotal", AggregateFunction.Sum)
    .Then()
    .UseTableBoundGrouper<OrderLine>().GroupBy(ol => ol.OrderId);

// Outer — select from Order plus the LineTotal column from the derived table
var sql = Q.New(false)
    .UseTableBoundSelector<Order>().Column(o => o.Id).Column(o => o.CreatedAt).Then()
    .UseDerivedTableSelector(lineTotals)
        .Select("LineTotal")                      // plain column
        .Select("LineTotal", "TotalAmount")       // column with alias
    .Then()
    .UseJoiner()
        .UseDerivedTableJoiner<Order>()
        .InnerJoin(o => o.Id, lineTotals, "OrderId")
    .Then()
    .Build();
```

### Joining a derived table (`UseDerivedTableJoiner`)

To join a subquery against an outer table, chain `UseDerivedTableJoiner<T>()` off `UseJoiner()`. All four join types are available — `InnerJoin`, `LeftJoin`, `RightJoin`, `FullJoin`. The third argument is the field name **as a string** in the derived table:

```csharp
var totals = Q.New(false)
    .UseTableBoundSelector<OrderLine>()
        .Column(ol => ol.OrderId)
        .Aggregate(ol => ol.Amount, "LineTotal", AggregateFunction.Sum)
    .Then()
    .UseTableBoundGrouper<OrderLine>().GroupBy(ol => ol.OrderId);

var sql = Q.New(false)
    .UseTableBoundSelector<Order>().Column(o => o.Id).Then()
    .UseJoiner()
        .UseDerivedTableJoiner<Order>()
        .InnerJoin(o => o.Id, totals, "OrderId")   // ← outer field, inner QBuilder, inner field name
    .Then()
    .Build();
```

Multiple derived table joins against the same outer table can be chained by calling `UseDerivedTableJoiner<T>()` again on the returned `JoinBuilder`:

```csharp
.UseJoiner()
    .UseDerivedTableJoiner<TaskExecutionHistory>()
    .InnerJoin(teh => teh.TaskDefinitionId, inner, nameof(TaskExecutionHistory.TaskDefinitionId))
    .UseDerivedTableJoiner<TaskExecutionHistory>()
    .InnerJoin(teh => teh.EndTime, inner, "LatestVersion")
.Then()
```

### `AggregateRowQuerierBuilder<T>` — latest-row-per-group helper

For the classic "latest record per foreign key" pattern (e.g. the most recent execution per task), `AggregateRowQuerierBuilder<T>` builds the inner subquery + join automatically:

```csharp
using Jattac.Libraries.QBuilder.Helpers;

var latestRunPerTask = new AggregateRowQuerierBuilder<TaskExecutionHistory>()
    .SetAggregationFunction("Max")
    .SetForeignKeyResolver(teh => teh.TaskDefinitionId)
    .SetIncrementingFieldName(teh => teh.EndTime)
    .AddWhereEqualsToFilter(teh => teh.Succeeded, true)   // optional inner filter
    .Build();   // returns a QBuilder (the derived table)

// Use the result as a derived table join in a larger query
var sql = Q.New(false)
    .UseTableBoundSelector<TaskDefinition>().Column(td => td.Name).Then()
    .UseJoiner()
        .UseDerivedTableJoiner<TaskDefinition>()
        .InnerJoin(td => td.Id, latestRunPerTask, nameof(TaskExecutionHistory.TaskDefinitionId))
    .Then()
    .Build();
```

`Build()` returns a `QBuilder` you can pass directly to `UseDerivedTableSelector` or `UseDerivedTableJoiner`. Optional filters via `AddWhereEqualsToFilter` / `AddWhereInFilter` are applied to both the inner aggregation and the outer result query.

---

## Set operations

```csharp
var activeUsers = Q.New(false)
    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
    .UseTableBoundFilter<User>().WhereEqualTo(u => u.IsActive, true).Then();

var premiumUsers = Q.New(false)
    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
    .UseTableBoundFilter<User>().WhereEqualTo(u => u.Tier, "premium").Then();

var sql = activeUsers
    .UnionAll(premiumUsers)    // or .Union() .Intersect() .Except()
    .Build();
```

Call `Build()` on the **combined** query. Do not call `Build()` on the sub-queries beforehand.

---

## Parameterized mode — complete example

```csharp
var built = Q.New()
    .UseTableBoundSelector<User>()
        .Column(u => u.Id)
        .Column(u => u.Name, "UserName")
    .Then()
    .UseTableBoundFilter<User>()
        .WhereEqualTo(u => u.IsActive, true)
        .AndWhereIsNull(u => u.DeletedAt)
        .If(!string.IsNullOrEmpty(nameFilter),
            fb => fb.AndWhereContains(u => u.Name, nameFilter))
    .Then()
    .UseTableBoundOrderBy<User>().Ascending(u => u.Name).Then()
    .BuildWithParameters();

// built.ParameterizedSql — SQL with @Name0 placeholders
// built.Parameters       — Dictionary<string, object>

// Optional: log the SQL for debugging
var built2 = Q.New()
    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
    .BuildWithParameters(sql => logger.LogDebug(sql));

// Execute with Dapper
var users = await conn.QueryAsync<User>(built.ParameterizedSql, built.Parameters);
```

---

## Full real-world example

```csharp
// Paged list of active users with their total order amounts,
// filtered to totals > 500, ordered by total descending.
// Dialect configured globally as SqlServer — UsePageBy dispatches automatically.

var built = Q.New()
    .UseTableBoundSelector<User>()
        .Column(u => u.Id)
        .Column(u => u.Name, "UserName")
    .Then()
    .UseTableBoundSelector<Order>()
        .Aggregate(o => o.Amount, "TotalAmount", AggregateFunction.Sum)
    .Then()
    .UseTableBoundJoinBuilder<User, Order>().LeftJoin(u => u.Id, o => o.UserId)
    .UseTableBoundFilter<User>()
        .WhereEqualTo(u => u.IsActive, true)
        .AndWhereIsNull(u => u.DeletedAt)
    .Then()
    .UseTableBoundGrouper<Order>().GroupBy(o => o.UserId)
    .UseTableBoundHaving<Order>().HavingGreaterThan(o => o.Amount, 500).Then()
    .UseTableBoundOrderBy<Order>().Descending(o => o.Amount).Then()
    .UsePageBy<Order, decimal>(o => o.Amount, page: 1, pageSize: 25)
    .Build();
```

---

## DML — INSERT, UPDATE, DELETE

DML builders are **terminal** — they do not chain back through `QBuilder.Build()`. Call `.BuildWithParameters()` at the end to get a `BuiltQuery` you pass directly to Dapper's `Execute`.

### INSERT

```csharp
var q = Q.New()
    .UseTableBoundInsert<User>()
    .Value(u => u.Id, Guid.NewGuid())
    .Value(u => u.Name, "Alice")
    .Value(u => u.IsActive, true)
    .Value(u => u.DeletedAt, null)
    .BuildWithParameters();

await conn.ExecuteAsync(q.ParameterizedSql, q.Parameters);
// SQL Server: Insert Into [dbo].[User] ([Id], [Name], [IsActive], [DeletedAt])
//             Values (@Id0, @Name0, @IsActive0, @DeletedAt0)
```

### UPDATE

```csharp
var q = Q.New()
    .UseTableBoundUpdate<User>()
    .Set(u => u.Name, "Alice Updated")
    .Set(u => u.IsActive, false)
    .WhereEqualTo(u => u.Id, userId)
    .BuildWithParameters();

await conn.ExecuteAsync(q.ParameterizedSql, q.Parameters);
// SQL Server: Update [dbo].[User] Set [Name] = @Name0, [IsActive] = @IsActive0
//             Where [tUser].[Id] = @Id0
```

### DELETE

```csharp
var q = Q.New()
    .UseTableBoundDelete<User>()
    .WhereEqualTo(u => u.Id, userId)
    .BuildWithParameters();

await conn.ExecuteAsync(q.ParameterizedSql, q.Parameters);
// SQL Server: Delete From [dbo].[User] Where [tUser].[Id] = @Id0
```

### Full WHERE predicate surface on DML

All `And*/Or*` predicates available on `TableBoundWhereBuilder<T>` are available on DELETE and UPDATE:

```csharp
Q.New()
    .UseTableBoundDelete<User>()
    .WhereEqualTo(u => u.IsActive, false)
    .AndWhereIsNotNull(u => u.DeletedAt)
    .BuildWithParameters();
```

### Explicit full-table operations

Calling `.BuildWithParameters()` without any WHERE predicate throws `InvalidOperationException`. Use `.ForEntireTable()` to confirm you intend a full-table operation:

```csharp
// Deactivate all users (intentional)
Q.New()
    .UseTableBoundUpdate<User>()
    .Set(u => u.IsActive, false)
    .ForEntireTable()
    .BuildWithParameters();

// Delete all soft-deleted records (intentional)
Q.New()
    .UseTableBoundDelete<User>()
    .ForEntireTable()
    .BuildWithParameters();
```

### SQL logging on DML

All three DML builders accept an optional `Action<string> logSql` delegate on `BuildWithParameters()`:

```csharp
var q = Q.New()
    .UseTableBoundDelete<User>()
    .WhereEqualTo(u => u.Id, userId)
    .BuildWithParameters(sql => logger.LogDebug("DML: {Sql}", sql));
```

### POCO convenience — `FromObject` / `FromObjects` / `QBatch`

Decorate your model once and skip the per-property boilerplate:

```csharp
using Jattac.Libraries.QBuilder.Attributes;

public class User
{
    [QKey]                        // → WHERE on UPDATE/DELETE; inserted normally on INSERT
    public Guid Id { get; set; }

    [QColumn("user_name")]        // → SQL column name override (FromObject path only)
    public string Name { get; set; }

    [QIgnore]                     // → skipped everywhere
    public bool InternalFlag { get; set; }
}
```

```csharp
// INSERT
Q.New().UseTableBoundInsert<User>().FromObject(user).BuildWithParameters();

// UPDATE — [QKey] → WHERE, rest → SET
Q.New().UseTableBoundUpdate<User>().FromObject(user).BuildWithParameters();

// DELETE — [QKey] → WHERE, everything else discarded
Q.New().UseTableBoundDelete<User>().FromObject(user).BuildWithParameters();

// Shorthand
Q.New().InsertFrom<User, User>(user).BuildWithParameters();
Q.New().UpdateFrom<User, User>(user).BuildWithParameters();
Q.New().DeleteFrom<User, User>(user).BuildWithParameters();
```

**Bulk INSERT** — single `VALUES` clause for multiple rows:

```csharp
Q.New().UseTableBoundInsert<User>().FromObjects(users).BuildWithParameters();
// → Insert Into User (Id, user_name) Values (@Id0, @Name0), (@Id1, @Name1), ...
```

**Batch DML** — multiple statements in one round-trip, parameters collision-renamed automatically:

```csharp
var batch = QBatch.New()
    .AddRange(users.Select(u =>
        Q.New().UseTableBoundUpdate<User>().FromObject(u).BuildWithParameters()))
    .Build();

conn.Execute(batch.ParameterizedSql, batch.Parameters);
```

Anonymous objects work with `FromObject` — since they carry no attributes, all properties are treated as regular values. For UPDATE/DELETE on anonymous objects, chain the WHERE clause manually.

See [`docs/migration-v7-to-v8.md`](docs/migration-v7-to-v8.md) for full attribute reference, DB-generated key patterns, chunking guidance, and `WhereIn` for bulk delete.

#### Inheritance and `new`-hidden properties

`FromObject` reflects the full public property list of the runtime type, including inherited properties. When a derived class hides a base-class property with the `new` keyword, both declarations appear in the reflection output. QBuilder deduplicates by column name and always keeps the **most-derived** declaration, so `new`-shadowed properties behave exactly as you'd expect.

```csharp
public class EntityBase
{
    public int Deleted { get; set; }  // base declaration
}

public class Order : EntityBase
{
    [QIgnore]
    public new int Deleted { get; set; }  // shadow with [QIgnore] → excluded from all SQL
}
```

A common pattern when working with shared base classes from external libraries: re-declare the unwanted property in an intermediate base with `[QIgnore]` to suppress it globally across every derived model.

```csharp
// In your own ModelBase, suppress a property inherited from a NuGet base class:
[QIgnore]
public new bool HasNoId => base.HasNoId;
```

---

### DML pitfalls and best practices

**Always use `Q.New()` (parameterized mode) for DML.** Embedding user-supplied values in a raw INSERT / UPDATE is as dangerous as raw SELECT.

**`ForEntireTable()` is a safety acknowledgment, not a shortcut.** If you find yourself calling it frequently, reconsider whether the WHERE clause should be required upstream in business logic.

**Identity / auto-increment columns.** QBuilder produces the INSERT statement only — it does not append `; SELECT LAST_INSERT_ID()` or `OUTPUT INSERTED.Id`. Retrieve the generated ID via Dapper's `ExecuteScalar` or `QuerySingle` with the appropriate identity query for your database.

```csharp
// SQL Server — get the inserted ID
var id = await conn.ExecuteScalarAsync<int>(insertSql + "; SELECT SCOPE_IDENTITY()", q.Parameters);

// MySQL
var id = await conn.ExecuteScalarAsync<long>(insertSql + "; SELECT LAST_INSERT_ID()", q.Parameters);
```

**UPDATE `Set()` order matters for readability, not for SQL correctness** — columns are emitted in `.Set()` call order.

**No multi-table UPDATE or MERGE.** Dialect-specific constructs (`UPDATE … JOIN` in MySQL, `MERGE` in SQL Server) are deferred. For those, use raw parameterized SQL via Dapper directly.

---

## Pitfalls and edge cases

### `Build()` is single-use

```csharp
var qb = Q.New(false).UseTableBoundSelector<User>().Column(u => u.Id).Then();
var sql1 = qb.Build();   // OK
var sql2 = qb.Build();   // throws InvalidOperationException
```

Create a new `QBuilder` for each query execution. QBuilder is cheap to construct.

### Calling `Build()` and `BuildWithParameters()` must match the mode

```csharp
// Wrong — Build() throws when parameterize: true
Q.New().UseTableBoundSelector<User>().Column(u => u.Id).Then().Build();

// Wrong — BuildWithParameters() throws when parameterize: false
Q.New(false).UseTableBoundSelector<User>().Column(u => u.Id).Then().BuildWithParameters();
```

### Global config mutations are process-wide

`QBuilderConfig.ConfigureDefault` and `Configure` write to a static registry. In tests, always call `QBuilderConfig.Reset()` in teardown — failing to do so leaks config state between tests and causes hard-to-diagnose failures.

```csharp
public class MyTests : IDisposable
{
    public void Dispose() => QBuilderConfig.Reset();
}
```

### Unnamed config must be registered before use

`Q.New("reporting")` throws `InvalidOperationException` if the `"reporting"` name has not been registered via `QBuilderConfig.Configure`. Register all named configs at startup, not lazily.

### `[Column]` attributes are ignored

`TableBound*` builders resolve column names from the **C# property name**, not from EF/DataAnnotations `[Column]` attributes. If your model has `[Column("is_active")]` on a property named `IsActive`, the generated SQL uses `IsActive`. Use QBuilder's own `[QColumn("is_active")]` attribute if you need an alias — it is respected by the `FromObject` path. The expression-based fluent path (`Value(u => u.IsActive, ...)`) always uses the C# property name regardless.

### Explicit aliases in self-joins bypass quoting

When you pass `leftAlias: "tEmp"` and `rightAlias: "tMgr"` to a join, those literal strings are used as-is. Quote them yourself when using a quoting dialect:

```csharp
// SQL Server — quote the explicit aliases yourself
.UseTableBoundJoinBuilder<Employee, Employee>()
    .InnerJoin(e => e.ManagerId, m => m.Id, leftAlias: "[tEmp]", rightAlias: "[tMgr]")
```

### `WhereIn` / `WhereNotIn` with null or empty collections silently no-ops

```csharp
// No WHERE clause is emitted — returns all rows
.UseTableBoundFilter<User>().WhereIn<string, string>(u => u.Status, null).Then()
.UseTableBoundFilter<User>().WhereIn<string, string>(u => u.Status, new string[0]).Then()
```

This is intentional — safe to pass optional filter lists. To emit an impossible condition, use `.WhereExplicitly("1=0")` in non-parameterized mode.

### `OpenGroup` / `CloseGroup` must be balanced

```csharp
.UseTableBoundFilter<Order>()
    .OpenGroup()
    .WhereEqualTo(o => o.Status, "new")
    // Missing CloseGroup() → Build() throws "An unclosed parentheses was found"
.Then()
```

### Set operations consume sub-queries eagerly

`Union(other)` calls `other.Build()` immediately. Do not modify `other` after calling a set operation on it.

### Booleans in parameterized mode

Pass C# `bool` values directly — they are stored as-is and ADO.NET converts them to integers for the database. Do not substitute `1`/`0` integers manually (both work, but `true`/`false` is idiomatic).

### `WhereExplicitly(string)` throws in parameterized mode

```csharp
// Throws — raw SQL injection point blocked in parameterized mode
Q.New().UseTableBoundFilter<User>().WhereExplicitly("Status = 'active'").Then()

// Safe alternative — use the two-arg overload
Q.New().UseTableBoundFilter<User>().WhereExplicitly("Status = @status", new { status = "active" }).Then()
```

### Reserved-word table names without a dialect

With `Dialect.None` (the default) identifiers are emitted unquoted. Table names that are SQL reserved words (`Order`, `User`, `Group`, `Index`) will fail at runtime. Solutions in order of preference:

1. **Use a dialect** — `opt.Dialect = Dialect.SqlServer` (or any non-None dialect). All identifiers are quoted automatically.
2. **Custom resolver** — `opt.TableNameResolver = t => t.Name == "Order" ? "[Order]" : t.Name` for targeted quoting when you can't set a global dialect.

---

## Best practices

1. **Configure dialect at startup, not per-query.** Set `QBuilderConfig.ConfigureDefault` in `Program.cs` / `Startup.cs` and forget about it. Every `Q.New()` in the process inherits the dialect.

2. **Use `Dialect.SqlServer` / `MySql` / `Sqlite` etc. rather than `Dialect.None`** in production — reserved-word collisions are eliminated, and SQL is portable to database tools that require quoted identifiers.

3. **Always use parameterized mode (`Q.New()`) with user-supplied values.** The default is safe on purpose.

4. **Define domain models as plain classes.** QBuilder only uses the type name and property names. No attributes, no base class, no EF dependency.

5. **Use named configs for multi-database apps:**
   ```csharp
   QBuilderConfig.Configure("analytics", opt =>
   {
       opt.Dialect = Dialect.Postgres;
       opt.TableNameResolver = t => "analytics." + t.Name;
   });
   var q = Q.New("analytics");
   ```

6. **Use `UsePageBy` instead of explicit paging builders.** It auto-dispatches based on dialect — if you swap the database, the paging SQL updates for free.

7. **Build sub-queries first for EXISTS / CTEs:**
   ```csharp
   var sub = Q.New(false)
       .UseTableBoundSelector<Order>().Column(o => o.Id).Then()
       .UseTableBoundFilter<Order>().WhereEqualTo(o => o.UserId, userId).Then();

   var sql = Q.New(false)
       .UseTableBoundSelector<User>().Column(u => u.Id).Then()
       .UseTableBoundFilter<User>().WhereExists(sub).Then()
       .Build();
   ```

8. **Use `.If()` for optional filters** instead of conditional branching in calling code:
   ```csharp
   .UseTableBoundFilter<User>()
       .WhereEqualTo(u => u.IsActive, true)
       .If(hasNameFilter, fb => fb.AndWhereContains(u => u.Name, nameFilter))
       .If(hasRoleFilter, fb => fb.AndWhereEqualTo(u => u.Role, roleFilter))
   .Then()
   ```

9. **Log the SQL during development:**
   ```csharp
   var built = qb.BuildWithParameters(sql => Console.WriteLine(sql));
   ```

10. **Do not reuse a `QBuilder` instance** — create a new one per query.

11. **Inspect SQL in non-parameterized mode first,** then switch to `Q.New()` for production.

12. **Call `QBuilderConfig.Reset()` in test teardown** to avoid config bleed between tests.

---

## Migration from v7 to v8

v8 removed `QBuilderExtensions` (the flat extension-method API). See [`docs/migration-v7-to-v8.md`](docs/migration-v7-to-v8.md) for the complete method-by-method mapping.

Quick summary:

```csharp
// v7 (removed)
Q.New(false)
    .Select<User, string>(u => u.Name)
    .Where<User, bool>(u => u.IsActive, FilterOperator.EqualTo, true)
    .OrderBy<User, string>(u => u.Name)
    .Build();

// v8+
Q.New(false)
    .UseTableBoundSelector<User>().Column(u => u.Name).Then()
    .UseTableBoundFilter<User>().WhereEqualTo(u => u.IsActive, true).Then()
    .UseTableBoundOrderBy<User>().Ascending(u => u.Name).Then()
    .Build();
```

---

## Migration from Rocket.Libraries.QBuilder

Jattac.Libraries.QBuilder is the renamed successor to `Rocket.Libraries.QBuilder`.

**Step 1 — replace the package reference**

```xml
<!-- Before -->
<PackageReference Include="Rocket.Libraries.QBuilder" Version="*" />

<!-- After -->
<PackageReference Include="Jattac.Libraries.QBuilder" Version="9.2.0" />
```

**Step 2 — update namespace imports**

```csharp
// Before
using Rocket.Libraries.Qurious;

// After
using Jattac.Libraries.QBuilder;
```

Then migrate the call sites using the v8 `TableBound*` API — see [`docs/migration-v7-to-v8.md`](docs/migration-v7-to-v8.md).

---

## Gaps and roadmap

- **Window functions** — LAG, LEAD, RANK, DENSE_RANK, ROW_NUMBER OVER (PARTITION BY), SUM OVER are planned. The ROW_NUMBER paging infrastructure already exists; the full window-function surface will be layered on top.
- **Multi-table UPDATE / MERGE** — `UPDATE t1 JOIN t2` (MySQL) and `MERGE` (SQL Server) are not currently supported. Use raw parameterized SQL for these.
- **DDL** — CREATE TABLE, ALTER TABLE, CREATE INDEX are not in scope. QBuilder is a query builder, not a schema migration tool.
- **Correlated subqueries in SELECT list** — scalar subqueries in the SELECT column list (e.g. `(SELECT COUNT(*) FROM …)`) are not yet supported. Use a JOIN or CTE instead.

---

## License

MIT — see [LICENSE](LICENSE).
