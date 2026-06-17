# Migration Guide: v7 → v8

v8.0.0 is a **hard breaking-change release**. `QBuilderExtensions` is deleted and `WhereConjunctionBuilder` is made `internal`. Every call-site that used the old extension-method API must be migrated to the `TableBound*` builder pattern.

---

## Why the break?

`QBuilderExtensions` used `QBuilder` (non-generic) as the receiver. Because `QBuilder` carries no type parameter, the C# compiler could not infer `TTable` from a lambda like `qb.Where<User, string>(u => u.Name, ...)`. Callers had to either annotate the lambda parameter or provide two explicit type arguments — "plain LINQ" friction instead of fluent SQL.

`TableBound*` builders bind `TTable` on the class once (via `UseTableBoundFilter<User>()`) so every subsequent method only needs `TField`, which is always inferred from the lambda. Zero explicit type arguments.

---

## General migration pattern

```csharp
// v7
qb.Select<User, string>(u => u.Name);
qb.Where<User, string>(u => u.Name, FilterOperator.EqualTo, "Alice");
qb.Build();

// v8
Q.New(parameterize: false)
    .UseTableBoundSelector<User>().Column(u => u.Name).Then()
    .UseTableBoundFilter<User>().WhereEqualTo(u => u.Name, "Alice")
    .Then().Build();
```

All clause entry points (`.UseTableBoundSelector<T>()`, `.UseTableBoundFilter<T>()`, etc.) return a typed builder. `.Then()` exits back to `QBuilder` for the next clause or the terminal `.Build()` / `.BuildWithParameters()` call.

---

## Method-by-method mapping

### SELECT

| v7 (removed) | v8 equivalent |
|---|---|
| `qb.Select<User, string>(u => u.Name)` | `.UseTableBoundSelector<User>().Column(u => u.Name).Then()` |
| `qb.Select<User, string>(u => u.Name, "Alias")` | `.UseTableBoundSelector<User>().Column(u => u.Name, "Alias").Then()` |
| `qb.SelectDistinct<User, string>(u => u.Name)` | `.UseTableBoundSelector<User>().Distinct().Column(u => u.Name).Then()` |
| `qb.SelectTop<User, string>(10, u => u.Name)` | `.UseTableBoundSelector<User>().Top(10).Column(u => u.Name).Then()` |
| `qb.SelectAggregated<Order, decimal>(o => o.Amount, "Total", AggregateFunction.Sum)` | `.UseTableBoundSelector<Order>().Aggregate(o => o.Amount, "Total", AggregateFunction.Sum).Then()` |
| `qb.SelectCaseWhen(caseExpr, "Label")` | `.UseTableBoundSelector<Order>().CaseWhen(caseExpr, "Label").Then()` |

### JOIN

| v7 (removed) | v8 equivalent |
|---|---|
| `qb.InnerJoin<User, Order, Guid, Guid>(u => u.Id, o => o.UserId)` | `.UseTableBoundJoinBuilder<User, Order>().InnerJoin(u => u.Id, o => o.UserId)` |
| `qb.LeftJoin<User, Order, Guid, Guid>(u => u.Id, o => o.UserId)` | `.UseTableBoundJoinBuilder<User, Order>().LeftJoin(u => u.Id, o => o.UserId)` |
| `qb.CrossJoin<User, Product>()` | `.UseJoiner().CrossJoin<User, Product>().Then()` |

Note: `InnerJoin` and `LeftJoin` return `QBuilder` directly (no `.Then()` needed).

### WHERE — basic predicates

| v7 (removed) | v8 equivalent |
|---|---|
| `qb.Where<User, string>(u => u.Name, FilterOperator.EqualTo, "x")` | `.UseTableBoundFilter<User>().WhereEqualTo(u => u.Name, "x").Then()` |
| `qb.Where<User, string>(u => u.Name, FilterOperator.NotEqualTo, "x")` | `.UseTableBoundFilter<User>().WhereNotEqualTo(u => u.Name, "x").Then()` |
| `qb.Where<User, int>(u => u.Age, FilterOperator.GreaterThan, 18)` | `.UseTableBoundFilter<User>().WhereGreaterThan(u => u.Age, 18).Then()` |
| `qb.Where<User, int>(u => u.Age, FilterOperator.GreaterThanOrEqualTo, 18)` | `.UseTableBoundFilter<User>().WhereGreaterThanOrEqualTo(u => u.Age, 18).Then()` |
| `qb.Where<User, int>(u => u.Age, FilterOperator.LessThan, 65)` | `.UseTableBoundFilter<User>().WhereLessThan(u => u.Age, 65).Then()` |
| `qb.Where<User, int>(u => u.Age, FilterOperator.LessThanOrEqualTo, 65)` | `.UseTableBoundFilter<User>().WhereLessThanOrEqualTo(u => u.Age, 65).Then()` |
| `qb.Where<User, string>(u => u.Name, FilterOperator.Contains, "lic")` | `.UseTableBoundFilter<User>().WhereContains(u => u.Name, "lic").Then()` |
| `qb.Where<User, string>(u => u.Name, FilterOperator.StartsWith, "Al")` | `.UseTableBoundFilter<User>().WhereStartsWith(u => u.Name, "Al").Then()` |
| `qb.Where<User, string>(u => u.Name, FilterOperator.EndsWith, "ce")` | `.UseTableBoundFilter<User>().WhereEndsWith(u => u.Name, "ce").Then()` |
| `qb.WhereIsNull<User, Guid>(u => u.DeletedAt)` | `.UseTableBoundFilter<User>().WhereIsNull(u => u.DeletedAt).Then()` |
| `qb.WhereIsNotNull<User, Guid>(u => u.DeletedAt)` | `.UseTableBoundFilter<User>().WhereIsNotNull(u => u.DeletedAt).Then()` |
| `qb.WhereIn<User, Guid, Guid>(u => u.Id, ids)` | `.UseTableBoundFilter<User>().WhereIn<Guid, Guid>(u => u.Id, ids).Then()` |
| `qb.WhereNotIn<User, Guid, Guid>(u => u.Id, ids)` | `.UseTableBoundFilter<User>().WhereNotIn<Guid, Guid>(u => u.Id, ids).Then()` |
| `qb.WhereBetween<User, DateTime>(u => u.CreatedAt, from, to)` | `.UseTableBoundFilter<User>().WhereBetween(u => u.CreatedAt, from, to).Then()` |
| `qb.WhereNotBetween<User, DateTime>(u => u.CreatedAt, from, to)` | `.UseTableBoundFilter<User>().WhereNotBetween(u => u.CreatedAt, from, to).Then()` |
| `qb.WhereExists(subQuery)` | `.UseTableBoundFilter<User>().WhereExists(subQuery).Then()` |
| `qb.WhereNotExists(subQuery)` | `.UseTableBoundFilter<User>().WhereNotExists(subQuery).Then()` |

### WHERE — AND continuation

In v7, `AndWhere` was an extension on `QBuilder`, requiring explicit type arguments each time. In v8, AND continuation is a method on the same `TableBoundWhereBuilder<T>` instance — no new `UseTableBoundFilter` call needed:

```csharp
// v7
qb.Where<User, string>(u => u.Name, FilterOperator.EqualTo, "Alice");
qb.AndWhere<User, bool>(u => u.IsActive, FilterOperator.EqualTo, true);

// v8
.UseTableBoundFilter<User>()
    .WhereEqualTo(u => u.Name, "Alice")
    .AndWhereEqualTo(u => u.IsActive, true)
.Then()
```

Full `And*` family: `AndWhereEqualTo`, `AndWhereNotEqualTo`, `AndWhereLessThan`, `AndWhereLessThanOrEqualTo`, `AndWhereGreaterThan`, `AndWhereGreaterThanOrEqualTo`, `AndWhereContains`, `AndWhereStartsWith`, `AndWhereEndsWith`, `AndWhereIsNull`, `AndWhereIsNotNull`, `AndWhereIn`, `AndWhereNotIn`, `AndWhereBetween`, `AndWhereNotBetween`, `AndWhereExists`, `AndWhereNotExists`.

### WHERE — OR continuation (new in v8)

v7 had no `OrWhere` family at all. v8 adds a complete `Or*` mirror:

```csharp
.UseTableBoundFilter<User>()
    .WhereEqualTo(u => u.Name, "Alice")
    .OrWhereEqualTo(u => u.Name, "Bob")
.Then()
```

Full `Or*` family: `OrWhereEqualTo`, `OrWhereNotEqualTo`, `OrWhereLessThan`, `OrWhereLessThanOrEqualTo`, `OrWhereGreaterThan`, `OrWhereGreaterThanOrEqualTo`, `OrWhereContains`, `OrWhereStartsWith`, `OrWhereEndsWith`, `OrWhereIsNull`, `OrWhereIsNotNull`, `OrWhereIn`, `OrWhereNotIn`, `OrWhereBetween`, `OrWhereNotBetween`, `OrWhereExists`, `OrWhereNotExists`.

### WHERE — grouping

| v7 | v8 |
|---|---|
| `qb.OpenParentheses()` | `.UseTableBoundFilter<T>().OpenGroup()` (then chain more predicates) |
| `qb.CloseParentheses()` | `.CloseGroup()` (on the same builder) |

```csharp
// v8 example
.UseTableBoundFilter<Order>()
    .WhereEqualTo(o => o.Status, "new")
    .OpenGroup()
        .OrWhereEqualTo(o => o.Status, "processing")
    .CloseGroup()
.Then()
```

### WHERE — conditional (new in v8)

```csharp
.UseTableBoundFilter<User>()
    .If(!string.IsNullOrEmpty(nameFilter),
        fb => fb.WhereEqualTo(u => u.Name, nameFilter))
.Then()
```

### WHERE — explicit SQL

In v7, `WhereExplicitly(string)` threw in parameterized mode. In v8 it still throws in parameterized mode (pass an explicit raw fragment only in non-parameterized queries):

```csharp
// non-parameterized only
.UseTableBoundFilter<T>().WhereExplicitly("1=1").Then()
```

### HAVING

| v7 (removed) | v8 equivalent |
|---|---|
| `qb.Having<Order, decimal>(o => o.Amount, FilterOperator.GreaterThan, 100)` | `.UseTableBoundHaving<Order>().HavingGreaterThan(o => o.Amount, 100).Then()` |

`UseTableBoundHaving<T>()` exposes the same predicate surface as `UseTableBoundFilter<T>()` but writes to the `HAVING` clause. All `Having*`, `AndHaving*`, and `OrHaving*` variants are available.

### GROUP BY

| v7 (removed) | v8 equivalent |
|---|---|
| `qb.GroupBy<Order, Guid>(o => o.UserId)` | `.UseTableBoundGrouper<Order>().GroupBy(o => o.UserId)` |

`GroupBy` returns `QBuilder` directly (no `.Then()` needed).

### ORDER BY

| v7 (removed) | v8 equivalent |
|---|---|
| `qb.OrderBy<User, string>(u => u.Name)` (ascending) | `.UseTableBoundOrderBy<User>().Ascending(u => u.Name).Then()` |
| `qb.OrderByDescending<User, string>(u => u.Name)` | `.UseTableBoundOrderBy<User>().Descending(u => u.Name).Then()` |
| `qb.ThenBy<User, Guid>(u => u.Id)` | `.ThenAscending(u => u.Id)` (on the same builder) |

### PAGING

| v7 (removed) | v8 equivalent |
|---|---|
| `qb.PageSqlServer<User, string>(u => u.Name, page, pageSize)` | `.UseSqlServerPagingBuilder<User>().PageBy(u => u.Name, page, pageSize).Build()` |
| `qb.PageOffsetFetch<User, string>(u => u.Name, page, pageSize)` | `.UseOffsetFetchPagingBuilder<User>().PageBy(u => u.Name, page, pageSize).Build()` |

### PARAMETERIZED BUILD

| v7 | v8 |
|---|---|
| `qb.BuildWithParameters()` | `.BuildWithParameters()` — unchanged |
| _(not available)_ | `.BuildWithParameters(sql => Console.WriteLine(sql))` — optional log hook |

---

## WhereConjunctionBuilder is now internal

Any code that stored a `WhereConjunctionBuilder` reference in a variable must be updated. Switch to chaining directly on `TableBoundWhereBuilder<T>`:

```csharp
// v7 — stored conjunction builder
WhereConjunctionBuilder conj = qb.UseFilter().WhereEqualTo(...);
conj.And().WhereEqualTo(...);

// v8 — chain on the table-bound builder
qb.UseTableBoundFilter<User>()
    .WhereEqualTo(...)
    .AndWhereEqualTo(...)
    .Then();
```

---

## bool parameters in parameterized mode (bug fix)

Prior to v8, passing a C# `bool` to any predicate in parameterized mode stored `"True"` or `"False"` as a string parameter. This caused comparison failures against INTEGER columns (e.g. SQLite's `IsActive INTEGER`).

v8 fixes `ConditionMaker` to store the raw value and let Dapper/ADO.NET handle type conversion. If your existing code worked around this by passing `1`/`0` instead of `true`/`false`, both approaches now work correctly.

---

## FieldNameResolver and [Column] attribute

`TableBound*` builders resolve column names from the C# property name, **not** from a `[Column]` data annotation. If your model uses `[Column("some_column")]` but the property is named `SomeColumn`, the generated SQL uses `tTable.SomeColumn`. Apply a custom table/column name resolver via `Q.New(t => ...)` if you need a different mapping strategy.

---

## Picking a dialect (new in v8)

Prior versions of this library produced unquoted, plain SQL identifiers with no way to configure quoting or paging strategy. v8 introduces a `Dialect` enum and a startup-time `QBuilderConfig` API so you can match the library's output to your target database once, globally, rather than manually quoting or hand-rolling paging in every query.

### What a dialect controls

| Concern | Effect |
|---|---|
| **Identifier quoting** | Wraps table names, aliases, and column names in the delimiter your database expects. Protects reserved-word clashes (e.g. `Order`, `User`). |
| **`UsePageBy` strategy** | Dispatches to the correct paging syntax for the dialect — ROW_NUMBER() for SQL Server, LIMIT for MySQL/MariaDB, OFFSET/FETCH for everything else. |

### Dialect reference

| `Dialect` value | Quote style | Paging |
|---|---|---|
| `None` _(default)_ | no quoting | OFFSET / FETCH |
| `SqlServer` / `MsSql` | `[brackets]` | ROW_NUMBER() |
| `MySql` | `` `backticks` `` | LIMIT |
| `MariaDb` | `` `backticks` `` | LIMIT |
| `Sqlite` | `"double quotes"` | OFFSET / FETCH |
| `Postgres` | `"double quotes"` | OFFSET / FETCH |
| `Generic` | `"double quotes"` | OFFSET / FETCH |

`Dialect.None` is the backward-compatible default — it produces the same unquoted SQL that older versions always generated, so existing call sites keep working without any change.

### Registering a dialect at startup

```csharp
// Program.cs / Startup.cs — run once before any Q.New() call
QBuilderConfig.ConfigureDefault(opt =>
{
    opt.Dialect = Dialect.SqlServer;
});
```

After this, every `Q.New()` / `Q.New(parameterize: false)` call uses SQL Server quoting and paging automatically. No per-query change needed.

### Multiple databases in one application

If your application talks to more than one database engine, register a named configuration for each:

```csharp
QBuilderConfig.ConfigureDefault(opt => opt.Dialect = Dialect.SqlServer); // primary DB
QBuilderConfig.Configure("reporting", opt => opt.Dialect = Dialect.Postgres); // read replica

// Usage
var q = Q.New();               // SqlServer
var q = Q.New("reporting");    // Postgres
```

### Per-query override (without global config)

For one-off cases you can pass a `QBuilderOptions` instance directly to `Q.New`:

```csharp
var sql = Q.New(new QBuilderOptions { Dialect = Dialect.MySql }, parameterize: false)
    .UseTableBoundSelector<User>()
    .Column(u => u.Name)
    .Then()
    .Build();
// → Select `tUser`.`Name` From `User` `tUser`
```

### Which dialect should I pick?

- **SQL Server / Azure SQL** → `Dialect.SqlServer` (or the alias `Dialect.MsSql`)
- **MySQL** → `Dialect.MySql`
- **MariaDB** → `Dialect.MariaDb`
- **SQLite** → `Dialect.Sqlite`
- **PostgreSQL** → `Dialect.Postgres`
- **Other ANSI-compliant DB** → `Dialect.Generic`
- **Legacy behaviour / no quoting needed** → `Dialect.None` (the default; no `QBuilderConfig` call required)

---

## POCO shortcuts (new in v9.2)

Prior versions required one explicit expression-based call per column when performing DML:

```csharp
// Before v9.2 — required for every property
Q.New()
    .UseTableBoundInsert<User>()
    .Value(u => u.Id, user.Id)
    .Value(u => u.Name, user.Name)
    .Value(u => u.IsActive, user.IsActive)
    .BuildWithParameters();
```

v9.2 adds `FromObject<T>(T obj)` to all three DML builders. Pass the entire object and the
library reflects its properties automatically:

```csharp
// v9.2
Q.New()
    .UseTableBoundInsert<User>()
    .FromObject(user)
    .BuildWithParameters();
```

The fluent API still works and is not deprecated.

### New attributes

Decorate your POCO properties to control how `FromObject` handles them:

| Attribute | Effect |
|---|---|
| `[QKey]` | Marks the primary key. UPDATE and DELETE route this property to the WHERE clause; INSERT treats it as a normal column. |
| `[QIgnore]` | Skip this property in all `FromObject` operations (INSERT, SET, WHERE). |
| `[QColumn("name")]` | Override the SQL column name. Only affects the `FromObject` path; fluent methods always use the C# property name. |

```csharp
using Jattac.Libraries.QBuilder.Attributes;

public class User
{
    [QKey]
    public Guid Id { get; set; }          // → WHERE clause in UPDATE/DELETE

    [QColumn("user_name")]
    public string Name { get; set; }      // → SQL column is "user_name"

    [QIgnore]
    public bool IsInternalFlag { get; set; } // → never appears in generated SQL
}
```

### INSERT

All non-`[QIgnore]` properties are included. `[QKey]` properties are inserted as normal values.

```csharp
Q.New()
    .UseTableBoundInsert<User>()
    .FromObject(user)
    .BuildWithParameters();
// → Insert Into User (Id, user_name) Values (@Id0, @Name0)
```

### UPDATE

`[QKey]` properties go to WHERE; all other non-ignored properties go to SET.

```csharp
Q.New()
    .UseTableBoundUpdate<User>()
    .FromObject(user)
    .BuildWithParameters();
// → Update User Set user_name = @Name0 Where User.Id = @Id0
```

### DELETE

Only `[QKey]` properties are used (for the WHERE clause). All other properties are ignored.

```csharp
Q.New()
    .UseTableBoundDelete<User>()
    .FromObject(user)
    .BuildWithParameters();
// → Delete From User Where User.Id = @Id0
```

### Anonymous objects

Anonymous objects work with `FromObject`. They cannot have attributes, so all properties are
treated as regular values. For UPDATE and DELETE, chain the WHERE clause manually:

```csharp
// INSERT — all properties become column values
Q.New()
    .UseTableBoundInsert<User>()
    .FromObject(new { Id = Guid.NewGuid(), Name = "Alice" })
    .BuildWithParameters();

// UPDATE with anonymous object — add WHERE manually
Q.New()
    .UseTableBoundUpdate<User>()
    .FromObject(new { Name = "Alice" })     // SET clause only
    .WhereEqualTo(u => u.Id, userId)        // WHERE chained manually
    .BuildWithParameters();
```

### Shorthand methods on QBuilder

```csharp
// Equivalent to UseTableBoundInsert<User>().FromObject(user)
Q.New().InsertFrom<User, User>(user).BuildWithParameters();
Q.New().UpdateFrom<User, User>(user).BuildWithParameters();
Q.New().DeleteFrom<User, User>(user).BuildWithParameters();
```

### Limitation: database-generated keys

If your primary key is auto-generated by the database (e.g., SQL Server `IDENTITY`, PostgreSQL
`SERIAL`), do not use `[QKey]` — use `[QIgnore]` to skip the key from INSERT and chain WHERE
manually for UPDATE and DELETE:

```csharp
public class Product
{
    [QIgnore]                             // auto-generated; skip from INSERT
    public int Id { get; set; }
    public string Name { get; set; }
}

// INSERT without Id
Q.New()
    .UseTableBoundInsert<Product>()
    .FromObject(product)
    .BuildWithParameters();

// UPDATE with explicit WHERE
Q.New()
    .UseTableBoundUpdate<Product>()
    .FromObject(product)
    .WhereEqualTo(p => p.Id, product.Id)  // WHERE added manually
    .BuildWithParameters();
```

### Bulk INSERT — `FromObjects` (new in v9.2)

Insert multiple rows in a single SQL statement:

```csharp
var users = new List<User> { ... };

Q.New()
    .UseTableBoundInsert<User>()
    .FromObjects(users)
    .BuildWithParameters();
// → Insert Into User (Id, Name) Values (@Id0, @Name0), (@Id1, @Name1), ...
```

**SQL Server parameter limit:** SQL Server allows a maximum of 2,100 parameters per query.
For wide tables or large collections, chunk the list before calling `FromObjects`:

```csharp
foreach (var chunk in users.Chunk(200))   // .NET 6+ has Enumerable.Chunk
{
    var q = Q.New().UseTableBoundInsert<User>().FromObjects(chunk).BuildWithParameters();
    connection.Execute(q.ParameterizedSql, q.Parameters);
}
```

### Batched DML — `QBatch` (new in v9.2)

Combine multiple DML statements into a single round-trip:

```csharp
var result = QBatch.New()
    .AddRange(users.Select(u =>
        Q.New().UseTableBoundUpdate<User>().FromObject(u).BuildWithParameters()))
    .Build();

connection.Execute(result.ParameterizedSql, result.Parameters);
```

`QBatch` automatically renames any parameter name that collides across statements.
It does NOT wrap statements in a transaction — use an ADO.NET transaction if needed.

### Bulk DELETE — `WhereIn` (already available, recommended)

For deleting multiple rows by a set of IDs, use the existing `WhereIn`:

```csharp
Q.New()
    .UseTableBoundDelete<User>()
    .WhereIn<Guid, Guid>(u => u.Id, idsToDelete)
    .BuildWithParameters();
// → Delete From User Where User.Id In (@Id0, @Id1, @Id2)
```
