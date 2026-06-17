# Business Requirements Specification
## POCO Convenience Layer for DML Builders
### Jattac.Libraries.QBuilder — v9.2

---

## Table of Contents

1. [Purpose and Context](#1-purpose-and-context)
2. [What You Are Building](#2-what-you-are-building)
3. [Codebase Orientation — Read This First](#3-codebase-orientation--read-this-first)
4. [How the Existing DML Builders Work](#4-how-the-existing-dml-builders-work)
5. [Design Decisions — Locked, Not Yours to Change](#5-design-decisions--locked-not-yours-to-change)
6. [File-by-File Implementation Instructions](#6-file-by-file-implementation-instructions)
   - 6.1 [New Attribute Files](#61-new-attribute-files)
   - 6.2 [New Helper: PocoReflector](#62-new-helper-pocoreflector)
   - 6.3 [Modify TableBoundInsertBuilder](#63-modify-tableboundinsertbuilder)
   - 6.4 [Modify DmlWhereBuilder](#64-modify-dmlwherebuilder)
   - 6.5 [Modify TableBoundUpdateBuilder](#65-modify-tableboundupdatebuilder)
   - 6.6 [Modify TableBoundDeleteBuilder](#66-modify-tablebounddeletebuilder)
   - 6.7 [Modify QBuilder — Shorthand Methods](#67-modify-qbuilder--shorthand-methods)
7. [Tests You Must Write](#7-tests-you-must-write)
8. [Migration Guide Update](#8-migration-guide-update)
9. [What Success Looks Like](#9-what-success-looks-like)
10. [Pitfalls — Read Before You Write a Single Line](#10-pitfalls--read-before-you-write-a-single-line)
11. [Bulk INSERT — `FromObjects`](#11-bulk-insert--fromobjects)
12. [Batched DML — `QBatch`](#12-batched-dml--qbatch)
13. [Bulk UPDATE and DELETE Patterns](#13-bulk-update-and-delete-patterns)
14. [Additional Tests for Bulk and Batch](#14-additional-tests-for-bulk-and-batch)
15. [Additional Pitfalls for Bulk and Batch](#15-additional-pitfalls-for-bulk-and-batch)

---

## 1. Purpose and Context

### The problem being solved

Right now, inserting or updating a row using this library looks like this:

```csharp
Q.New()
    .UseTableBoundInsert<User>()
    .Value(u => u.Id, user.Id)
    .Value(u => u.Name, user.Name)
    .Value(u => u.IsActive, user.IsActive)
    .Value(u => u.DeletedAt, user.DeletedAt)
    .BuildWithParameters();
```

Every property needs its own `.Value(...)` call. If a `User` has 12 properties, that is 12 lines of code that a developer must write by hand, in the right order, making sure they do not miss any column. If a new column is added to the model, the developer must remember to add a matching `.Value(...)` call everywhere the INSERT is performed. This is tedious, repetitive, and error-prone.

The solution is to allow passing the entire POCO object directly:

```csharp
Q.New()
    .UseTableBoundInsert<User>()
    .FromObject(user)           // reflects all properties automatically
    .BuildWithParameters();
```

The library will use C# reflection to iterate the properties of `user`, and call the underlying plumbing for each one — the same plumbing that the hand-written `.Value(...)` calls use. The developer gets convenience; the engine stays the same.

### What this is NOT

- This is NOT an ORM. The library does not track changes, manage relationships, or lazily load data.
- This is NOT replacing the fluent API. `.Value(...)`, `.Set(...)`, `.WhereEqualTo(...)` still exist and still work. `FromObject` is an additional convenience path on top of them.
- This is NOT adding support for anonymous projections, SELECT, or anything query-related.

---

## 2. What You Are Building

You are adding the following, in this order:

1. **Three new attribute classes** — tiny marker types that decorate POCO properties to give the library hints about what to do with each property.
2. **One new helper class (`PocoReflector`)** — reads those attributes and the property values off a POCO instance via reflection; caches the results for performance.
3. **`FromObject<T>(T obj)` method on `TableBoundInsertBuilder<TTable>`** — populates the INSERT from a POCO.
4. **A protected string-based WHERE helper on `DmlWhereBuilder`** — needed internally so the POCO path can add WHERE conditions without using expression trees.
5. **`FromObject<T>(T obj)` method on `TableBoundUpdateBuilder<TTable>`** — sets columns from non-key properties; adds WHERE from key properties.
6. **`FromObject<T>(T obj)` method on `TableBoundDeleteBuilder<TTable>`** — adds WHERE from key properties.
7. **Three shorthand methods on `QBuilder`** — `InsertFrom`, `UpdateFrom`, `DeleteFrom` — one-liner convenience.
8. **Tests** for all of the above.
9. **A section in the migration guide**.

---

## 3. Codebase Orientation — Read This First

Before you touch any file, understand the repo layout and the key classes you will interact with.

### Project structure

```
src/
  Jattac.Libraries.QBuilder/
    Attributes/                   ← YOU WILL CREATE THIS FOLDER AND PUT 3 FILES HERE
    Builders/
      DML/
        BuilderBase.cs            ← base class all builders inherit from
        DmlWhereBuilder.cs        ← YOU WILL MODIFY THIS
        TableBoundInsertBuilder.cs ← YOU WILL MODIFY THIS
        TableBoundUpdateBuilder.cs ← YOU WILL MODIFY THIS
        TableBoundDeleteBuilder.cs ← YOU WILL MODIFY THIS
      TableBoundWhereBuilder.cs   ← DO NOT TOUCH
      TableBoundSelectBuilder.cs  ← DO NOT TOUCH
    Config/
      QBuilderConfig.cs           ← DO NOT TOUCH
      QBuilderOptions.cs          ← DO NOT TOUCH
      QBuilderRegistry.cs         ← DO NOT TOUCH
    Enums/
      Dialect.cs                  ← DO NOT TOUCH
      FilterOperator.cs           ← DO NOT TOUCH
    Helpers/
      ConditionMaker.cs           ← READ, DO NOT TOUCH
      FieldNameResolver.cs        ← READ, DO NOT TOUCH
      Guard.cs                    ← READ, DO NOT TOUCH
      IdentifierQuoter.cs         ← READ, DO NOT TOUCH
      PocoReflector.cs            ← YOU WILL CREATE THIS
    QBuilder.cs                   ← YOU WILL MODIFY THIS
    BuiltQuery.cs                 ← READ ONLY
    Extensions/
      Q.cs                        ← DO NOT TOUCH

tests/
  Jattac.Libraries.QBuilder/
    DML/
      DmlValidationTests.cs       ← DO NOT TOUCH (reference for test style)
      Integration/
        DmlIntegrationTests.cs    ← DO NOT TOUCH (reference for integration test style)
        PocoIntegrationTests.cs   ← YOU WILL CREATE THIS
      PocoTests.cs                ← YOU WILL CREATE THIS
    Helpers/
      PocoReflectorTests.cs       ← YOU WILL CREATE THIS
    Models/
      User.cs                     ← YOU WILL ADD ATTRIBUTES TO SOME PROPERTIES
```

### Key classes to understand before touching anything

**`BuiltQuery`** (src/Jattac.Libraries.QBuilder/BuiltQuery.cs)

This is the return type of `BuildWithParameters()`. It has two properties:
- `string ParameterizedSql` — the SQL string with `@param` placeholders
- `Dictionary<string, object> Parameters` — a map of `@paramName → value`

Dapper (the micro-ORM used in integration tests) can take these two things and execute the query.

**`ConditionMaker.GetParameterName(string field, BuiltQuery builtQuery)`** (static method in `ConditionMaker.cs`)

This generates a parameter name like `@Id0`, `@Name1`, `@Id1` (incrementing suffix) that does not collide with any already-stored parameter in `builtQuery.Parameters`. Always use this method when creating a new `@param` — never hand-craft parameter names like `"@" + colName`.

Example: if `builtQuery.Parameters` already has `@Id0`, calling `GetParameterName("Id", builtQuery)` returns `@Id1`.

**`IdentifierQuoter.QuoteIdentifier(string name, Dialect dialect)`** (static method in `IdentifierQuoter.cs`)

Wraps an identifier in the correct quotes for the dialect:
- `Dialect.None` → `Id` (no quoting)
- `Dialect.SqlServer` → `[Id]`
- `Dialect.MySql` → `` `Id` ``
- `Dialect.Sqlite` / `Dialect.Postgres` / `Dialect.Generic` → `"Id"`

**`Guard.Against(bool condition, string message)`** and **`Guard.NotNull(object value, string message)`** (`Guard.cs`)

The pattern used throughout the library for validation. Throw `InvalidOperationException` or `ArgumentNullException` with a clear message when a caller has used the API incorrectly. Use these; do not write raw `throw new ...` statements.

**`QBuilder.Dialect`** and **`QBuilder.TableNameResolver`**

Every builder holds a reference to the `QBuilder` that created it (via `BuilderBase`). Access the dialect and table name resolver through that reference. In `BuilderBase`, the `QBuilder` is accessed via the protected `QBuilder` property.

---

## 4. How the Existing DML Builders Work

Read this carefully. `FromObject` must produce identical results to the equivalent manual fluent calls. If you do not understand how the existing code works, you will break it.

### INSERT flow (read `TableBoundInsertBuilder.cs`)

The `Value<TField>(Expression<Func<TTable, TField>> descriptor, object value)` method:

1. Calls `_fnr.GetFieldName(descriptor)` → gets the C# property name as a string, e.g. `"Id"`.
2. Calls `IdentifierQuoter.QuoteIdentifier(col, QBuilder.Dialect)` → gets the quoted column name, e.g. `[Id]`.
3. If parameterized (`_builtQuery != null`):
   - Calls `ConditionMaker.GetParameterName(col, _builtQuery)` → gets a unique parameter name like `@Id0`.
   - Adds `@Id0 → value` to `_builtQuery.Parameters`.
   - Adds `@Id0` to `_valuePlaceholders`.
4. If NOT parameterized (`_builtQuery == null`):
   - Converts value to a SQL literal: strings get wrapped in single-quotes, nulls become the string `"NULL"`, everything else calls `.ToString()`.
   - Adds the literal to `_valuePlaceholders`.
5. In both cases, adds the quoted column name to `_columns`.

`BuildSql()` then joins everything:
```
Insert Into {tableName} (col1, col2, col3) Values (@Id0, @Name1, @IsActive2)
```

### UPDATE flow (read `TableBoundUpdateBuilder.cs`)

`Set<TField>()` works identically to `Value<TField>()` above, but builds `_setClauses` entries like `[Name] = @Name0` instead of separate column and value lists.

`Build()` / `BuildWithParameters()` then validates:
- At least one `.Set()` was called.
- Either `.WhereEqualTo(...)` (or any other WHERE method) was called, OR `.ForEntireTable()` was called.

If neither condition is met, it throws `InvalidOperationException`. **This guard already exists and you must not remove or bypass it.**

### DELETE flow (read `TableBoundDeleteBuilder.cs`)

No columns or values — just WHERE. The same no-WHERE guard as UPDATE.

### How WHERE works in DML builders

`DmlWhereBuilder` is the abstract base class for both `TableBoundUpdateBuilder` and `TableBoundDeleteBuilder`. It holds a private `WhereBuilder _wb` field and a `bool HasWhere` field.

When you call `.WhereEqualTo(u => u.Id, value)`, it:
1. Extracts the field name from the expression: `"Id"`.
2. Calls `_wb.SetNextConjunction("And")`.
3. Calls `_wb.Where<TTable>("Id", FilterOperator.EqualTo, value, _tableName)`.
4. Sets `HasWhere = true`.
5. Returns `Me` (the derived builder, via CRTP).

`_tableName` is the table name **without** schema prefix and **without** alias. For example `"User"`, not `"dbo.User"` and not `"tUser"`. The WHERE clause in DML statements looks like `Where User.Id = @Id0`, not `Where tUser.Id = @Id0` (aliases are for SELECT only).

---

## 5. Design Decisions — Locked, Not Yours to Change

These decisions were made deliberately. Do not deviate from them.

### D1: `FromObject` delegates to the same internal core that the fluent API uses

This is the most important rule. The fluent method (e.g., `.Value(u => u.Id, val)`) and the POCO method (`FromObject(user)`) must produce **identical SQL output** for equivalent inputs. The way to guarantee this is to refactor the fluent method so both paths share a single private core method.

The pattern:
- Extract the body of `Value<TField>()` into a private `ValueCore(string columnName, string paramSeed, object value)`.
- Make `Value<TField>()` call `ValueCore(col, col, value)`.
- Make `FromObject` call `ValueCore(p.ColumnName, p.PropertyName, p.Value)`.

**Do NOT copy-paste the logic from `Value()` into `FromObject()`.** If the logic lives in two places, a future bug fix in one place will not fix the other.

### D2: `[QKey]` properties are INCLUDED in INSERT

`[QKey]` tells the library "this property is the primary key; use it in the WHERE clause for UPDATE and DELETE." It does NOT mean "skip this column in INSERT." Key columns are valid INSERT targets (unless the key is database-generated).

If a developer has an auto-increment / identity column they do not want to INSERT, they must decorate it with `[QIgnore]` — not `[QKey]`. This is documented in section 6 and in the migration guide. Do not add special "do not insert key" logic.

### D3: `[QIgnore]` means skip everywhere — INCLUDING the WHERE clause

A property decorated with `[QIgnore]` is completely invisible to `FromObject`. It does not appear in INSERT columns, SET columns, or WHERE conditions. Even if a property is decorated with both `[QKey]` and `[QIgnore]`, `[QIgnore]` wins — the property is not used anywhere.

**Check `IsIgnored` before checking `IsKey`.** The filtering logic is:
```
if (IsIgnored) → skip entirely
if (IsKey)     → route to WHERE (Update/Delete) or include normally (Insert)
else           → regular column
```

### D4: Anonymous objects are supported but cannot have attributes

Anonymous types (e.g., `new { Name = "Alice", IsActive = true }`) can be passed to `FromObject<T>(T obj)` because C# infers `T` at compile time. However, anonymous types are generated by the compiler and cannot be decorated with attributes.

The behavior for anonymous objects:
- **INSERT** `FromObject(new { Name = "Alice" })` → all properties become column values. Works the same as `Value(u => u.Name, "Alice")`.
- **UPDATE** `FromObject(new { Name = "Alice" })` → all properties go into SET; no WHERE is generated. The existing no-WHERE guard will throw at `.BuildWithParameters()` unless the caller also chains `.WhereEqualTo(...)` manually.
- **DELETE** `FromObject(new { })` (or any anonymous object with only non-key props) → no WHERE is generated. Same guard fires.

For anonymous objects, do NOT throw in `FromObject` even if there are no `[QKey]` properties. The guard in `Build()` / `BuildWithParameters()` already handles the missing-WHERE scenario.

For **named POCO types** (non-anonymous), you MUST throw early in `FromObject` if the operation requires keys but none are found — see section 6.

### D5: Two separate string parameters for column name and parameter seed

When calling `ValueCore` or `SetCore`, two separate strings are passed:
- `columnName` — the raw (unquoted) SQL column name, which may come from `[QColumn("alias")]`.
- `paramSeed` — always the C# property name (`PropertyInfo.Name`), used as the base for `ConditionMaker.GetParameterName(paramSeed, builtQuery)`.

Example: if a property is declared as:
```csharp
[QColumn("user_name")]
public string Name { get; set; }
```
Then:
- `columnName = "user_name"` → SQL becomes `user_name` (or `[user_name]` with SqlServer quoting)
- `paramSeed = "Name"` → parameter becomes `@Name0`

**Why separate?** If we used `"user_name"` as the param seed, parameters would look like `@user_name0`, which is inconsistent with the expression-based path where the same property generates `@Name0`. This would break any test that asserts on parameter names.

### D6: `protected` visibility for `AndEqualToByName` in `DmlWhereBuilder`

The string-based WHERE helper that bypasses expression-tree parsing is `protected`, not `public`. The outside world must never see it. It is only for use by the DML builders' `FromObject` implementations. Do not accidentally mark it `public` or `internal`.

### D7: Cache reflection results in `PocoReflector`

Reflection is expensive when called in a tight loop. `PocoReflector` caches the property descriptors (not the values — values change per instance) the first time a type is encountered. Use `ConcurrentDictionary<Type, IReadOnlyList<PocoPropertyDescriptor>>` for the cache. The compiled getter delegates should also be cached alongside the descriptors.

### D8: The existing fluent API must continue to work identically

After your changes, every existing test must pass without modification. You are ADDING behaviour, not changing any existing behaviour. The signature of `.Value()`, `.Set()`, `.WhereEqualTo()` etc. do not change.

---

## 6. File-by-File Implementation Instructions

### 6.1 New Attribute Files

Create the folder `src/Jattac.Libraries.QBuilder/Attributes/` and create three files inside it.

**File: `Attributes/QKeyAttribute.cs`**

```csharp
namespace Jattac.Libraries.QBuilder.Attributes
{
    using System;

    /// <summary>
    /// Marks a property as the primary key of the table.
    /// When <c>FromObject</c> is called:
    /// - On INSERT: the property is inserted as a normal column.
    /// - On UPDATE: the property value is placed in the WHERE clause, not the SET clause.
    /// - On DELETE: the property value is placed in the WHERE clause.
    /// To skip a database-generated key from INSERT, use <see cref="QIgnoreAttribute"/> instead.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class QKeyAttribute : Attribute
    {
    }
}
```

Nothing more. This is a pure marker — no constructor parameters, no properties. Do not add any.

---

**File: `Attributes/QIgnoreAttribute.cs`**

```csharp
namespace Jattac.Libraries.QBuilder.Attributes
{
    using System;

    /// <summary>
    /// Instructs <c>FromObject</c> to skip this property entirely.
    /// The property will not appear in INSERT columns, SET clauses, or WHERE conditions.
    /// If a property is decorated with both <see cref="QIgnoreAttribute"/> and
    /// <see cref="QKeyAttribute"/>, it is still fully ignored.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class QIgnoreAttribute : Attribute
    {
    }
}
```

---

**File: `Attributes/QColumnAttribute.cs`**

```csharp
namespace Jattac.Libraries.QBuilder.Attributes
{
    using System;

    /// <summary>
    /// Overrides the SQL column name used by <c>FromObject</c>.
    /// When not present, the property name is used as-is.
    /// IMPORTANT: This attribute only affects the <c>FromObject</c> (reflection) path.
    /// The expression-based fluent methods (.Value(u => u.Name, ...), .Set(...), etc.)
    /// always use the C# property name and are unaffected by this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class QColumnAttribute : Attribute
    {
        public string Name { get; }

        public QColumnAttribute(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Column name cannot be null or whitespace.", nameof(name));
            Name = name;
        }
    }
}
```

---

### 6.2 New Helper: PocoReflector

Create `src/Jattac.Libraries.QBuilder/Helpers/PocoReflector.cs`.

This class has three responsibilities:
1. Read the attributes off a type's properties and categorise each property.
2. Cache those descriptors so reflection only runs once per type.
3. Detect whether a type is a C# anonymous type.

Here is the full intended structure. Read every comment — they explain decisions.

```csharp
namespace Jattac.Libraries.QBuilder.Helpers
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.CompilerServices;
    using Jattac.Libraries.QBuilder.Attributes;

    /// <summary>
    /// Reflects the public instance properties of a type and categorises them as
    /// key, ignored, or regular — based on QBuilder attributes.
    /// Results are cached per Type for performance.
    /// </summary>
    internal static class PocoReflector
    {
        // ── Cached descriptor (shape of the type, no values) ─────────────────────────

        internal sealed class PocoPropertyDescriptor
        {
            // The C# property name (PropertyInfo.Name). Always used as the parameter seed.
            internal string PropertyName { get; }

            // The SQL column name. Comes from [QColumn("x")] if present, else same as PropertyName.
            internal string ColumnName { get; }

            internal bool IsKey { get; }
            internal bool IsIgnored { get; }

            // Compiled getter: avoids MethodInfo.Invoke on every call.
            // Signature: object GetValue(object instance)
            internal Func<object, object> GetValue { get; }

            internal PocoPropertyDescriptor(
                string propertyName,
                string columnName,
                bool isKey,
                bool isIgnored,
                Func<object, object> getValue)
            {
                PropertyName = propertyName;
                ColumnName = columnName;
                IsKey = isKey;
                IsIgnored = isIgnored;
                GetValue = getValue;
            }
        }

        // ── Per-call result (descriptor + the value from a specific instance) ────────

        internal sealed class PocoProperty
        {
            internal string PropertyName { get; }
            internal string ColumnName { get; }
            internal bool IsKey { get; }
            internal bool IsIgnored { get; }
            internal object Value { get; }

            internal PocoProperty(PocoPropertyDescriptor descriptor, object value)
            {
                PropertyName = descriptor.PropertyName;
                ColumnName = descriptor.ColumnName;
                IsKey = descriptor.IsKey;
                IsIgnored = descriptor.IsIgnored;
                Value = value;
            }
        }

        // ── Cache ────────────────────────────────────────────────────────────────────

        private static readonly ConcurrentDictionary<Type, IReadOnlyList<PocoPropertyDescriptor>> _cache
            = new ConcurrentDictionary<Type, IReadOnlyList<PocoPropertyDescriptor>>();

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the properties of <typeparamref name="T"/> with values populated from
        /// <paramref name="instance"/>. Descriptors are cached; values are read on every call.
        /// </summary>
        internal static IReadOnlyList<PocoProperty> GetProperties<T>(T instance)
        {
            var descriptors = _cache.GetOrAdd(typeof(T), BuildDescriptors);
            var result = new List<PocoProperty>(descriptors.Count);
            foreach (var d in descriptors)
                result.Add(new PocoProperty(d, d.GetValue(instance)));
            return result;
        }

        /// <summary>
        /// Returns true if <paramref name="t"/> is a C# anonymous type.
        /// Anonymous types: compiler-generated, sealed, not public, name contains "AnonymousType".
        /// This heuristic is reliable for all current C# compiler versions.
        /// </summary>
        internal static bool IsAnonymousType(Type t) =>
            t.GetCustomAttributes(typeof(CompilerGeneratedAttribute), false).Length > 0
            && t.IsSealed
            && !t.IsPublic
            && t.Name.Contains("AnonymousType");

        // ── Private ──────────────────────────────────────────────────────────────────

        private static IReadOnlyList<PocoPropertyDescriptor> BuildDescriptors(Type type)
        {
            // Only public instance properties with a readable getter.
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var descriptors = new List<PocoPropertyDescriptor>(props.Length);

            foreach (var prop in props)
            {
                if (!prop.CanRead) continue;

                var isIgnored = prop.GetCustomAttribute<QIgnoreAttribute>() != null;
                var isKey     = prop.GetCustomAttribute<QKeyAttribute>()    != null;
                var colAttr   = prop.GetCustomAttribute<QColumnAttribute>();
                var colName   = colAttr != null ? colAttr.Name : prop.Name;

                // Compile a getter delegate once and cache it.
                // Using CreateDelegate is significantly faster than MethodInfo.Invoke.
                var getter = (Func<object, object>)Delegate.CreateDelegate(
                    typeof(Func<object, object>),
                    null,
                    // Box the return value to object.
                    // For value types this creates a small heap allocation per call,
                    // which is acceptable — the alternative is more complexity.
                    MakeObjectGetter(prop));

                descriptors.Add(new PocoPropertyDescriptor(
                    prop.Name,
                    colName,
                    isKey,
                    isIgnored,
                    getter));
            }

            return descriptors;
        }

        private static MethodInfo MakeObjectGetter(PropertyInfo prop)
        {
            // We need a MethodInfo that takes (object) and returns (object).
            // The property getter is typed (e.g. Func<User, Guid>), so we wrap it.
            // The cleanest way in .NET Standard / .NET 6 without Expression trees:
            // create a small lambda and get its MethodInfo via reflection.
            // But for simplicity and compatibility, just use a captured lambda.
            // This method is only called once per property during cache population.

            // Note: the Delegate.CreateDelegate path above does NOT work directly for
            // typed getters because of covariance limitations. Use a lambda capture instead.
            throw new NotImplementedException(
                "See implementation note below — replace the Delegate.CreateDelegate call above " +
                "with the lambda approach shown here.");
        }
    }
}
```

**STOP. The `MakeObjectGetter` / compiled delegate approach above is intentionally left incomplete** to prevent copy-paste errors. Here is the correct, simple implementation of `BuildDescriptors` that you should use instead of the overly complex delegate approach. Replace the entire `BuildDescriptors` method with:

```csharp
private static IReadOnlyList<PocoPropertyDescriptor> BuildDescriptors(Type type)
{
    var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
    var descriptors = new List<PocoPropertyDescriptor>(props.Length);

    foreach (var prop in props)
    {
        if (!prop.CanRead) continue;

        var isIgnored = prop.GetCustomAttribute<QIgnoreAttribute>() != null;
        var isKey     = prop.GetCustomAttribute<QKeyAttribute>()    != null;
        var colAttr   = prop.GetCustomAttribute<QColumnAttribute>();
        var colName   = colAttr != null ? colAttr.Name : prop.Name;

        // Capture prop in a local variable to avoid closure-capture-loop bug.
        var capturedProp = prop;
        Func<object, object> getter = instance => capturedProp.GetValue(instance);

        descriptors.Add(new PocoPropertyDescriptor(
            prop.Name,
            colName,
            isKey,
            isIgnored,
            getter));
    }

    return descriptors;
}
```

This is simpler and correct. `prop.GetValue(instance)` calls the CLR property getter. It boxes value types (e.g., `Guid`, `int`, `bool`) into `object`. This is fine — the existing `Value()` and `Set()` methods already receive `object` and the same boxing happens there too.

**Critical closure-capture warning:** In the loop above, `capturedProp` is declared inside the loop body, meaning each iteration captures its own `prop`. If you write `Func<object, object> getter = instance => prop.GetValue(instance)` with `prop` declared in the `foreach` header, some older C# compilers would capture the same variable reference for all iterations, meaning all getters read the last property. Declaring `var capturedProp = prop` inside the loop body prevents this. In modern C# (C# 5+) `foreach` loop variables are captured per-iteration, but the explicit capture makes the intent clear and is safe across all versions.

---

### 6.3 Modify TableBoundInsertBuilder

File: `src/Jattac.Libraries.QBuilder/Builders/DML/TableBoundInsertBuilder.cs`

**Step 1: Add the using directive** at the top:
```csharp
using Jattac.Libraries.QBuilder.Helpers; // already present
using System.Linq;                        // add this for any LINQ used in FromObject
```
You also need to reference `PocoReflector`, which is in the same `Helpers` namespace, so no extra using is needed beyond what is already there.

**Step 2: Extract `ValueCore`.**

Right now `Value<TField>()` contains all the logic inline. You must extract that logic into a new private method named `ValueCore`. Here is exactly what the two methods should look like after the refactor:

```csharp
/// <summary>
/// Adds a column-value pair to the INSERT statement.
/// </summary>
public TableBoundInsertBuilder<TTable> Value<TField>(Expression<Func<TTable, TField>> descriptor, object value)
{
    var col = _fnr.GetFieldName(descriptor);  // extracts "Id" from u => u.Id
    return ValueCore(col, col, value);
}

/// <summary>
/// Core insert logic shared by the expression-based Value() and the reflection-based FromObject().
/// </summary>
/// <param name="columnName">The SQL column name (may be overridden by [QColumn]).</param>
/// <param name="paramSeed">The C# property name used as the base for parameter name generation.</param>
/// <param name="value">The value to insert.</param>
private TableBoundInsertBuilder<TTable> ValueCore(string columnName, string paramSeed, object value)
{
    _columns.Add(IdentifierQuoter.QuoteIdentifier(columnName, QBuilder.Dialect));
    if (_builtQuery != null)
    {
        var paramName = ConditionMaker.GetParameterName(paramSeed, _builtQuery);
        _builtQuery.Parameters.Add(paramName, value);
        _valuePlaceholders.Add(paramName);
    }
    else
    {
        var literal = value is string s ? $"'{s}'" : value?.ToString() ?? "NULL";
        _valuePlaceholders.Add(literal);
    }
    return this;
}
```

**Do not change anything else in the file** — `Build()`, `BuildWithParameters()`, `BuildSql()`, and `Validate()` remain exactly as they are.

**Step 3: Add `FromObject<T>`.**

Add this public method after `Value<TField>()`:

```csharp
/// <summary>
/// Populates the INSERT statement from a POCO or anonymous object by reflecting its properties.
/// All public instance properties are included unless decorated with <c>[QIgnore]</c>.
/// Properties decorated with <c>[QKey]</c> are included as normal INSERT columns.
/// The column name in SQL is taken from <c>[QColumn("name")]</c> if present, otherwise
/// the C# property name is used.
/// </summary>
public TableBoundInsertBuilder<TTable> FromObject<T>(T obj)
{
    Guard.NotNull(obj, nameof(obj));
    foreach (var p in PocoReflector.GetProperties(obj))
    {
        if (!p.IsIgnored)
            ValueCore(p.ColumnName, p.PropertyName, p.Value);
    }
    return this;
}
```

Note that `Guard.NotNull` is already imported (it is in `Jattac.Libraries.QBuilder.Helpers`). The `obj` parameter name is the string passed to `ArgumentNullException` — use `nameof(obj)`.

---

### 6.4 Modify DmlWhereBuilder

File: `src/Jattac.Libraries.QBuilder/Builders/DML/DmlWhereBuilder.cs`

Add ONE new `protected` method. Place it in the `// ─── core helpers ───` section, just below the existing private `ExistsOr` method and before the `// ─── parentheses ───` section.

```csharp
/// <summary>
/// Adds an AND WHERE [column] = [value] condition using a raw column name string
/// rather than an expression tree. For internal use by FromObject only.
/// </summary>
/// <param name="columnName">The raw (unquoted) column name.</param>
/// <param name="value">The value to compare against.</param>
protected TBuilder AndEqualToByName(string columnName, object value)
{
    _wb.SetNextConjunction("And");
    _wb.Where<TTable>(columnName, FilterOperator.EqualTo, value, _tableName);
    HasWhere = true;
    return Me;
}
```

**Why `protected` and not `public`?** This method bypasses the expression-tree safety layer. The expression layer (`u => u.Id`) is what prevents developers from accidentally typing a wrong column name — the compiler checks it. The string-based version has no such compiler check. Making it `protected` means it can only be called from within this class hierarchy (i.e., from `FromObject` in the derived builders), not from external code. It must never be `public`.

**Do not change any existing methods.** Do not rename `_wb` to anything. Do not change visibility of `HasWhere` or `_tableName`.

---

### 6.5 Modify TableBoundUpdateBuilder

File: `src/Jattac.Libraries.QBuilder/Builders/DML/TableBoundUpdateBuilder.cs`

Add the using directive at the top:
```csharp
using System.Linq;  // for .Where() and .ToList() in FromObject
```

**Step 1: Extract `SetCore`.**

```csharp
/// <summary>
/// Adds a SET assignment: column = value.
/// </summary>
public TableBoundUpdateBuilder<TTable> Set<TField>(Expression<Func<TTable, TField>> descriptor, object value)
{
    var col = Fnr.GetFieldName(descriptor);
    return SetCore(col, col, value);
}

/// <summary>
/// Core SET logic shared by the expression-based Set() and the reflection-based FromObject().
/// </summary>
private TableBoundUpdateBuilder<TTable> SetCore(string columnName, string paramSeed, object value)
{
    var quotedCol = IdentifierQuoter.QuoteIdentifier(columnName, QBuilder.Dialect);
    if (_builtQuery != null)
    {
        var paramName = ConditionMaker.GetParameterName(paramSeed, _builtQuery);
        _builtQuery.Parameters.Add(paramName, value);
        _setClauses.Add($"{quotedCol} = {paramName}");
    }
    else
    {
        var literal = value is string s ? $"'{s}'" : value?.ToString() ?? "NULL";
        _setClauses.Add($"{quotedCol} = {literal}");
    }
    return this;
}
```

**Step 2: Add `FromObject<T>`.**

```csharp
/// <summary>
/// Populates the UPDATE statement from a POCO or anonymous object.
/// For named POCO types: properties with [QKey] go to the WHERE clause;
/// remaining non-ignored properties go to the SET clause.
/// For anonymous objects: all properties go to SET; no WHERE is generated
/// (add WHERE conditions manually via .WhereEqualTo(...) etc.).
/// Throws if a named (non-anonymous) POCO has no [QKey] property.
/// </summary>
public TableBoundUpdateBuilder<TTable> FromObject<T>(T obj)
{
    Guard.NotNull(obj, nameof(obj));

    var props = PocoReflector.GetProperties(obj);
    var isAnonymous = PocoReflector.IsAnonymousType(typeof(T));

    var keys    = new List<PocoReflector.PocoProperty>();
    var columns = new List<PocoReflector.PocoProperty>();

    foreach (var p in props)
    {
        if (p.IsIgnored) continue;
        if (p.IsKey) keys.Add(p);
        else columns.Add(p);
    }

    if (!isAnonymous && keys.Count == 0)
        Guard.Against(true,
            "FromObject on UPDATE requires at least one property decorated with [QKey] " +
            "to generate the WHERE clause. Decorate the primary key with [QKey], " +
            "or add WHERE conditions manually using .WhereEqualTo(...) etc.");

    foreach (var p in columns)
        SetCore(p.ColumnName, p.PropertyName, p.Value);

    foreach (var p in keys)
        AndEqualToByName(p.ColumnName, p.Value);

    return this;
}
```

**Note on `PocoReflector.PocoProperty` visibility:** `PocoProperty` is an `internal sealed class` on `PocoReflector`. Since `TableBoundUpdateBuilder` is in the same assembly, it can access it. If the compiler complains, move `PocoProperty` and `PocoPropertyDescriptor` out of the `PocoReflector` class as top-level `internal` classes in the same file. Do not make them `public`.

**Note on `LINQ` usage in `FromObject`:** The example above uses an explicit `foreach` loop with `if (p.IsIgnored)` checks instead of `.Where(p => !p.IsIgnored && p.IsKey).ToList()`. Either approach is fine; the explicit loop is shown here because it is easier to follow. If you prefer LINQ, that is also acceptable. Do not mix both in the same method.

---

### 6.6 Modify TableBoundDeleteBuilder

File: `src/Jattac.Libraries.QBuilder/Builders/DML/TableBoundDeleteBuilder.cs`

No core method refactor needed for DELETE (there are no value-setting methods to extract). Just add `FromObject<T>`.

```csharp
/// <summary>
/// Populates the WHERE clause from the [QKey] properties of a POCO.
/// Non-key and non-ignored properties are discarded — DELETE only needs the key.
/// For anonymous objects: no WHERE is generated (there are no [QKey] attributes on anonymous types);
/// the existing no-WHERE guard will fire at .Build() / .BuildWithParameters() unless
/// you chain .WhereEqualTo(...) manually.
/// Throws if a named (non-anonymous) POCO has no [QKey] property.
/// </summary>
public TableBoundDeleteBuilder<TTable> FromObject<T>(T obj)
{
    Guard.NotNull(obj, nameof(obj));

    var props = PocoReflector.GetProperties(obj);
    var isAnonymous = PocoReflector.IsAnonymousType(typeof(T));

    var keys = new List<PocoReflector.PocoProperty>();
    foreach (var p in props)
    {
        if (!p.IsIgnored && p.IsKey)
            keys.Add(p);
    }

    if (!isAnonymous && keys.Count == 0)
        Guard.Against(true,
            "FromObject on DELETE requires at least one property decorated with [QKey] " +
            "to generate the WHERE clause. Decorate the primary key with [QKey], " +
            "or add WHERE conditions manually using .WhereEqualTo(...) etc.");

    foreach (var p in keys)
        AndEqualToByName(p.ColumnName, p.Value);

    return this;
}
```

---

### 6.7 Modify QBuilder — Shorthand Methods

File: `src/Jattac.Libraries.QBuilder/QBuilder.cs`

Add three methods alongside the existing `UseTableBoundInsert`, `UseTableBoundUpdate`, `UseTableBoundDelete` methods. Place them immediately after those methods in the file.

```csharp
/// <summary>
/// Convenience shorthand for <c>UseTableBoundInsert&lt;TTable&gt;().FromObject(obj)</c>.
/// </summary>
public TableBoundInsertBuilder<TTable> InsertFrom<TTable, T>(T obj) =>
    UseTableBoundInsert<TTable>().FromObject(obj);

/// <summary>
/// Convenience shorthand for <c>UseTableBoundUpdate&lt;TTable&gt;().FromObject(obj)</c>.
/// </summary>
public TableBoundUpdateBuilder<TTable> UpdateFrom<TTable, T>(T obj) =>
    UseTableBoundUpdate<TTable>().FromObject(obj);

/// <summary>
/// Convenience shorthand for <c>UseTableBoundDelete&lt;TTable&gt;().FromObject(obj)</c>.
/// </summary>
public TableBoundDeleteBuilder<TTable> DeleteFrom<TTable, T>(T obj) =>
    UseTableBoundDelete<TTable>().FromObject(obj);
```

**Type inference note:** C# cannot partially infer generic type arguments — if you specify one, you must specify all. These methods have two type parameters: `TTable` (the table type) and `T` (the POCO/anonymous type). When the POCO type is the same as the table type, callers write:

```csharp
Q.New().InsertFrom<User, User>(user)   // explicit, verbose
```

However, if the caller already has a `User` instance and the type matches, they can also use the longer form via the builder:
```csharp
Q.New().UseTableBoundInsert<User>().FromObject(user)  // T is inferred as User
```

This is fine. The shorthand methods are a nice-to-have. Do not introduce overloading tricks or additional type parameters to work around the inference limitation.

---

## 7. Tests You Must Write

### Test models

Before writing tests, add the following test POCO to the tests project. Either add it as a new file or add it to the existing `Models/` folder:

```csharp
// tests/Jattac.Libraries.QBuilder/Models/UserWithAttributes.cs
namespace Jattac.QBuilderTests.Models
{
    using Jattac.Libraries.QBuilder.Attributes;
    using System;

    internal class UserWithKey
    {
        [QKey]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    internal class UserWithIgnore
    {
        [QKey]
        public Guid Id { get; set; }
        public string Name { get; set; }

        [QIgnore]
        public bool IsActive { get; set; }          // should never appear in SQL
    }

    internal class UserWithColumnAlias
    {
        [QKey]
        public Guid Id { get; set; }

        [QColumn("user_name")]
        public string Name { get; set; }             // SQL should say "user_name", not "Name"
    }

    internal class UserWithMultiKey
    {
        [QKey]
        public Guid TenantId { get; set; }
        [QKey]
        public Guid UserId { get; set; }
        public string Name { get; set; }
    }

    internal class UserNoKey                         // no [QKey] — used to test the error guard
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
```

---

### `tests/Jattac.Libraries.QBuilder/DML/PocoTests.cs`

This file contains unit tests that do not hit a database. They assert on the generated SQL string and the `Parameters` dictionary.

Write `[Fact]` methods for ALL of the following scenarios. Use `xUnit`. Look at `DmlValidationTests.cs` for the test style used in this project.

**INSERT tests:**

| Test method name | What to assert |
|---|---|
| `Insert_FromObject_AllProperties_AreIncludedInSql` | SQL contains `Id`, `Name`, `IsActive`, `DeletedAt`; `Parameters` has 4 entries |
| `Insert_FromObject_QIgnore_PropertyIsExcluded` | `IsActive` absent from SQL and `Parameters` |
| `Insert_FromObject_QColumn_SqlUsesAliasName` | SQL contains `user_name`; SQL does not contain `Name`; param is `@Name0` not `@user_name0` |
| `Insert_FromObject_QKey_IsIncludedAsNormalColumn` | `Id` appears in the INSERT column list |
| `Insert_FromObject_AnonymousObject_AllPropertiesIncluded` | `new { Name = "Alice", IsActive = true }` → both columns in SQL |
| `Insert_FromObject_NullObject_ThrowsArgumentNullException` | `Guard.NotNull` fires |
| `Insert_FromObject_DialectSqlServer_ColumnNamesAreBracketed` | `[Id]`, `[Name]` in SQL |
| `Insert_FromObject_DialectMySql_ColumnNamesAreBackticked` | `` `Id` ``, `` `Name` `` in SQL |
| `Insert_FromObject_ThenManualValue_BothAppear` | `.FromObject(user).Value(u => u.Extra, "x")` → all columns present |
| `Insert_FromObject_NonParameterized_ProducesLiterals` | Non-parameterized build; string values wrapped in single quotes |

**UPDATE tests:**

| Test method name | What to assert |
|---|---|
| `Update_FromObject_KeyGoesToWhere_RestGoesToSet` | SQL has `Set Name =` and `Where Id =`; `Id` is not in SET |
| `Update_FromObject_MultiKey_BothKeysInWhere` | WHERE has `TenantId` AND `UserId` |
| `Update_FromObject_QIgnore_AbsentFromSetAndWhere` | `IsActive` not in SQL at all |
| `Update_FromObject_QColumn_SqlUsesAlias` | `user_name` in SET clause |
| `Update_FromObject_AnonymousObject_AllPropsGoToSet` | All props in SET; no WHERE clause fragment |
| `Update_FromObject_AnonymousObject_NoWhereChained_ThrowsAtBuild` | `.FromObject(new { Name="x" }).BuildWithParameters()` throws because no WHERE |
| `Update_FromObject_AnonymousObject_WithFluentWhere_Succeeds` | `.FromObject(new { Name="x" }).WhereEqualTo(u => u.Id, id)` → valid SQL |
| `Update_FromObject_NoKeyOnNamedType_ThrowsImmediately` | `UserNoKey` → `InvalidOperationException` from `FromObject` itself, before `BuildWithParameters` |
| `Update_FromObject_NullObject_ThrowsArgumentNullException` | Guard fires |

**DELETE tests:**

| Test method name | What to assert |
|---|---|
| `Delete_FromObject_KeyGoesToWhere` | SQL has `Where` with `Id` |
| `Delete_FromObject_MultiKey_BothKeysInWhere` | WHERE has both keys |
| `Delete_FromObject_NonKeyPropertiesAreDiscarded` | `Name` does not appear in SQL |
| `Delete_FromObject_QIgnore_KeyIgnored_ThrowsIfNoOtherKey` | `[QKey, QIgnore]` on Id → no keys remain → throws |
| `Delete_FromObject_NoKeyOnNamedType_ThrowsImmediately` | `UserNoKey` → `InvalidOperationException` |
| `Delete_FromObject_AnonymousObject_NoWhere_ThrowsAtBuild` | `.FromObject(new { Name="x" }).BuildWithParameters()` → throws (no WHERE guard) |

---

### `tests/Jattac.Libraries.QBuilder/DML/Integration/DmlPocoIntegrationTests.cs`

Look at `DmlIntegrationTests.cs` for the exact setup pattern (SQLite in-memory database, table creation in the constructor, Dapper for execution). Copy that pattern exactly.

Write the following `[Fact]` methods:

| Test | What to do |
|---|---|
| `Insert_FromObject_RowIsInsertedCorrectly` | Insert a `UserWithKey` via `FromObject`, then SELECT it back and assert all field values match |
| `Update_FromObject_OnlyTargetRowChanged` | Insert two rows; update one via `FromObject`; verify only that row changed |
| `Delete_FromObject_OnlyTargetRowRemoved` | Insert two rows; delete one via `FromObject`; verify only that row is gone |
| `Update_FromObject_QIgnore_FieldRetainsOriginalValue` | Insert with `Name = "Alice"` and `IsActive = true`; update via `FromObject` of `UserWithIgnore` (which has `[QIgnore]` on `IsActive`); verify `IsActive` in DB is still `true` |
| `Insert_FromObject_QColumn_ColumnMappedCorrectly` | Use a table schema where the column is named `user_name`; insert via `UserWithColumnAlias.FromObject`; verify data stored under `user_name` |

---

### `tests/Jattac.Libraries.QBuilder/Helpers/PocoReflectorTests.cs`

| Test | Assert |
|---|---|
| `GetProperties_NoAttributes_ColumnNameEqualsPropertyName` | `p.ColumnName == p.PropertyName` for all properties |
| `GetProperties_QKey_IsKeyTrue` | `Id` property: `IsKey == true` |
| `GetProperties_QIgnore_IsIgnoredTrue` | Decorated property: `IsIgnored == true` |
| `GetProperties_QColumn_ColumnNameOverridden` | `ColumnName == "user_name"`, `PropertyName == "Name"` |
| `GetProperties_QKeyAndQIgnore_BothFlagsSet` | Both `IsKey` and `IsIgnored` are true |
| `GetProperties_CalledTwiceForSameType_ReturnsSameDescriptors` | Cache is hit; reference equality of descriptor list |
| `IsAnonymousType_AnonymousObject_ReturnsTrue` | `IsAnonymousType(new { }.GetType())` → `true` |
| `IsAnonymousType_NamedClass_ReturnsFalse` | `IsAnonymousType(typeof(User))` → `false` |
| `GetProperties_AnonymousObject_AllPropsNotKeyNotIgnored` | All properties: `IsKey == false`, `IsIgnored == false` |
| `GetProperties_ValueTypeProp_ValueBoxedCorrectly` | `bool IsActive = true` → `Value` is `(object)true` |

---

## 8. Migration Guide Update

File: `docs/migration-v7-to-v8.md`

Append a new section **at the very end of the file**:

```markdown
---

## POCO shortcuts (new in v9.2)

Prior versions required one explicit expression-based call per column when performing DML:

\```csharp
// Before v9.2 — required for every property
Q.New()
    .UseTableBoundInsert<User>()
    .Value(u => u.Id, user.Id)
    .Value(u => u.Name, user.Name)
    .Value(u => u.IsActive, user.IsActive)
    .BuildWithParameters();
\```

v9.2 adds `FromObject<T>(T obj)` to all three DML builders. Pass the entire object and the
library reflects its properties automatically:

\```csharp
// v9.2
Q.New()
    .UseTableBoundInsert<User>()
    .FromObject(user)
    .BuildWithParameters();
\```

The fluent API still works and is not deprecated.

### New attributes

Decorate your POCO properties to control how `FromObject` handles them:

| Attribute | Effect |
|---|---|
| `[QKey]` | Marks the primary key. UPDATE and DELETE route this property to the WHERE clause; INSERT treats it as a normal column. |
| `[QIgnore]` | Skip this property in all `FromObject` operations (INSERT, SET, WHERE). |
| `[QColumn("name")]` | Override the SQL column name. Only affects the `FromObject` path; fluent methods always use the C# property name. |

\```csharp
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
\```

### INSERT

All non-`[QIgnore]` properties are included. `[QKey]` properties are inserted as normal values.

\```csharp
Q.New()
    .UseTableBoundInsert<User>()
    .FromObject(user)
    .BuildWithParameters();
// → Insert Into User (Id, user_name) Values (@Id0, @Name0)
\```

### UPDATE

`[QKey]` properties go to WHERE; all other non-ignored properties go to SET.

\```csharp
Q.New()
    .UseTableBoundUpdate<User>()
    .FromObject(user)
    .BuildWithParameters();
// → Update User Set user_name = @Name0 Where User.Id = @Id0
\```

### DELETE

Only `[QKey]` properties are used (for the WHERE clause). All other properties are ignored.

\```csharp
Q.New()
    .UseTableBoundDelete<User>()
    .FromObject(user)
    .BuildWithParameters();
// → Delete From User Where User.Id = @Id0
\```

### Anonymous objects

Anonymous objects work with `FromObject`. They cannot have attributes, so all properties are
treated as regular values. For UPDATE and DELETE, chain the WHERE clause manually:

\```csharp
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
\```

### Shorthand methods on QBuilder

\```csharp
// Equivalent to UseTableBoundInsert<User>().FromObject(user)
Q.New().InsertFrom<User, User>(user).BuildWithParameters();
Q.New().UpdateFrom<User, User>(user).BuildWithParameters();
Q.New().DeleteFrom<User, User>(user).BuildWithParameters();
\```

### Limitation: database-generated keys

If your primary key is auto-generated by the database (e.g., SQL Server `IDENTITY`, PostgreSQL
`SERIAL`), do not use `[QKey]` — use `[QIgnore]` to skip the key from INSERT and chain WHERE
manually for UPDATE and DELETE:

\```csharp
public class Product
{
    [QIgnore]                             // auto-generated; skip from INSERT
    public int Id { get; set; }           // cannot use [QKey] — would include in WHERE
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
\```

A future release will introduce `[QKey(Insertable = false)]` to handle this automatically.
```

---

## 9. What Success Looks Like

Run the test suite:

```
dotnet test tests/Jattac.Libraries.QBuilder
```

All of the following must be true:

1. **Zero existing tests break.** If any pre-existing test fails after your changes, you have broken the existing API. Find the regression and fix it before continuing.
2. **All new unit tests in `PocoTests.cs` pass.**
3. **All new integration tests in `DmlPocoIntegrationTests.cs` pass.**
4. **All new helper tests in `PocoReflectorTests.cs` pass.**
5. **The project builds with zero warnings** (treat warnings as errors if the project is configured that way).

---

## 10. Pitfalls — Read Before You Write a Single Line

These are mistakes a developer unfamiliar with this codebase is likely to make. Read all of them.

### Pitfall 1: Copying logic instead of extracting it

If you copy the body of `Value()` into `FromObject()` instead of extracting `ValueCore()`, you will have the same logic in two places. The next time a bug is fixed in one, the other will not be fixed. The design requires a single shared method. Check: after your change, `Value()` should have only one line of real logic — extracting the property name and calling `ValueCore`.

### Pitfall 2: Using the column name as the parameter seed

The parameter name (`@Name0`) must always be seeded from the **C# property name** (`"Name"`), never from the SQL column name alias (`"user_name"`). If you accidentally pass `p.ColumnName` as the second argument to `ValueCore`, then with `[QColumn("user_name")]` the parameter becomes `@user_name0`, which differs from what the expression-based path produces for the same property. Tests will catch this, but understand WHY before you write it.

### Pitfall 3: Excluding `[QKey]` from INSERT

`[QKey]` does NOT mean "skip from INSERT." It means "this is the primary key — use it in WHERE for UPDATE/DELETE." In INSERT, every non-ignored column must appear. If you add an `if (p.IsKey) continue;` inside the INSERT `FromObject`, you will break the correct behaviour for callers who have application-generated UUIDs as primary keys.

### Pitfall 4: Throwing for anonymous objects with no key

Anonymous objects cannot have `[QKey]`. If you throw `InvalidOperationException` for anonymous objects that have no key properties, you will break the intended use case of anonymous INSERT and will also prevent the "anonymous UPDATE with manual WHERE" pattern. Only throw if the type is a named (non-anonymous) POCO with no `[QKey]`.

### Pitfall 5: Making `AndEqualToByName` public

This method bypasses the expression-tree compile-time column name checking. If it is `public`, external developers will call it with hand-written strings, which opens the door to typos that the compiler cannot catch. It must be `protected`. The public surface of the library uses expressions only.

### Pitfall 6: Forgetting that `_builtQuery` can be null

Both `ValueCore` and `SetCore` have two branches: parameterized (`_builtQuery != null`) and non-parameterized (`_builtQuery == null`). The non-parameterized path produces SQL literals (string values in single quotes, nulls as the word `NULL`). You must handle both branches. Look at the original `Value()` and `Set()` methods — both branches are already there. Your refactored `ValueCore`/`SetCore` must preserve both.

### Pitfall 7: Forgetting the closure-capture bug in the property loop

In `PocoReflector.BuildDescriptors`, when creating the getter lambda inside a `foreach` loop, always declare a local variable inside the loop body (`var capturedProp = prop;`) and capture that. Do not capture the loop variable directly. Although modern C# handles this correctly for `foreach`, the explicit capture makes intent clear and is the safe pattern.

### Pitfall 8: Including write-only or non-readable properties

`GetProperties` should skip any property where `prop.CanRead == false`. A write-only property (one with a setter but no getter) would throw `TargetException` at runtime when you call `prop.GetValue(instance)`. Check `CanRead` before adding the property to the descriptor list.

### Pitfall 9: Not testing the no-WHERE guard for anonymous UPDATE/DELETE

The expected behaviour when an anonymous object is passed to UPDATE or DELETE `FromObject` — and no fluent WHERE is chained — is that the **existing guard** in `Build()`/`BuildWithParameters()` throws at build time, not at `FromObject` call time. Write a test for this. Do not add an early throw in `FromObject` for anonymous objects; let the existing guard handle it. If you add an early throw, you also break the case where a developer uses `FromObject(anonymousObj).WhereEqualTo(...)`, which is valid.

### Pitfall 10: `[QIgnore]` must win over `[QKey]`

If a property has both `[QKey]` and `[QIgnore]`, it must be completely skipped everywhere — including the WHERE clause. The check order must be: if ignored → skip → else if key → add to keys → else → add to columns. Do not check `IsKey` before `IsIgnored`.

### Pitfall 11: Do not touch `WhereBuilder.cs` or `BuiltQuery.cs`

The new `AndEqualToByName` method in `DmlWhereBuilder` calls `_wb.Where<TTable>(...)`, which is a method that already exists on `WhereBuilder`. You do not need to modify `WhereBuilder`. If you find yourself editing `WhereBuilder.cs` or `BuiltQuery.cs`, stop and reconsider — you have gone off-track.

### Pitfall 12: Running only new tests, not the full suite

After every significant change, run `dotnet test tests/Jattac.Libraries.QBuilder` — the **entire** suite, not just the new test file. A regression in an existing test means you have broken the existing API.

---

## 11. Bulk INSERT — `FromObjects`

### What it is and why it matters

Single-row INSERT via `FromObject(user)` issues one `INSERT INTO ... VALUES (...)` per call. When inserting tens or hundreds of rows, calling `FromObject` in a loop and executing each statement separately causes one database round-trip per row. SQL supports inserting multiple rows in a single statement:

```sql
INSERT INTO User (Id, Name, IsActive) VALUES
    (@Id0, @Name0, @IsActive0),
    (@Id1, @Name1, @IsActive1),
    (@Id2, @Name2, @IsActive2)
```

This is significantly faster than three separate INSERT statements because it is one round-trip and one transaction boundary. The `FromObjects<T>(IEnumerable<T> items)` method generates this multi-row VALUES syntax from a collection of POCOs.

**Support across databases:** Multi-row `INSERT ... VALUES (...),(...)` syntax is supported by SQL Server 2008+, MySQL, MariaDB, PostgreSQL, and SQLite. All dialects supported by this library support it. You do not need to check the dialect.

---

### New state on `TableBoundInsertBuilder<TTable>`

Add one new private field to `TableBoundInsertBuilder<TTable>`:

```csharp
private readonly List<List<string>> _extraValueRows = new List<List<string>>();
```

**Why a separate list?** The existing `_valuePlaceholders` holds the placeholders for the first (or only) row. `_extraValueRows` holds placeholder lists for every additional row beyond the first. Keeping them separate means the existing single-row code path (`Value()`, `FromObject()`, `BuildSql()`) does not need to change until the very end of `BuildSql()`, minimising the risk of regressions.

---

### New private method: `BuildRowPlaceholders`

Add this private method to `TableBoundInsertBuilder<TTable>`. It is called by `FromObjects` for every item after the first.

```csharp
/// <summary>
/// Builds the value-placeholder list for a single additional row in a bulk INSERT.
/// Does NOT add to _columns — columns are set once from the first item.
/// </summary>
private List<string> BuildRowPlaceholders(IReadOnlyList<PocoReflector.PocoProperty> props)
{
    var rowPlaceholders = new List<string>();
    foreach (var p in props)
    {
        if (p.IsIgnored) continue;
        if (_builtQuery != null)
        {
            // Re-use the same BuiltQuery so ConditionMaker's collision avoidance
            // sees all parameters already registered by previous rows and picks unique names.
            var paramName = ConditionMaker.GetParameterName(p.PropertyName, _builtQuery);
            _builtQuery.Parameters.Add(paramName, p.Value);
            rowPlaceholders.Add(paramName);
        }
        else
        {
            var literal = p.Value is string s ? $"'{s}'" : p.Value?.ToString() ?? "NULL";
            rowPlaceholders.Add(literal);
        }
    }
    return rowPlaceholders;
}
```

**Critical detail:** `ConditionMaker.GetParameterName(p.PropertyName, _builtQuery)` takes the existing `_builtQuery` — the same object used by rows 1, 2, 3, etc. Because all parameters are stored in the same dictionary, `GetParameterName` sees `@Id0` from row 1 and generates `@Id1` for row 2, `@Id2` for row 3, and so on. This collision avoidance is automatic and correct. Do not create a new `BuiltQuery` per row.

---

### New public method: `FromObjects<T>`

Add this public method to `TableBoundInsertBuilder<TTable>`:

```csharp
/// <summary>
/// Populates a multi-row INSERT from a collection of POCOs or anonymous objects.
/// Generates: INSERT INTO table (col1, col2) VALUES (...), (...), (...)
/// All items must be the same concrete type T.
/// Column structure is derived from the first item; subsequent items must have the same
/// non-ignored properties in the same order. Do NOT mix types in a single call.
/// </summary>
public TableBoundInsertBuilder<TTable> FromObjects<T>(IEnumerable<T> items)
{
    Guard.NotNull(items, nameof(items));
    // Materialise once so we can check count and iterate safely.
    var list = items as IList<T> ?? new List<T>(items);
    Guard.Against(list.Count == 0,
        "FromObjects requires at least one item. The collection was empty.");

    bool isFirst = true;
    foreach (var item in list)
    {
        if (isFirst)
        {
            // The first item is handled exactly like a single FromObject call.
            // This fills _columns and _valuePlaceholders exactly as they would be
            // for a standard single-row insert, keeping the code DRY.
            FromObject(item);
            isFirst = false;
        }
        else
        {
            // Subsequent items: only add new value rows — columns are already set.
            var props = PocoReflector.GetProperties(item);
            _extraValueRows.Add(BuildRowPlaceholders(props));
        }
    }
    return this;
}
```

---

### Modify `BuildSql()` to emit multi-row VALUES

The existing `BuildSql()` in `TableBoundInsertBuilder<TTable>` is:

```csharp
private string BuildSql()
{
    var tableName = IdentifierQuoter.QuoteTable(QBuilder.TableNameResolver(typeof(TTable)), QBuilder.Dialect);
    var cols = string.Join(", ", _columns);
    var vals = string.Join(", ", _valuePlaceholders);
    return $"Insert Into {tableName} ({cols}) Values ({vals})";
}
```

Replace it with:

```csharp
private string BuildSql()
{
    var tableName = IdentifierQuoter.QuoteTable(QBuilder.TableNameResolver(typeof(TTable)), QBuilder.Dialect);
    var cols = string.Join(", ", _columns);

    // Single-row path — unchanged behaviour when no extra rows were added.
    if (_extraValueRows.Count == 0)
    {
        var vals = string.Join(", ", _valuePlaceholders);
        return $"Insert Into {tableName} ({cols}) Values ({vals})";
    }

    // Multi-row path — emit VALUES (row1), (row2), ...
    var allRows = new List<string>(1 + _extraValueRows.Count);
    allRows.Add($"({string.Join(", ", _valuePlaceholders)})");
    foreach (var row in _extraValueRows)
        allRows.Add($"({string.Join(", ", row)})");

    return $"Insert Into {tableName} ({cols}) Values {string.Join(", ", allRows)}";
}
```

**The existing `Validate()` method does not change.** It checks `_columns.Count == 0`, which remains the correct guard — if no columns were added, the INSERT is invalid regardless of how many rows were attempted.

**The existing `Build()` and `BuildWithParameters()` methods do not change.** They call `Validate()` and then `BuildSql()`. The multi-row logic is entirely inside `BuildSql()`.

---

### Usage example

```csharp
var users = new List<User>
{
    new User { Id = Guid.NewGuid(), Name = "Alice", IsActive = true },
    new User { Id = Guid.NewGuid(), Name = "Bob",   IsActive = true },
    new User { Id = Guid.NewGuid(), Name = "Carol",  IsActive = false },
};

var q = Q.New()
    .UseTableBoundInsert<User>()
    .FromObjects(users)
    .BuildWithParameters();

// q.ParameterizedSql:
// Insert Into User (Id, Name, IsActive) Values
//   (@Id0, @Name0, @IsActive0),
//   (@Id1, @Name1, @IsActive1),
//   (@Id2, @Name2, @IsActive2)

// q.Parameters:
// { "@Id0": <guid>, "@Name0": "Alice", "@IsActive0": true,
//   "@Id1": <guid>, "@Name1": "Bob",   "@IsActive1": true,
//   "@Id2": <guid>, "@Name2": "Carol", "@IsActive2": false }

connection.Execute(q.ParameterizedSql, q.Parameters);
```

---

### Database parameter count limit (SQL Server)

SQL Server has a hard limit of **2,100 parameters per query**. If you insert N rows of a T-column table, you need `N × T` parameters. The limit is hit at:

```
N = floor(2100 / T)
```

For a 5-column table: max ~420 rows per INSERT. For a 20-column table: max ~105 rows.

**The library does not enforce this limit** — doing so would require dialect-specific logic that is out of scope. It is the caller's responsibility to chunk large collections. A safe chunking helper:

```csharp
public static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> source, int size)
{
    var batch = new List<T>(size);
    foreach (var item in source)
    {
        batch.Add(item);
        if (batch.Count == size) { yield return batch; batch = new List<T>(size); }
    }
    if (batch.Count > 0) yield return batch;
}

// Usage
foreach (var chunk in Chunk(largeUserList, 200))
{
    var q = Q.New().UseTableBoundInsert<User>().FromObjects(chunk).BuildWithParameters();
    connection.Execute(q.ParameterizedSql, q.Parameters);
}
```

This pattern must be documented in the migration guide (see section 8 addition in section 14 below).

---

## 12. Batched DML — `QBatch`

### What batching means and when to use it

Bulk INSERT (section 11) sends multiple rows in one SQL statement. That only works for INSERT. For UPDATE and DELETE, there is no equivalent multi-row syntax in standard SQL — each affected row typically requires its own `UPDATE ... WHERE id = X` or `DELETE ... WHERE id = X`.

Batching solves this by combining multiple separate DML statements into a single string, separated by `;`, and executing that string as a single database call:

```sql
Update User Set Name = @Name0 Where User.Id = @Id0;
Update User Set Name = @Name1 Where User.Id = @Id1;
Delete From Order Where Order.UserId = @UserId0
```

This is one round-trip instead of three. The database driver executes all statements in sequence.

**When to batch vs when not to:**
- Use batching when you have multiple UPDATE or DELETE statements that must go together as a logical unit.
- Do NOT use batching as a replacement for bulk INSERT — use `FromObjects` for that (one `VALUES` row per item is more efficient than one statement per item).
- Do NOT use batching for very large sets of statements — some databases have a maximum SQL string length. Keep batches to hundreds of statements, not thousands.
- Batching does NOT mean a transaction. If you need atomicity (all succeed or all roll back), wrap the batch execution in an ADO.NET transaction yourself.

---

### New file: `src/Jattac.Libraries.QBuilder/QBatch.cs`

Create this file at the root of the `src/Jattac.Libraries.QBuilder/` folder, alongside `QBuilder.cs`.

```csharp
namespace Jattac.Libraries.QBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Jattac.Libraries.QBuilder.Helpers;

    /// <summary>
    /// Combines multiple parameterized DML statements into a single round-trip.
    /// Statements are joined with ";\n". Parameters from all statements are merged
    /// into a single dictionary; any name collisions are automatically renamed.
    /// </summary>
    /// <example>
    /// <code>
    /// var batch = QBatch.New()
    ///     .Add(Q.New().UseTableBoundUpdate&lt;User&gt;().FromObject(user1).BuildWithParameters())
    ///     .Add(Q.New().UseTableBoundUpdate&lt;User&gt;().FromObject(user2).BuildWithParameters())
    ///     .Build();
    ///
    /// connection.Execute(batch.ParameterizedSql, batch.Parameters);
    /// </code>
    /// </example>
    public sealed class QBatch
    {
        private readonly List<BuiltQuery> _queries = new List<BuiltQuery>();

        private QBatch() { }

        /// <summary>Creates a new empty batch.</summary>
        public static QBatch New() => new QBatch();

        /// <summary>
        /// Adds a parameterized query to the batch.
        /// The query must have been built with <c>BuildWithParameters()</c>.
        /// </summary>
        public QBatch Add(BuiltQuery query)
        {
            Guard.NotNull(query, nameof(query));
            Guard.Against(
                string.IsNullOrEmpty(query.ParameterizedSql),
                "Cannot add a query with an empty SQL string to a batch. " +
                "Ensure BuildWithParameters() has been called before Add().");
            _queries.Add(query);
            return this;
        }

        /// <summary>
        /// Adds multiple queries to the batch.
        /// </summary>
        public QBatch AddRange(IEnumerable<BuiltQuery> queries)
        {
            Guard.NotNull(queries, nameof(queries));
            foreach (var q in queries)
                Add(q);
            return this;
        }

        /// <summary>
        /// Combines all added queries into a single <see cref="BuiltQuery"/>.
        /// SQL statements are joined with ";\n".
        /// Parameters from all queries are merged; any name that collides with an
        /// already-registered parameter is automatically renamed in both the SQL and
        /// the parameter dictionary.
        /// </summary>
        public BuiltQuery Build()
        {
            Guard.Against(_queries.Count == 0,
                "No queries have been added to this batch. Call Add() at least once before Build().");

            var mergedParameters = new Dictionary<string, object>(StringComparer.Ordinal);
            var sqlParts = new List<string>(_queries.Count);

            foreach (var query in _queries)
            {
                // Work on a mutable copy of the SQL so we can rename parameters.
                var sql = query.ParameterizedSql;

                foreach (var kvp in query.Parameters)
                {
                    if (!mergedParameters.ContainsKey(kvp.Key))
                    {
                        // No collision — use the name as-is.
                        mergedParameters[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        // Collision — find a unique name and rename both the SQL
                        // placeholder and the entry we add to mergedParameters.
                        var newName = FindUniqueName(kvp.Key, mergedParameters);
                        sql = ReplaceParamName(sql, kvp.Key, newName);
                        mergedParameters[newName] = kvp.Value;
                    }
                }

                sqlParts.Add(sql);
            }

            return new BuiltQuery
            {
                ParameterizedSql = string.Join(";\n", sqlParts),
                Parameters = mergedParameters
            };
        }

        // ── Private helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Increments the trailing integer suffix of a parameter name until the
        /// resulting name is not already in <paramref name="existing"/>.
        /// Example: "@Id0" with "@Id0" taken → tries "@Id1", "@Id2", ...
        /// </summary>
        private static string FindUniqueName(string original, Dictionary<string, object> existing)
        {
            // Parameter names are always in the form @FieldName{N} (e.g. @Id0, @Name12).
            // Split at the last sequence of digits to get the prefix and current index.
            var match = Regex.Match(original, @"^(.+?)(\d+)$");
            if (!match.Success)
            {
                // Unexpected format — fall back to simple suffix appending.
                var fallback = original + "_b";
                while (existing.ContainsKey(fallback)) fallback += "_b";
                return fallback;
            }

            var prefix = match.Groups[1].Value;   // e.g. "@Id"
            var index  = int.Parse(match.Groups[2].Value) + 1;

            string candidate;
            do { candidate = $"{prefix}{index++}"; }
            while (existing.ContainsKey(candidate));

            return candidate;
        }

        /// <summary>
        /// Replaces all exact occurrences of <paramref name="oldName"/> in
        /// <paramref name="sql"/> with <paramref name="newName"/>, taking care
        /// not to replace a shorter name that is a prefix of a longer one
        /// (e.g. replacing @Id0 must not affect @Id01 or @Id0Extra).
        /// Uses a negative lookahead for word characters.
        /// </summary>
        private static string ReplaceParamName(string sql, string oldName, string newName)
        {
            // Regex.Escape handles the '@' and any special regex chars in the name.
            // Lookahead (?![a-zA-Z0-9_]) ensures we only match the full token.
            // Example: pattern for "@Id0" will NOT match inside "@Id01".
            var pattern = Regex.Escape(oldName) + @"(?![a-zA-Z0-9_])";
            return Regex.Replace(sql, pattern, newName);
        }
    }
}
```

**Important:** `QBatch` is in the root `Jattac.Libraries.QBuilder` namespace, not in any sub-namespace. This keeps it alongside `QBuilder.cs` and `BuiltQuery.cs`.

**Important:** `BuiltQuery` already has public setters on `ParameterizedSql` and `Parameters` (visible from the existing code). The `Build()` method sets them on a newly constructed `BuiltQuery`. Do not modify `BuiltQuery.cs`.

---

### How parameter renaming works — a worked example

Suppose you batch two UPDATE queries where both generate a parameter named `@Id0`:

```
Query 1 SQL:    "Update User Set Name = @Name0 Where User.Id = @Id0"
Query 1 Params: { "@Name0": "Alice", "@Id0": <guid-1> }

Query 2 SQL:    "Update User Set Name = @Name1 Where User.Id = @Id0"
Query 2 Params: { "@Name1": "Bob", "@Id0": <guid-2> }
```

During `Build()`:

- Process query 1: `@Name0` → no collision → add as-is. `@Id0` → no collision → add as-is.
- Process query 2: `@Name1` → no collision → add as-is. `@Id0` → **collision** with `@Id0` already in merged dict → call `FindUniqueName("@Id0", merged)` → returns `@Id1` → replace `@Id0` with `@Id1` in query 2's SQL → add `@Id1 → <guid-2>` to merged.

Result:
```
SQL:    "Update User Set Name = @Name0 Where User.Id = @Id0;\nUpdate User Set Name = @Name1 Where User.Id = @Id1"
Params: { "@Name0": "Alice", "@Id0": <guid-1>, "@Name1": "Bob", "@Id1": <guid-2> }
```

This is correct. The two UPDATE statements each refer to different `@Id` parameters, and the values are correct.

---

### Non-parameterized batch (raw SQL strings)

If the queries were built non-parameterized (using `.Build()` instead of `.BuildWithParameters()`), they return plain `string` values, not `BuiltQuery`. In that case, batching is trivial — just join the strings:

```csharp
var sql1 = Q.New(false).UseTableBoundUpdate<User>()...Build();
var sql2 = Q.New(false).UseTableBoundDelete<Order>()...Build();
var batchSql = string.Join(";\n", sql1, sql2);
connection.Execute(batchSql);
```

`QBatch` only handles parameterized queries. There is no class needed for the raw SQL case.

---

## 13. Bulk UPDATE and DELETE Patterns

### Bulk UPDATE — use `QBatch`

There is no multi-row UPDATE syntax in standard SQL. To update multiple rows where each row has different values, generate one `BuiltQuery` per row and combine them with `QBatch`:

```csharp
var batch = QBatch.New();
foreach (var user in users)
{
    var q = Q.New()
        .UseTableBoundUpdate<User>()
        .FromObject(user)       // [QKey] Id → WHERE; Name, IsActive → SET
        .BuildWithParameters();
    batch.Add(q);
}
var result = batch.Build();
connection.Execute(result.ParameterizedSql, result.Parameters);
```

If all rows change the same columns to the same value (e.g. deactivating all users in a list), use `WhereIn` instead — that is a single statement and much more efficient:

```csharp
// More efficient — single statement
Q.New()
    .UseTableBoundUpdate<User>()
    .Set(u => u.IsActive, false)
    .WhereIn<Guid, Guid>(u => u.Id, userIds)
    .BuildWithParameters();
// → Update User Set IsActive = @IsActive0 Where User.Id In (@Id0, @Id1, @Id2)
```

Choose `QBatch` when each row has **different values**. Choose `WhereIn` when all rows get the **same value**.

---

### Bulk DELETE — two patterns

**Pattern A: `WhereIn` (preferred, already available)**

This already works today. Delete multiple rows by ID in one statement:

```csharp
Q.New()
    .UseTableBoundDelete<User>()
    .WhereIn<Guid, Guid>(u => u.Id, idsToDelete)
    .BuildWithParameters();
// → Delete From User Where User.Id In (@Id0, @Id1, @Id2, ...)
```

This is one round-trip, one lock, and works efficiently for large ID lists (within the parameter count limit). **Prefer this pattern whenever you are deleting by a set of key values.**

**Pattern B: `QBatch` (for per-row conditions)**

If each DELETE has a different WHERE condition — for example deleting by a composite key where both columns vary per row:

```csharp
var batch = QBatch.New();
foreach (var key in compositeKeys)
{
    var q = Q.New()
        .UseTableBoundDelete<WorkflowInstanceState>()
        .WhereEqualTo(s => s.WorkflowInstanceId, key.InstanceId)
        .AndWhereEqualTo(s => s.UrgencyId, key.UrgencyId)
        .BuildWithParameters();
    batch.Add(q);
}
var result = batch.Build();
```

Use `QBatch` for composite-key deletes. Use `WhereIn` for single-key deletes.

---

### `AddRange` convenience for `QBatch`

When building a batch from a collection, prefer `AddRange` over a manual `foreach`:

```csharp
var queries = users.Select(user =>
    Q.New()
        .UseTableBoundUpdate<User>()
        .FromObject(user)
        .BuildWithParameters());

var result = QBatch.New().AddRange(queries).Build();
```

`AddRange` accepts `IEnumerable<BuiltQuery>` and adds each element via `Add()`. It has the same null guard as `Add()`.

---

## 14. Additional Tests for Bulk and Batch

### New test models needed

Add these to the existing `UserWithAttributes.cs` file or as a new file in `tests/.../Models/`:

```csharp
// For bulk INSERT tests — a minimal model to keep param counts predictable
internal class SimpleProduct
{
    [QKey]
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}
```

---

### `tests/.../DML/BulkInsertTests.cs` — unit tests (no DB)

| Test method name | What to assert |
|---|---|
| `FromObjects_ThreeItems_EmitsThreeValueRows` | SQL contains two `,` separating three `(...)` value groups |
| `FromObjects_ThreeItems_AllParametersPresent` | `Parameters` has `Count == 3 × column_count` |
| `FromObjects_ParamsAreUniqueAcrossRows` | No two entries in `Parameters` share the same key; specifically `@Id0`, `@Id1`, `@Id2` all exist |
| `FromObjects_EmptyCollection_ThrowsGuard` | `InvalidOperationException` with message about empty collection |
| `FromObjects_NullCollection_ThrowsArgumentNull` | `ArgumentNullException` |
| `FromObjects_SingleItem_BehavesLikeFromObject` | SQL is identical to calling `FromObject` with the same item |
| `FromObjects_QIgnore_PropertyAbsentFromAllRows` | Ignored column absent from every value row |
| `FromObjects_QColumn_AliasUsedInColumnHeader` | Column header in SQL uses alias name, not property name; appears once (columns are set from first row only) |
| `FromObjects_DialectSqlServer_ColumnsBracketed` | Column list uses `[...]` quoting |
| `FromObjects_NonParameterized_ProducesInlineLiterals` | Built without parameterize — all rows have inline literals |
| `FromObjects_ThenManualValue_AppendedToFirstRow` | `.FromObjects(twoItems).Value(u => u.Extra, "x")` → `Extra` column appears in column list and first row's values only (second row has no Extra) |

> **Note on that last test:** mixing `FromObjects` and `Value()` after it produces structurally mismatched rows — the columns list will have `Extra` but the extra rows' placeholder lists won't have a corresponding entry. This is a misuse of the API. The BRS does **not** require you to gracefully handle this; let the SQL be malformed and document it as a pitfall (see Pitfall 17 below). The test above is only to verify the current observable behaviour, not to test for an error.

---

### `tests/.../DML/BatchTests.cs` — unit tests (no DB)

| Test method name | What to assert |
|---|---|
| `QBatch_TwoQueries_SqlJoinedWithSemicolon` | Result SQL contains `;\n` between the two statements |
| `QBatch_NoCollision_ParamsMergedAsIs` | All original parameter names preserved; `Parameters.Count` equals sum of both queries' param counts |
| `QBatch_ParamCollision_CollidingParamRenamed` | When both queries have `@Id0`, result has `@Id0` and `@Id1` (not two `@Id0` entries) |
| `QBatch_ParamCollision_SqlUpdatedToMatchRenamedParam` | The second query's SQL uses `@Id1` after collision rename, not `@Id0` |
| `QBatch_ParamCollision_ValuesCorrectAfterRename` | `@Id0` value is from query 1; `@Id1` value is from query 2 |
| `QBatch_ThreeQueries_AllParamsMergedCorrectly` | Three queries with overlapping names all renamed consistently |
| `QBatch_EmptyBatch_ThrowsGuard` | `Build()` on an empty batch throws `InvalidOperationException` |
| `QBatch_AddNull_ThrowsArgumentNull` | `Add(null)` throws |
| `QBatch_AddEmptySql_ThrowsGuard` | `Add(new BuiltQuery())` (empty SQL) throws |
| `QBatch_AddRange_AddsAll` | `AddRange(threeQueries)` → `Build()` emits three statements |
| `QBatch_AddRange_NullCollection_Throws` | `AddRange(null)` throws |
| `QBatch_ShortParamName_NoFalseSubstringReplacement` | When `@Id0` and `@Id01` both exist, renaming `@Id0` does not corrupt `@Id01` |

> The last test is the most important for correctness. Construct it explicitly: build two queries — one with param `@Id0`, one with param `@Id01`. Batch them. Verify `@Id01` in the second query's SQL is untouched after the first query's `@Id0` is processed.

---

### `tests/.../DML/Integration/BulkAndBatchIntegrationTests.cs` — SQLite round-trips

Follow the same setup pattern as `DmlIntegrationTests.cs` (SQLite in-memory, table creation in constructor, Dapper for execution).

| Test | What to do |
|---|---|
| `FromObjects_InsertThreeRows_AllRetrievable` | Insert 3 rows via `FromObjects`; SELECT all; assert 3 rows with correct values |
| `FromObjects_InsertEmpty_ThrowsBeforeHittingDb` | Guard fires before any DB call |
| `QBatch_UpdateTwoRows_BothUpdated` | Insert 2 rows; build a batch of 2 UPDATEs (one per row); execute batch; verify both rows updated |
| `QBatch_DeleteByCompositeKey_CorrectRowsRemoved` | Insert 3 rows with composite keys; batch-delete 2 of them; verify 1 remains |
| `QBatch_InsertThenUpdate_BothApplied` | Batch an INSERT and an UPDATE together; verify both effects are visible |

---

### Migration guide additions for bulk/batch

Append to the same `docs/migration-v7-to-v8.md` section you added for POCO shortcuts:

```markdown
### Bulk INSERT — `FromObjects` (new in v9.2)

Insert multiple rows in a single SQL statement:

\```csharp
var users = new List<User> { ... };

Q.New()
    .UseTableBoundInsert<User>()
    .FromObjects(users)
    .BuildWithParameters();
// → Insert Into User (Id, Name) Values (@Id0, @Name0), (@Id1, @Name1), ...
\```

**SQL Server parameter limit:** SQL Server allows a maximum of 2,100 parameters per query.
For wide tables or large collections, chunk the list before calling `FromObjects`:

\```csharp
foreach (var chunk in users.Chunk(200))   // .NET 6+ has Enumerable.Chunk
{
    var q = Q.New().UseTableBoundInsert<User>().FromObjects(chunk).BuildWithParameters();
    connection.Execute(q.ParameterizedSql, q.Parameters);
}
\```

### Batched DML — `QBatch` (new in v9.2)

Combine multiple DML statements into a single round-trip:

\```csharp
var result = QBatch.New()
    .AddRange(users.Select(u =>
        Q.New().UseTableBoundUpdate<User>().FromObject(u).BuildWithParameters()))
    .Build();

connection.Execute(result.ParameterizedSql, result.Parameters);
\```

`QBatch` automatically renames any parameter name that collides across statements.
It does NOT wrap statements in a transaction — use an ADO.NET transaction if needed.

### Bulk DELETE — `WhereIn` (already available, recommended)

For deleting multiple rows by a set of IDs, use the existing `WhereIn`:

\```csharp
Q.New()
    .UseTableBoundDelete<User>()
    .WhereIn<Guid, Guid>(u => u.Id, idsToDelete)
    .BuildWithParameters();
// → Delete From User Where User.Id In (@Id0, @Id1, @Id2)
\```
```

---

## 15. Additional Pitfalls for Bulk and Batch

### Pitfall 13: Mixing `FromObjects` and then `Value()` (column mismatch)

After calling `FromObjects(items)`, the `_columns` list is set from the first item. If you then call `.Value(u => u.ExtraCol, val)`, the new column is added to `_columns` and a placeholder is added to `_valuePlaceholders` (the first row's placeholder list). The second and subsequent rows stored in `_extraValueRows` will have **fewer placeholders than columns**. The resulting SQL is structurally invalid:

```sql
-- BROKEN: col1, col2, ExtraCol listed but second row only has 2 values
INSERT INTO User (Id, Name, ExtraCol) VALUES (@Id0, @Name0, @ExtraCol0), (@Id1, @Name1)
```

**Do not call `Value()` or `FromObject()` after `FromObjects()`.** If you need to add a fixed value to all rows, either include it in the POCO or pre-populate the collection before passing it to `FromObjects`.

### Pitfall 14: Passing a mixed-type collection to `FromObjects`

`FromObjects<T>` is generic in `T`. If you pass `List<object>` containing a mix of different concrete types:

```csharp
var mixed = new List<object> { new User { ... }, new Order { ... } };
Q.New().UseTableBoundInsert<User>().FromObjects(mixed);
```

The first item (`User`) sets the columns from `User`'s properties. The second item (`Order`) is then reflected as `Order` — `PocoReflector.GetProperties(orderInstance)` will return `Order`'s properties, not `User`'s. The column count will mismatch. Always use a strongly-typed collection: `List<User>`, not `List<object>`.

### Pitfall 15: Creating a new `BuiltQuery` per row in `BuildRowPlaceholders`

Inside `BuildRowPlaceholders`, parameters are added to `_builtQuery` — the same shared `BuiltQuery` instance used by all rows. Do NOT create `var rowQuery = new BuiltQuery()` and pass it to `ConditionMaker.GetParameterName`. If you do, the collision avoidance loop in `GetParameterName` will start from `@Id0` for every row because it only sees the empty `rowQuery.Parameters` dictionary — not the parameters from previous rows. You will end up with duplicate `@Id0` keys and a crash when trying to add them to the main `_builtQuery.Parameters` dictionary.

### Pitfall 16: Forgetting that `QBatch` does not provide a transaction

After `.Build()`, executing `batch.ParameterizedSql` sends all statements as a single string. If the third statement fails, the first two have already committed (unless you wrapped in an ADO.NET transaction). `QBatch` is a **SQL composition** utility, not a transaction manager. If you need atomicity:

```csharp
using var transaction = connection.BeginTransaction();
try
{
    connection.Execute(batch.ParameterizedSql, batch.Parameters, transaction);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### Pitfall 17: The `@Id0` / `@Id01` substring collision in `ReplaceParamName`

`@Id0` is a prefix of `@Id01`. A naive `sql.Replace("@Id0", "@Id1")` would corrupt `@Id01` → `@Id11`. The `ReplaceParamName` method uses the regex pattern:

```
Regex.Escape(oldName) + @"(?![a-zA-Z0-9_])"
```

The negative lookahead `(?![a-zA-Z0-9_])` means "only match if the next character is NOT a letter, digit, or underscore." This correctly matches `@Id0` when followed by `,`, `)`, space, or end-of-string — and does NOT match `@Id0` inside `@Id01` (because `1` is a digit). Do not simplify this to a plain `string.Replace`. The regex is required.

### Pitfall 18: Assuming the parameter count is within database limits

`FromObjects` generates `N × C` parameters (N rows, C non-ignored columns). SQL Server fails with a runtime error when this exceeds 2,100. MySQL, PostgreSQL, SQLite, and MariaDB have much higher or no practical limits, but SQL Server is common enough that this matters. The library does not validate or chunk automatically. If your application targets SQL Server and inserts more than a few hundred rows at once, implement chunking in the calling code.

### Pitfall 19: Calling `QBatch.Build()` and ignoring the return value

`QBatch.Build()` does NOT mutate any internal state — it returns a new `BuiltQuery`. The common mistake is:

```csharp
var batch = QBatch.New().Add(q1).Add(q2);
batch.Build();  // result discarded!
connection.Execute(???, ???);
```

Always assign the return value:

```csharp
var result = batch.Build();
connection.Execute(result.ParameterizedSql, result.Parameters);
```
