# QBuilder v8 — Full Fluency Implementation Plan

**Target version:** `8.0.0`  
**Breaking change:** `QBuilderExtensions` removed, `WhereConjunctionBuilder` made internal.  
**New capability:** Every clause has a `TableBound<T>` builder; all predicates are expression-based with full type inference and `And*`/`Or*` named variants.

---

## Guiding principle

```csharp
// Zero explicit type args. Cross-clause chain via .Then().
Q.New()
    .UseTableBoundSelector<User>()
        .Column(u => u.Name)
    .Then()
    .UseTableBoundJoinBuilder<User, Order>()
        .InnerJoin(u => u.Id, o => o.UserId)      // returns QBuilder directly
    .UseTableBoundFilter<User>()
        .WhereEqualTo(u => u.IsActive, true)
        .AndWhereIsNull(u => u.DeletedAt)
        .If(!string.IsNullOrWhiteSpace(name),
            tb => tb.AndWhereContains(u => u.Name, name))
    .Then()
    .UseTableBoundOrderBy<User>()
        .Ascending(u => u.Name)
    .Then()
    .BuildWithParameters();
```

---

## Task checklist

### Infrastructure
- [ ] **INFRA-1** Add `Microsoft.Data.Sqlite` + `Dapper` to test project
- [ ] **INFRA-2** Create `IntegrationTestBase` + schema helpers (User, Order tables in SQLite)
- [ ] **INFRA-3** Add first integration smoke test (SELECT * FROM User)

### Bug fix
- [ ] **BUG-1** Add regression test: `TableBoundWhereBuilder<T>.WhereIsNull` compiles and generates `IS NULL`

### `TableBoundWhereBuilder<T>` — return type change *(compat break)*
- [ ] **TBWB-1** Change all existing predicate methods to return `TableBoundWhereBuilder<TTable>` (returning `this`) and always explicitly set conjunction to "And" before delegating

### `TableBoundWhereBuilder<T>` — missing predicates
- [ ] **TBWB-2** Add `WhereBetween<TField>` + `WhereNotBetween<TField>` (delegates to `WhereBuilder`)
- [ ] **TBWB-3** Add `WhereExists(QBuilder)` + `WhereNotExists(QBuilder)`

### `TableBoundWhereBuilder<T>` — `And*` family
- [ ] **TBWB-4** `AndWhereEqualTo`, `AndWhereNotEqualTo`
- [ ] **TBWB-5** `AndWhereLessThan`, `AndWhereLessThanOrEqualTo`, `AndWhereGreaterThan`, `AndWhereGreaterThanOrEqualTo`
- [ ] **TBWB-6** `AndWhereContains`, `AndWhereStartsWith`, `AndWhereEndsWith`
- [ ] **TBWB-7** `AndWhereIsNull`, `AndWhereIsNotNull`
- [ ] **TBWB-8** `AndWhereIn`, `AndWhereNotIn`
- [ ] **TBWB-9** `AndWhereBetween`, `AndWhereNotBetween`
- [ ] **TBWB-10** `AndWhereExists`, `AndWhereNotExists`

### `TableBoundWhereBuilder<T>` — `Or*` family *(FEAT-1)*
- [ ] **TBWB-11** `OrWhereEqualTo`, `OrWhereNotEqualTo`
- [ ] **TBWB-12** `OrWhereLessThan`, `OrWhereLessThanOrEqualTo`, `OrWhereGreaterThan`, `OrWhereGreaterThanOrEqualTo`
- [ ] **TBWB-13** `OrWhereContains`, `OrWhereStartsWith`, `OrWhereEndsWith`
- [ ] **TBWB-14** `OrWhereIsNull`, `OrWhereIsNotNull`
- [ ] **TBWB-15** `OrWhereIn`, `OrWhereNotIn`
- [ ] **TBWB-16** `OrWhereBetween`, `OrWhereNotBetween`
- [ ] **TBWB-17** `OrWhereExists`, `OrWhereNotExists`

### `TableBoundWhereBuilder<T>` — utility *(FEAT-2 + QoL-4)*
- [ ] **UTIL-1** Add `.If(bool, Func<TableBoundWhereBuilder<T>, TableBoundWhereBuilder<T>>)` *(FEAT-2)*
- [ ] **UTIL-2** Add `WhereExplicitly(string sql, object parameters = null)` parameterized overload *(QoL-4)*
- [ ] **UTIL-3** Rename `OpenParentheses`/`CloseParentheses` → `OpenGroup`/`CloseGroup` on `TableBoundWhereBuilder<T>`

### `TableBoundSelectBuilder<T>` — complete the surface
- [ ] **SELECT-1** Add `SelectCaseWhen(CaseWhenBuilder, string alias)`
- [ ] **SELECT-2** Add `Aggregate<TField>(Expression, string alias, AggregateFunction)` with enum (mirrors extensions logic)

### `TableBoundHavingBuilder<T>` — new *(Or* family + factory)*
- [ ] **HAVING-1** Create `TableBoundHavingBuilder<T>` mirroring `TableBoundWhereBuilder<T>` (wraps `HavingBuilder`)
- [ ] **HAVING-2** Add `QBuilder.UseTableBoundHaving<TTable>()` factory method

### `TableBoundOrderByBuilder<T>` — new
- [ ] **ORDER-1** Create `TableBoundOrderByBuilder<T>` with `Ascending`, `Descending`, `ThenAscending`, `ThenDescending`
- [ ] **ORDER-2** Add `QBuilder.UseTableBoundOrderBy<TTable>()` factory method

### Debug logging *(QoL-3)*
- [ ] **LOG-1** Add `BuildWithParameters(Action<string> logSql = null)` overload

### Breaking cleanup
- [ ] **CONJ-1** Make `WhereConjunctionBuilder` `internal`; fix filename typo (`WhereConjuntionBuilder.cs` → `WhereConjunctionBuilder.cs`)
- [ ] **EXT-1** Delete `QBuilderExtensions.cs`

### Tests
- [ ] **TEST-1** Rewrite `FluentApiTests.cs` — all tests use v8 `TableBound*` API
- [ ] **TEST-2** Integration tests: `And*`/`Or*` family validated against live SQLite data
- [ ] **TEST-3** Integration tests: `.If()`, `WhereExists`, `WhereBetween` against live SQLite data
- [ ] **TEST-4** Integration tests: `TableBoundHavingBuilder<T>` + `TableBoundOrderByBuilder<T>`

### Release
- [ ] **RELEASE-1** Bump version to `8.0.0` in `.csproj`
- [ ] **DOCS-1** Write `docs/migration-v7-to-v8.md` with full method mapping table
- [ ] **DOCS-2** Write `CHANGELOG.md` entry for v8.0.0

---

## Architecture notes

### Why `SetNextConjunction` must be called explicitly in every method

`WhereBuilder._nextConjuntion` is sticky — it stays "Or" until reset. Every public method on
`TableBoundWhereBuilder<T>` must call `_whereBuilder.SetNextConjunction("And"/"Or")` before
delegating, so callers cannot accidentally inherit a previous OR context.

### `WhereConjunctionBuilder.Then()` already works (via `BuilderBase`)

The test in `WhereBuilderTests.cs` already chains `.WhereEqualTo(...).Then()` successfully.
`WhereConjunctionBuilder` inherits `Then()` from `BuilderBase`. After TBWB-1, `.WhereEqualTo`
returns `TableBoundWhereBuilder<T>` whose `Then()` (also from `BuilderBase`) returns `QBuilder`.
The chain works in both states.

### `FieldNameResolver` uses the C# property name only

No `[Column]` attribute support. Column names in the model must match the DB column names.
This is a known limitation; document in migration guide.

### Integration tests use SQLite in-memory with Dapper

Generated SQL is validated by actually executing it against an in-memory SQLite database seeded
with known data. This proves correctness end-to-end, not just SQL string shape.
