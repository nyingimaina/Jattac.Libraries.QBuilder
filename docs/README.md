# Jattac.Libraries.QBuilder — API Reference

A fluent SQL query builder for .NET 6+. Produces raw SQL strings or parameterized queries; it never executes them. Works with any database driver.

---

## Table of Contents

1. [Getting Started](#1-getting-started)
2. [SELECT](#2-select)
3. [JOIN](#3-join)
4. [WHERE](#4-where)
5. [GROUP BY / HAVING](#5-group-by--having)
6. [ORDER BY](#6-order-by)
7. [Paging](#7-paging)
8. [CTEs](#8-ctes)
9. [Set Operations](#9-set-operations)
10. [Parameterization Deep-Dive](#10-parameterization-deep-dive)
11. [Self-Joins](#11-self-joins)
12. [Known Limitations](#12-known-limitations)

---

## 1. Getting Started

### Installation

```
dotnet add package Jattac.Libraries.QBuilder
```

### Namespaces

```csharp
using Jattac.Libraries.QBuilder;
using Jattac.Libraries.QBuilder.Enums;
```

### Two modes: literal vs parameterized

| Mode | Entry point | Terminator | Returns |
|---|---|---|---|
| Literal | `Q.New(parameterize: false)` | `.Build()` | `string` |
| Parameterized | `Q.New()` (default) | `.BuildWithParameters()` | `BuiltQuery` |

`BuiltQuery` contains:
- `string Sql` — the SQL string with `@p_Name` placeholders
- `Dictionary<string, object> Parameters` — the bound values

**Use parameterized mode whenever values come from user input.** Literal mode is for values you fully control (constants, enums, known IDs).

```csharp
// Literal
string sql = Q.New(parameterize: false)
    .Select((User u) => u.Name)
    .Build();

// Parameterized
BuiltQuery result = Q.New()
    .Select((User u) => u.Name)
    .Where((User u) => u.Age, FilterOperator.GreaterThan, 18)
    .BuildWithParameters();
// result.Sql        → "Select tUser.Name From User tUser\nWhere tUser.Age > @p_Age\n"
// result.Parameters → { "@p_Age": 18 }
```

### Custom table name resolver

By default the table name is the CLR type name (`typeof(User).Name` → `"User"`). Override globally with a delegate:

```csharp
// Pluralise + schema-prefix
Q.New(t => $"dbo.{t.Name}s")
    .Select((User u) => u.Id)
    .Build();
// → "Select tUser.Id From dbo.Users tUser"
```

### One-time use

A builder cannot be reused after calling `Build()` or `BuildWithParameters()`. Create a new `Q.New()` for each query.

---

## 2. SELECT

### Type inference — recommended style

C# infers both `TTable` and `TField` from a typed lambda parameter. You never need to write explicit type arguments:

```csharp
// Recommended — types inferred from (User u)
Q.New().Select((User u) => u.Id).Build();

// Equivalent — explicit types, more verbose
Q.New().Select<User, Guid>(u => u.Id).Build();
```

### Plain field — no column alias

```csharp
Q.New()
    .Select((User u) => u.Id)
    .Select((User u) => u.Name)
    .Build();
// → "Select\ntUser.Id,\ntUser.Name From User tUser"
```

### Plain field — with column alias

```csharp
Q.New()
    .Select((User u) => u.Name, "UserName")
    .Build();
// → "Select\ntUser.Name as UserName From User tUser"
```

### Aggregate functions

```csharp
Q.New()
    .Aggregate((Order o) => o.Total, "TotalRevenue", AggregateFunction.Sum)
    .Aggregate((Order o) => o.Id,    "OrderCount",   AggregateFunction.Count)
    .Build();
```

| `AggregateFunction` value | Emitted SQL fragment |
|---|---|
| `Count` | `Count(tOrder.Id)` |
| `CountDistinct` | `Count(Distinct tOrder.Id)` |
| `Sum` | `Sum(tOrder.Total)` |
| `Avg` | `Avg(tOrder.Total)` |
| `Min` | `Min(tOrder.Total)` |
| `Max` | `Max(tOrder.Total)` |

### TOP — SQL Server only

```csharp
Q.New()
    .Top(10)
    .Select((User u) => u.Name)
    .Build();
// → "Select  Top 10  \ntUser.Name From User tUser"
```

**Pitfall:** `Top()` emits SQL Server `SELECT TOP n` syntax — not portable. Use `PageOffsetFetch` or `PageMySql` for cross-database row limiting.

### DISTINCT

```csharp
Q.New()
    .Distinct()
    .Select((User u) => u.CountryCode)
    .Build();
```

`Distinct()` applies to the entire result set, not to individual columns.

### CASE WHEN

```csharp
var statusExpr = new CaseWhenBuilder()
    .When("tOrder.Status = 1").Then("Active")
    .When("tOrder.Status = 2").Then("Closed")
    .Else("Unknown");

Q.New()
    .Select((Order o) => o.Id)
    .SelectCaseWhen(statusExpr, "StatusLabel")
    .Build();
```

**Limitation:** `Then()` and `Else()` values are always emitted as single-quoted strings. Numeric THEN values are not currently supported.

---

## 3. JOIN

### Type inference on joins

All four type parameters (`TLeft`, `TRight`, `TLeftField`, `TRightField`) are inferred from typed lambda parameters:

```csharp
// Inferred — recommended
Q.New()
    .Select((User u) => u.Name)
    .Select((Order o) => o.Total)
    .InnerJoin((User u) => u.Id, (Order o) => o.UserId)
    .Build();

// Explicit — same result
Q.New()
    .InnerJoin<User, Order, Guid, Guid>(u => u.Id, o => o.UserId)
    .Build();
```

### All join types

```csharp
// INNER JOIN — most common
.InnerJoin((User u) => u.Id, (Order o) => o.UserId)

// LEFT JOIN
.LeftJoin((User u) => u.Id, (Order o) => o.UserId)

// RIGHT JOIN
.RightJoin((User u) => u.Id, (Order o) => o.UserId)

// FULL OUTER JOIN — ⚠ not supported in MySQL
.FullOuterJoin((User u) => u.Id, (Order o) => o.UserId)

// CROSS JOIN — no ON clause emitted
.CrossJoin<User, Country>()
```

**Emitted SQL (INNER JOIN example):**
```sql
Select
tUser.Name,
tOrder.Total From User tUser
join Order tOrder on tUser.Id = tOrder.UserId
```

### DB compatibility

| Join type | SQL Server | PostgreSQL | MySQL / MariaDB |
|---|---|---|---|
| INNER | ✓ | ✓ | ✓ |
| LEFT | ✓ | ✓ | ✓ |
| RIGHT | ✓ | ✓ | ✓ |
| FULL OUTER | ✓ | ✓ | ✗ |
| CROSS | ✓ | ✓ | ✓ |

### Self-joins

See [Section 11 — Self-Joins](#11-self-joins).

### Derived table joins

Use `JoinBuilder.UseDerivedTableJoiner<TOuterTable>()` to join a subquery as a derived table. The inner query must be fully built (call `.Build()` on it) before passing it to the joiner.

### Pitfall: mismatched join key types

The compiler accepts `InnerJoin((User u) => u.Id, (Order o) => o.UserId)` even when the two fields are different CLR types. This compiles but may produce wrong SQL at runtime. Verify that join key types match.

### Pitfall: no composite join key in one call

`JOIN A ON A.Key1 = B.Key1 AND A.Key2 = B.Key2` cannot be expressed in a single join call. Workaround: add the second condition as a WHERE predicate.

### Pitfall: no OR between join conditions

Each join emits exactly one equality `ON` clause. You cannot express `ON (A.Id = B.AId OR A.Id = B.BId)` directly.

---

## 4. WHERE

### First predicate vs conjunctions

```csharp
// First condition — use Where
.Where((User u) => u.Active, FilterOperator.EqualTo, true)

// Additional AND condition
.AndWhere((User u) => u.Age, FilterOperator.GreaterThan, 18)

// Additional OR condition
.OrWhere((User u) => u.Role, FilterOperator.EqualTo, "admin")
```

**Best practice:** Always use `AndWhere`/`OrWhere` for predicates after the first. Calling `Where` a second time is internally treated as AND, but the intent is unclear to readers.

### All FilterOperator values

| `FilterOperator` | Emitted SQL |
|---|---|
| `EqualTo` | `= value` |
| `NotEqualTo` | `<> value` |
| `GreaterThan` | `> value` |
| `GreaterThanOrEqualTo` | `>= value` |
| `LessThan` | `< value` |
| `LessThanOrEqualTo` | `<= value` |
| `Like` | `Like 'value'` |
| `NotLike` | `Not Like 'value'` |
| `IsNull` | `Is Null` |
| `IsNotNull` | `Is Not Null` |

### NULL checks

```csharp
.WhereIsNull((User u) => u.DeletedAt)
.AndWhereIsNull((User u) => u.DeletedAt)

.WhereIsNotNull((User u) => u.DeletedAt)
.AndWhereIsNotNull((User u) => u.DeletedAt)
```

### BETWEEN / NOT BETWEEN

```csharp
// Inclusive bounds
.WhereBetween((Order o) => o.Total, 100.0m, 500.0m)
.AndWhereBetween((Order o) => o.CreatedAt, startDate, endDate)
```

**Pitfall:** Both bounds are mandatory. The `from` value must be ≤ `to` in most databases — reversing them returns zero rows, not the full set.

### IN / NOT IN

```csharp
var ids = new[] { 1, 2, 3 };

.WhereIn((User u) => u.Id, ids)
.AndWhereIn((User u) => u.Id, ids)

.WhereNotIn((User u) => u.StatusCode, new[] { "X", "D" })
.AndWhereNotIn((User u) => u.StatusCode, new[] { "X", "D" })
```

**Pitfall: silent no-op on empty or null list.** If `ids` is empty or `null`, the entire predicate is silently skipped — no WHERE clause is emitted for that call. An empty `WhereIn` does **not** produce `WHERE 1=0`. If you need match-nothing semantics, check the list length yourself before building.

In parameterized mode, each value gets its own parameter (`@p_Id`, `@p_Id_1`, `@p_Id_2`, …). Very large lists create many parameters — SQL Server caps the total at 2100 per query.

### EXISTS / NOT EXISTS (subquery)

```csharp
var subQuery = Q.New(parameterize: false)
    .Select((Order o) => o.Id)
    .Where((Order o) => o.UserId, FilterOperator.EqualTo, "tUser.Id")
    .Build();

.WhereExists(subQuery)
.AndWhereExists(subQuery)
.WhereNotExists(subQuery)
.AndWhereNotExists(subQuery)
```

### Parenthesis groups

```csharp
Q.New()
    .Where((User u) => u.Active, FilterOperator.EqualTo, true)
    .OpenGroup()
        .AndWhere((User u) => u.Role, FilterOperator.EqualTo, "admin")
        .OrWhere((User u) => u.Role, FilterOperator.EqualTo, "superuser")
    .CloseGroup()
    .Build();
// → "Where tUser.Active = @p_Active\n And  (tUser.Role = @p_Role\n Or tUser.Role = @p_Role_1\n) "
```

Every `OpenGroup()` must be paired with `CloseGroup()`. The builder throws at `Build()` time if any group is left open. Nested groups are supported.

### WhereExplicitly — raw SQL fragment

```csharp
Q.New(parameterize: false)
    .Select((User u) => u.Id)
    .WhereExplicitly("tUser.Age > 18 And tUser.CountryCode = 'KE'")
    .Build();
```

**Only available in literal mode.** Throws `InvalidOperationException` in parameterized mode. Never pass user-controlled values to this method.

### tableAlias override

```csharp
.Where((Employee e) => e.DeptId, FilterOperator.EqualTo, 5, tableAlias: "emp")
```

See [Section 11](#11-self-joins) for the full self-join pattern.

---

## 5. GROUP BY / HAVING

### GROUP BY

```csharp
Q.New()
    .Select((Order o) => o.UserId)
    .Aggregate((Order o) => o.Total, "TotalSpend", AggregateFunction.Sum)
    .GroupBy((Order o) => o.UserId)
    .Build();
// → "Select\ntOrder.UserId,\nSum(tOrder.Total) as TotalSpend From Order tOrder\n Group By tOrder.UserId"
```

Multiple grouping columns:

```csharp
.GroupBy((Order o) => o.UserId)
.GroupBy((Order o) => o.StatusCode)
```

String-name overload:

```csharp
qb.UseGrouper().GroupBy<Order>("UserId");
```

**Best practice:** Every non-aggregated SELECT column must appear in GROUP BY. PostgreSQL and SQL Server enforce this strictly; MySQL allows it but produces non-deterministic values for omitted columns.

### HAVING

```csharp
Q.New()
    .Select((Order o) => o.UserId)
    .Aggregate((Order o) => o.Total, "TotalSpend", AggregateFunction.Sum)
    .GroupBy((Order o) => o.UserId)
    .Having((Order o) => o.Total, FilterOperator.GreaterThan, 1000m)
    .AndHaving((Order o) => o.Total, FilterOperator.LessThan, 50000m)
    .Build();
```

HAVING supports all `FilterOperator` values and the `tableAlias` override, identical to WHERE.

**Pitfall:** HAVING without GROUP BY is syntactically valid in some databases but produces undefined results. Always pair HAVING with at least one GroupBy call.

---

## 6. ORDER BY

### Basic usage

```csharp
.OrderBy((User u) => u.Name)                   // ASC
.OrderByDescending((User u) => u.CreatedAt)    // DESC

// Multiple columns — left-to-right priority
.OrderBy((User u) => u.LastName)
.ThenBy((User u) => u.FirstName)
.ThenByDescending((User u) => u.CreatedAt)
```

`ThenBy` / `ThenByDescending` are identical to `OrderBy` / `OrderByDescending` — they exist purely to express secondary-sort intent.

### String field name overloads

When the column is not on a strongly-typed model:

```csharp
qb.UseOrdering().OrderBy<User>("FullName");
qb.UseOrdering().OrderByDescending<User>("CreatedAt");
```

### Ordering by a SELECT alias

A computed alias (e.g. from CASE WHEN) is not a real column — it must not be qualified with a table alias:

```csharp
qb.UseOrdering()
    .DoNotQualifyWithTableName()
    .OrderBy<User>("StatusLabel");
// → "Order By StatusLabel Asc"
```

### tableAlias override

```csharp
.OrderBy((Employee e) => e.Name, tableAlias: "mgr")
// → "Order By mgr.Name Asc"
```

---

## 7. Paging

Always add an ORDER BY before paging — without a stable sort, pages are non-deterministic.

### SQL Server — ROW_NUMBER()

Compatible with SQL Server 2005+ and Azure SQL.

```csharp
Q.New()
    .Select((User u) => u.Id)
    .Select((User u) => u.Name)
    .PageSqlServer((User u) => u.Id, page: 1, pageSize: 20)
    .Build();
```

The sort column for ROW_NUMBER is specified directly in the paging call. A separate `.OrderBy()` is not required.

### ANSI — OFFSET / FETCH NEXT

Compatible with SQL Server 2012+, PostgreSQL 9.3+, SQLite 3.25+.

```csharp
Q.New()
    .Select((User u) => u.Id)
    .OrderBy((User u) => u.Id)
    .PageOffsetFetch((User u) => u.Id, page: 2, pageSize: 25)
    .Build();
// → "… Order By tUser.Id Asc OFFSET 25 ROWS FETCH NEXT 25 ROWS ONLY"
```

### MySQL — LIMIT / OFFSET

Compatible with MySQL and MariaDB.

```csharp
Q.New()
    .Select((User u) => u.Id)
    .PageMySql((User u) => u.Id, page: 3, pageSize: 10, orderAscending: false)
    .Build();
// → "… Order By tUser.Id Desc LIMIT 20,10"
```

### All paging parameters

| Parameter | Type | Meaning |
|---|---|---|
| `fieldSelector` | lambda | Column to sort by |
| `page` | `uint` | 1-based page number |
| `pageSize` | `ushort` | Rows per page |
| `orderAscending` | `bool` | `true` = ASC (default), `false` = DESC |

---

## 8. CTEs

```csharp
var activeUsersQuery = Q.New(parameterize: false)
    .Select((User u) => u.Id)
    .Select((User u) => u.Name)
    .Where((User u) => u.Active, FilterOperator.EqualTo, true)
    .Build();

var result = Q.New(parameterize: false)
    .WithCte("ActiveUsers", activeUsersQuery)
    .Select((User u) => u.Name)
    .Build();
// → "With ActiveUsers As (\nSelect …\n)\nSelect tUser.Name From User tUser"
```

Multiple CTEs:

```csharp
Q.New(parameterize: false)
    .WithCte("Cte1", query1)
    .WithCte("Cte2", query2)
    .Select(...)
    .Build();
// → "With Cte1 As (…), Cte2 As (…)\nSelect …"
```

**Constraint:** CTEs are emitted in declaration order. A CTE cannot reference another CTE declared after it. Declare them in dependency order.

---

## 9. Set Operations

```csharp
var query1 = Q.New(parameterize: false).Select((User u) => u.Id).Build();
var query2 = Q.New(parameterize: false).Select((Admin a) => a.UserId).Build();

// UNION — deduplicates rows
Q.New(parameterize: false).Select((User u) => u.Id).Union(query2).Build();

// UNION ALL — keeps duplicates, faster
Q.New(parameterize: false).Select((User u) => u.Id).UnionAll(query2).Build();

// INTERSECT — ⚠ not in MySQL
Q.New(parameterize: false).Select((User u) => u.Id).Intersect(query2).Build();

// EXCEPT — ⚠ not in MySQL
Q.New(parameterize: false).Select((User u) => u.Id).Except(query2).Build();
```

**Requirements:**
- Both sides must select the same number of columns.
- Column types should be compatible.
- Column names come from the first (left) query.

**DB compatibility:**

| Operation | SQL Server | PostgreSQL | MySQL / MariaDB |
|---|---|---|---|
| UNION | ✓ | ✓ | ✓ |
| UNION ALL | ✓ | ✓ | ✓ |
| INTERSECT | ✓ | ✓ | ✗ |
| EXCEPT | ✓ | ✓ | ✗ |

Chaining:

```csharp
Q.New(parameterize: false)
    .Select((User u) => u.Id)
    .Union(query2)
    .Union(query3)
    .Build();
```

---

## 10. Parameterization Deep-Dive

### How parameters are named

Each bound value gets a parameter named `@p_<fieldName>`. Collisions within the same query get a counter suffix:

```csharp
var r = Q.New()
    .Select((User u) => u.Name)
    .Where((User u) => u.Age, FilterOperator.GreaterThan, 18)
    .AndWhere((User u) => u.Age, FilterOperator.LessThan, 65)
    .BuildWithParameters();
// r.Parameters → { "@p_Age": 18, "@p_Age_1": 65 }
```

### IN generates N individual parameters

```csharp
var r = Q.New()
    .Select((User u) => u.Id)
    .WhereIn((User u) => u.Id, new[] { 1, 2, 3 })
    .BuildWithParameters();
// r.Sql        → "… tUser.Id  in (@p_Id, @p_Id_1, @p_Id_2)\n"
// r.Parameters → { "@p_Id": 1, "@p_Id_1": 2, "@p_Id_2": 3 }
```

This is not the same as an array parameter — values are enumerated at build time. SQL Server has a 2100 total parameter limit per query. For large sets, consider a temp table or JOIN strategy.

### WhereExplicitly is unavailable in parameterized mode

Calling `WhereExplicitly` when `parameterize: true` throws `InvalidOperationException`. Use a typed `Where` overload instead.

### Security

Always use parameterized mode (the default) for any value sourced from user input, HTTP parameters, or external systems. Literal mode is safe only for compile-time constants or trusted internal values.

---

## 11. Self-Joins

### The problem

The auto-generated alias is always `"t" + TypeName`. Two references to the same table in one query get the same alias, producing invalid SQL:

```sql
-- WRONG (both sides get tEmployee)
select tEmployee.Name, tEmployee.Name
from Employee tEmployee
join Employee tEmployee on tEmployee.ManagerId = tEmployee.Id
```

### Solution

Every join method accepts optional `leftAlias` and `rightAlias`. Every SELECT, WHERE, ORDER BY, GROUP BY, and HAVING method accepts optional `tableAlias`. Providing these overrides the auto-generated alias for that specific reference.

### Complete self-join example

```csharp
var result = Q.New()
    .Select((Employee e) => e.Name, tableAlias: "emp")
    .Select((Employee e) => e.Name, "ManagerName", tableAlias: "mgr")
    .InnerJoin((Employee e) => e.ManagerId, (Employee m) => m.Id,
        leftAlias: "emp", rightAlias: "mgr")
    .Where((Employee e) => e.Active, FilterOperator.EqualTo, true, tableAlias: "emp")
    .OrderBy((Employee e) => e.Name, tableAlias: "emp")
    .BuildWithParameters();
```

**Emitted SQL:**
```sql
Select
emp.Name,
mgr.Name as ManagerName From Employee emp
join Employee mgr on emp.ManagerId = mgr.Id
Where emp.Active = @p_Active

Order By emp.Name Asc
```

### Rules

1. **Both** `leftAlias` and `rightAlias` must be supplied on the join call. Providing only one is not supported and falls back to auto-alias behaviour.
2. Every SELECT / WHERE / ORDER BY / GROUP BY / HAVING call that references the self-joined table **must** include `tableAlias:` to specify which instance.
3. For methods that have both a column-alias overload and a table-alias overload (e.g. `Select`), use the **named argument** syntax to avoid ambiguity: `Select((Employee e) => e.Name, tableAlias: "emp")`.

### Triple self-join

```csharp
// Employee → Manager → Director
Q.New()
    .Select((Employee e) => e.Name, tableAlias: "emp")
    .Select((Employee e) => e.Name, "ManagerName",  tableAlias: "mgr")
    .Select((Employee e) => e.Name, "DirectorName", tableAlias: "dir")
    .InnerJoin((Employee e) => e.ManagerId, (Employee m) => m.Id,
        leftAlias: "emp", rightAlias: "mgr")
    .InnerJoin((Employee e) => e.ManagerId, (Employee d) => d.Id,
        leftAlias: "mgr", rightAlias: "dir")
    .Build();
```

### Self-join with GROUP BY and HAVING

```csharp
Q.New()
    .Select((Employee e) => e.DeptId, tableAlias: "emp")
    .Aggregate((Employee e) => e.Id, "HeadCount", AggregateFunction.Count, tableAlias: "emp")
    .InnerJoin((Employee e) => e.ManagerId, (Employee m) => m.Id,
        leftAlias: "emp", rightAlias: "mgr")
    .GroupBy((Employee e) => e.DeptId, tableAlias: "emp")
    .Having((Employee e) => e.Id, FilterOperator.GreaterThan, 5, tableAlias: "emp")
    .Build();
```

---

## 12. Known Limitations

| Limitation | Detail / Workaround |
|---|---|
| Builder is one-time use | `Build()` / `BuildWithParameters()` resets internal state. Create a new `Q.New()` for each query. |
| No subqueries in SELECT position | Subqueries are only supported in WHERE via `WhereExists` / `WhereNotExists`. |
| No window functions | Deferred to v7.1. `ROW_NUMBER` is only available through the paging methods. |
| CASE WHEN result values are always strings | `Then("val")` and `Else("val")` always emit single-quoted values. Numeric THEN is not supported. |
| FULL OUTER JOIN not in MySQL | Workaround: `LEFT JOIN … UNION … RIGHT JOIN`. |
| INTERSECT / EXCEPT not in MySQL | Workaround: subquery or application-level filtering. |
| `Top()` is SQL Server only | Use `PageOffsetFetch` (SQL Server 2012+ / PostgreSQL) or `PageMySql` for cross-database limiting. |
| No OR between join conditions | Each join emits one equality ON clause. Additional conditions must be WHERE predicates. |
| No composite join key in one call | Workaround: add the extra equality as a WHERE predicate or chain a second join. |
| Large IN lists generate many parameters | SQL Server caps total parameters at 2100 per query. Use a temp table or JOIN for large sets. |
| `WhereExplicitly` unavailable in parameterized mode | Use a typed `Where` overload instead. |
| Self-join requires manual aliases everywhere | Every column reference for a self-joined table must carry an explicit `tableAlias:` named argument. |
