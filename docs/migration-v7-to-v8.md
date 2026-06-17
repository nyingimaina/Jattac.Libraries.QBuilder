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
