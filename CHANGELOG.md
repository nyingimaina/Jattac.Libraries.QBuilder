# Changelog

All notable changes to this project are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

---

## [7.0.0-rc2] — 2026-06-14

### Added

- `TableBoundWhereBuilder<T>.WhereIsNull<TField>(Expression)` and `WhereIsNotNull<TField>(Expression)` — lambda overloads for IS NULL / IS NOT NULL checks in the legacy `Use*()/Then()` API, matching the lambda API available on the fluent extension API.

### Fixed

- **Backward-compatibility regression** — `UseTableBoundSelector<T>().Select(expr, alias)` was dispatching to the `SelectBuilder.Select(expr, explicitTableAlias)` overload instead of `SelectBuilder.Select(expr, fieldAlias, explicitTableAlias)`, causing the alias string to be used as a table-name prefix rather than a column alias. This produced broken SQL (`Alias.Column` instead of `Table.Column AS Alias`) and a `MySqlException: Unknown column` at runtime when code was migrated from `Rocket.Libraries.QBuilder` with a namespace-only change. Now dispatches to the correct overload.

---

## [7.0.0-beta01] — 2026-06-12

Major release introducing a fully-fledged C# dialect of SQL with a zero-boilerplate fluent API, complete SQL clause coverage, and XML doc comments on every public member.

### Added

**Entry point**
- `Q` static factory — `Q.New()` (parameterized by default), `Q.New(false)`, `Q.New(tableNameResolver)`

**Fluent extension API (`QBuilderExtensions`)**
- `Select<TTable, TField>` / `Select<TTable, TField>(alias)` — lambda-based column selection
- `Aggregate<TTable, TField>(alias, AggregateFunction)` — SUM, COUNT, AVG, MIN, MAX, COUNT DISTINCT
- `Top(n)`, `Distinct()`
- `SelectCaseWhen(CaseWhenBuilder, alias)` — inline CASE WHEN expressions
- `InnerJoin`, `LeftJoin`, `RightJoin`, `FullOuterJoin`, `CrossJoin` — all five join types via lambda
- `Where`, `AndWhere`, `OrWhere` — typed predicates with any `FilterOperator`
- `WhereIsNull`, `AndWhereIsNull`, `WhereIsNotNull`, `AndWhereIsNotNull`
- `WhereBetween`, `AndWhereBetween` — BETWEEN with full parameterization
- `WhereIn`, `AndWhereIn`, `WhereNotIn`, `AndWhereNotIn`
- `WhereExists`, `AndWhereExists`, `WhereNotExists`, `AndWhereNotExists`
- `OpenGroup`, `CloseGroup` — nestable parenthesis groups
- `Having`, `AndHaving`
- `GroupBy<TTable, TField>`
- `OrderBy`, `OrderByDescending`, `ThenBy`, `ThenByDescending`
- `PageSqlServer`, `PageOffsetFetch`, `PageMySql`

**New builders**
- `HavingBuilder` — reuses `WhereBuilder` internals, emits `HAVING` keyword
- `CaseWhenBuilder` — fluent `CASE WHEN … THEN … ELSE … END` builder; `CaseWhenBuilder.For<TTable>().When(…).Then(…).Else(…)`
- `OffsetFetchPagingBuilder<TTable>` — `OFFSET n ROWS FETCH NEXT n ROWS ONLY` (SQL Server 2012+ / ANSI SQL)

**New enums**
- `AggregateFunction` — `Count`, `CountDistinct`, `Sum`, `Avg`, `Min`, `Max`
- `JoinType` — `Inner`, `Left`, `Right`, `FullOuter`, `Cross`
- `PageFlavor` — `SqlServer`, `SqlServerOffsetFetch`, `MySql`
- `SetOperationType` — `Union`, `UnionAll`, `Intersect`, `Except`

**QBuilder — new methods**
- `UseHaving()` — returns the `HavingBuilder` for the current query
- `UseOffsetFetchPagingBuilder<TTable>()`
- `WithCte(name, subQuery)` — prepends a named CTE; multiple calls produce a comma-separated `WITH` block
- `Union(other)`, `UnionAll(other)`, `Intersect(other)`, `Except(other)` — all four SQL set operations

**WhereBuilder — new methods**
- `WhereIsNull<TTable>(field)`, `WhereIsNotNull<TTable>(field)`
- `WhereBetween<TTable>(field, from, to)`, `WhereNotBetween<TTable>(field, from, to)`
- `WhereExists(subQuery)`, `WhereNotExists(subQuery)`
- Constructor now accepts `keyword` parameter (`"Where"` / `"Having"`) for subclass reuse

**JoinBuilder**
- `CrossJoin<TLeftTable, TRightTable>()` — Cartesian product, no ON clause emitted

**SelectBuilder**
- `SelectCaseWhen(CaseWhenBuilder, alias)`

**OrderBuilder**
- Multi-column ORDER BY — `OrderBy` / `OrderByDescending` now append to a list instead of overwriting
- Lambda overloads: `OrderBy<TTable, TField>(Expression)`, `OrderByDescending<TTable, TField>(Expression)`

**GroupBuilder**
- Lambda overload: `GroupBy<TTable, TField>(Expression)`

**FilterOperator enum**
- `IsNull = 10`, `IsNotNull = 11`, `Between = 12`, `NotBetween = 13`

**Documentation**
- Comprehensive `README.md` at repo root — getting started, all features, pitfalls, best practices, real-world example
- XML doc comments on every public type, method, parameter, and enum value

**Tests**
- `FluentApiTests.cs` — 61 tests covering the entire extension API
- `FullSqlFeaturesTests.cs` — 36 tests covering every new SQL feature
- Total: **97 tests, 0 failures**

### Fixed

- `JoinTypes.LeftJoin` constant value was `"FullLeft"` → corrected to `"Left"`
- `JoinTypes.RightJoin` constant value was `"FullRight"` → corrected to `"Right"`
- `TableNameAliaser.GetTableAlias` now strips schema prefix — `dbo.Users` → `tUsers` (was `tdbo.Users`)
- `PageRange.GetHashCode` was calling `base.GetHashCode()` (always same value) → now uses `HashCode.Combine(Start, End, PageSize)`
- `ConditionMaker`: `StartsWith` and `EndsWith` LIKE patterns were swapped
- `WhereBuilder.OpenParentheses` rejected nested groups — guard removed; nesting is now fully supported
- `QBuilder.Build()` can no longer be called twice on the same instance (throws `InvalidOperationException`)

### Breaking changes

None — the v7.0 extension API is purely additive. All existing `Use*()/Then()` code compiles and runs unchanged.

---

## [6.2.0-beta06] — unreleased

### Fixed
- `WhereIn` parameterized path: parameter names enclosed in brackets to handle reserved words

---

## [6.2.0-beta01] — unreleased

### Changed
- Migrated target framework to `net6.0`

---

## [6.1.7-beta03]

### Fixed
- Typo fix in internal builder

## [6.1.7-beta02]

### Fixed
- Aggregation querying and ordering while paging

## [6.1.7-beta01]

### Added
- `SelectExplicit` on `DerivedTableSelector`

---

## [6.1.6]

### Fixed
- Bad string interpolation in query generation

---

## [6.1.5]

### Fixed
- String interpolation issue

---

## [6.1.4]

### Fixed
- Bad string interpolation

---

## [6.1.3]

### Fixed
- SQL Server ordering fix

---

## [6.1.2]

### Added
- Ability to specify sort direction when paging

---

## [6.1.1]

### Fixed
- Build fix

---

## [6.1.0]

### Added
- New filter syntax via `FilterDescription<TTable>`

---

## [6.0.0]

### Added
- MySQL / MariaDB paging support (`MySqlServerPagingBuilder`)

---

## [5.x and earlier]

### Added (cumulative)
- SQL Server ROW_NUMBER paging
- Derived table joins (`DerivedTableJoiner`)
- `TableBoundSelectBuilder`, `TableBoundJoinBuilder`, `TableBoundWhereBuilder`, `TableBoundGroupBuilder` — typed lambda-based sub-builders
- `GroupBuilder` — GROUP BY clause
- `OrderBuilder` — ORDER BY clause
- `WhereBuilder` — WHERE with AND/OR conjunctions, IN, NOT IN, optional where, explicit where, parenthesis groups
- `JoinBuilder` — INNER, LEFT, RIGHT, FULL OUTER joins
- `SelectBuilder` — SELECT with aggregates, DISTINCT, TOP
- Initial release — basic SELECT / WHERE / FROM query building
