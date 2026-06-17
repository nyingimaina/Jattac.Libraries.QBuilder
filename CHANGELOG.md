# Changelog

All notable changes to this project are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

---

## [9.1.1] — 2026-06-17

### Documentation

- **Derived table queries** — new section documenting `UseDerivedTableSelector` (selecting columns from a subquery in the FROM clause), `UseJoiner().UseDerivedTableJoiner<T>()` (joining a derived table against an outer table; all four join types — `InnerJoin`, `LeftJoin`, `RightJoin`, `FullJoin`), and chaining multiple derived table joins on the same builder.
- **`AggregateRowQuerierBuilder<T>`** — new section documenting the latest-row-per-group helper: `SetAggregationFunction`, `SetForeignKeyResolver`, `SetIncrementingFieldName`, `AddWhereEqualsToFilter` / `AddWhereInFilter`, `Build()`.
- **Gaps section corrected** — removed the false entry that listed `UseDerivedTableSelector` as an unimplemented gap. Replaced with an accurate entry for correlated subqueries in the SELECT list (which are genuinely not yet supported).

No code changes — this is a documentation-only release.

---

## [9.1.0] — 2026-06-16

### Added

- **`Dialect` enum** — `None` (backward-compat default, no quoting), `SqlServer` / `MsSql` (bracket `[…]` quoting, ROW_NUMBER paging), `MySql` / `MariaDb` (backtick `` `…` `` quoting, LIMIT paging), `Sqlite`, `Postgres`, `Generic` (double-quote `"…"` quoting, OFFSET/FETCH paging). `MsSql` is a value alias for `SqlServer`.

- **`QBuilderOptions`** — configuration object holding `Dialect`, `TableNameResolver`, and `AliasPrefix`. Drives identifier quoting and paging strategy for a `QBuilder` instance.

- **`QBuilderConfig`** static API — modelled after `IHttpClientFactory` named-client pattern:
  - `QBuilderConfig.ConfigureDefault(opt => ...)` — set the application-wide default once at startup.
  - `QBuilderConfig.Configure("name", opt => ...)` — register named configurations for different databases, schemas, or read replicas.
  - `QBuilderConfig.Reset()` — restore factory defaults (test-isolation helper).

- **`Q.New()` overloads**:
  - `Q.New()` / `Q.New(parameterize: false)` — reads from the configured default.
  - `Q.New("name")` / `Q.New("name", false)` — reads from a named config.
  - `Q.New(QBuilderOptions options)` — creates a QBuilder from an inline options object, bypassing the registry.
  - `Q.New(Func<Type, string> resolver)` — existing inline resolver overload retained (backward compat).

- **`IdentifierQuoter`** (internal) — single quoting helper used by every builder. `QuoteTable` handles schema-qualified names (`dbo.Users` → `[dbo].[Users]`). `QuoteIdentifier` quotes a single part.

- **Identifier quoting in all builders** — quoting is applied wherever a table name, alias, or column name is emitted, for every SQL clause: SELECT column list, FROM table + alias, JOIN … ON, WHERE field references, ORDER BY, GROUP BY, DML table names, INSERT column list, UPDATE SET clause. `Dialect.None` is a strict no-op so all existing code and tests are unaffected.

- **`QBuilder.UsePageBy<TTable, TField>(fieldSelector, page, pageSize, ascending)`** — dialect-aware paging facade: dispatches to `UseSqlServerPagingBuilder` (SqlServer/MsSql), `UseMySqlServerPagingBuilder` (MySql/MariaDb), or `UseOffsetFetchPagingBuilder` (all others). Explicit dialect-specific paging methods are retained.

- **23 new tests** — `DialectAndConfigTests.cs` covers all six dialects, config registry isolation, reset, `Q.New` overloads, `UsePageBy` dispatch, DML quoting, and WHERE clause quoting. Total: **174 tests**.

### Architecture

Dialect flows via `QBuilder._options` (set once at construction, read by every builder through `QBuilder.Dialect`). No builder constructor was changed — zero breaking change surface.

---

## [9.0.0] — 2026-06-16

### Added

- **`TableBoundDeleteBuilder<T>`** — fluent DELETE builder. `Q.New(true).UseTableBoundDelete<T>().WhereEqualTo(...).BuildWithParameters()` produces Dapper-ready parameterized SQL. Bare-table delete (no WHERE) is blocked by default; call `.ForEntireTable()` to opt out.
- **`TableBoundUpdateBuilder<T>`** — fluent UPDATE builder. Chain `.Set(col, value)` calls followed by `.WhereEqualTo(...)` then `.BuildWithParameters()`. Bare-table update (no WHERE) is blocked by default; call `.ForEntireTable()` to opt out.
- **`TableBoundInsertBuilder<T>`** — fluent INSERT builder. Chain `.Value(col, value)` calls then `.BuildWithParameters()`. Emits `INSERT INTO {table} (cols) VALUES (params)`.
- All three builders:
  - Accept an optional `Action<string> logSql` delegate on `BuildWithParameters()` — receives the generated SQL for logging (consistent with the existing `QBuilder.BuildWithParameters(Action<string>)` overload).
  - Also expose `.Build()` returning a raw SQL `string` for non-parameterized contexts.
  - Expose the full WHERE predicate family (`WhereEqualTo`/`And*/Or*`, `WhereBetween`, `WhereIn`, `WhereIsNull`, `WhereExists`, `OpenGroup`/`CloseGroup`, etc.) via the shared `DmlWhereBuilder<TTable, TBuilder>` CRTP base — WHERE predicates use the actual table name as the column qualifier (`User.Id`) rather than the SELECT alias (`tUser.Id`).
- Entry points on `QBuilder`: `UseTableBoundDelete<T>()`, `UseTableBoundUpdate<T>()`, `UseTableBoundInsert<T>()` — follow the same factory pattern as all other `UseTableBound*` methods.
- 19 new tests (9 contract/guard, 10 SQLite integration).

### Architecture

DML builders are **terminal builders** — they produce `BuiltQuery` directly and do not chain back through `QBuilder.Build()`. The shared `DmlWhereBuilder<TTable, TBuilder>` abstract base uses the CRTP pattern so all WHERE methods return the correct concrete type, keeping the Dapper call as the last step: `conn.Execute(q.ParameterizedSql, q.Parameters)`.

---

## [8.0.1] — 2026-06-16

### Changed

- **Replaced `Rocket.Libraries.Validation 2.0.0-beta05` with an internal `Guard` class.** The prerelease dependency has been removed entirely. All validation is now handled by `Guard.Against` (`InvalidOperationException`), `Guard.NotNull` (`ArgumentNullException`), and `Guard.Range` (`ArgumentOutOfRangeException`) — standard .NET exception types that callers can catch without any additional dependency.
- **Improved exception types for null and range checks.** Null filter values now throw `ArgumentNullException` instead of a custom `FailedValidationException`. Invalid page/pageSize values now throw `ArgumentOutOfRangeException`. All builder-state invariants continue to throw `InvalidOperationException`.

### Fixed

- 11 `CA2000` (`IDisposable` not disposed) warnings eliminated — `DataValidator` was instantiated inline and never disposed at every call site.
- `NU5104` warning eliminated — stable NuGet packages cannot depend on prerelease packages; the dependency is now gone.

---

## [8.0.0] — 2026-06-16

### Breaking changes

- **`QBuilderExtensions` removed.** All `Select<T,F>()`, `Where<T,F>()`, `AndWhere<T,F>()`, `Having<T,F>()`, `GroupBy<T,F>()`, `OrderBy<T,F>()`, `InnerJoin<TL,TR,KL,KR>()`, `PageSqlServer<T,F>()`, `PageOffsetFetch<T,F>()`, and related extension methods on `QBuilder` are deleted. Migrate to the `TableBound*` builder API — see `docs/migration-v7-to-v8.md` for the full method-by-method mapping.
- **`WhereConjunctionBuilder` is now `internal`.** Code that held a `WhereConjunctionBuilder` variable must switch to chaining on `TableBoundWhereBuilder<T>`.
- **`UseFilter()` and `UseHaving()` are now `internal`.** Use `UseTableBoundFilter<T>()` and `UseTableBoundHaving<T>()`.

### Added

- **Full `And*`/`Or*` predicate family on `TableBoundWhereBuilder<T>`.** Every base predicate now has `And*` and `Or*` variants: `AndWhereEqualTo`/`OrWhereEqualTo`, `AndWhereLessThan`/`OrWhereLessThan`, `AndWhereGreaterThan`/`OrWhereGreaterThan`, `AndWhereContains`/`OrWhereContains`, `AndWhereStartsWith`/`OrWhereStartsWith`, `AndWhereEndsWith`/`OrWhereEndsWith`, `AndWhereIsNull`/`OrWhereIsNull`, `AndWhereIsNotNull`/`OrWhereIsNotNull`, `AndWhereIn`/`OrWhereIn`, `AndWhereNotIn`/`OrWhereNotIn`, `AndWhereBetween`/`OrWhereBetween`, `AndWhereNotBetween`/`OrWhereNotBetween`, `AndWhereExists`/`OrWhereExists`, `AndWhereNotExists`/`OrWhereNotExists`. All return `TableBoundWhereBuilder<T>` for fluent chaining.
- **`WhereBetween` / `WhereNotBetween` on `TableBoundWhereBuilder<T>`.** Full And*/Or* variants.
- **`WhereExists(QBuilder)` / `WhereNotExists(QBuilder)` on `TableBoundWhereBuilder<T>`.** Pass a sub-`QBuilder` as the EXISTS subquery. Full And*/Or* variants.
- **`TableBoundHavingBuilder<T>`.** New builder exposing the same predicate surface as `TableBoundWhereBuilder<T>` but targeting the `HAVING` clause. Access via `qb.UseTableBoundHaving<T>()`.
- **`TableBoundOrderByBuilder<T>`.** New builder for type-safe ORDER BY: `Ascending(expr)`, `Descending(expr)`, `ThenAscending(expr)`, `ThenDescending(expr)`. Access via `qb.UseTableBoundOrderBy<T>()`.
- **`.If(condition, builder)` on `TableBoundWhereBuilder<T>`.** Conditionally apply a filter branch without breaking the fluent chain.
- **`BuildWithParameters(Action<string> logSql)` overload.** Optional debug hook — the generated SQL is passed to the delegate before returning the `BuiltQuery`.
- **`OpenGroup()` / `CloseGroup()` aliases** on `TableBoundWhereBuilder<T>` (renamed from `OpenParentheses`/`CloseParentheses`).
- **19 SQLite integration tests** covering `And*`/`Or*` families, `.If()`, `WhereBetween`, `WhereExists`/`WhereNotExists`, `TableBoundOrderByBuilder<T>`, and `TableBoundHavingBuilder<T>`.

### Fixed

- **Parameterized bool comparison (`ConditionMaker`).** `ConditionMaker.GetCondition` previously called `value.ToString()` for the default operator case, converting C# `true`/`false` to strings `"True"`/`"False"`. These strings fail comparison against INTEGER columns. Now stores the raw `object` value so ADO.NET/Dapper handles type conversion correctly (`true` → `1`, `false` → `0`).

### Internal

- `WhereConjuntionBuilder.cs` filename typo fixed → `WhereConjunctionBuilder.cs`.

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
