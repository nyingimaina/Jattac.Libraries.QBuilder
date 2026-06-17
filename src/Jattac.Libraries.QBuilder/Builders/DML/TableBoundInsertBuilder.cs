namespace Jattac.Libraries.QBuilder.Builders.DML
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Jattac.Libraries.QBuilder.Enums;
    using Jattac.Libraries.QBuilder.Helpers;

    /// <summary>
    /// Fluent INSERT builder scoped to <typeparamref name="TTable"/>.
    /// Emits <c>INSERT INTO {table} (col, ...) VALUES (val, ...)</c>.
    /// At least one <see cref="Value{TField}"/> call is required.
    /// </summary>
    /// <typeparam name="TTable">The table to insert into.</typeparam>
    public class TableBoundInsertBuilder<TTable> : BuilderBase
    {
        private readonly BuiltQuery _builtQuery;
        private readonly FieldNameResolver _fnr = new FieldNameResolver();
        private readonly List<string> _columns = new List<string>();
        private readonly List<string> _valuePlaceholders = new List<string>();
        private readonly List<List<string>> _extraValueRows = new List<List<string>>();

        internal TableBoundInsertBuilder(QBuilder qBuilder, BuiltQuery builtQuery)
            : base(qBuilder)
        {
            _builtQuery = builtQuery;
        }

        /// <summary>
        /// Adds a column-value pair to the INSERT statement.
        /// </summary>
        public TableBoundInsertBuilder<TTable> Value<TField>(Expression<Func<TTable, TField>> descriptor, object value)
        {
            var col = _fnr.GetFieldName(descriptor);
            return ValueCore(col, col, value);
        }

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

        /// <summary>
        /// Populates a multi-row INSERT from a collection of POCOs or anonymous objects.
        /// Generates: INSERT INTO table (col1, col2) VALUES (...), (...), (...)
        /// All items must be the same concrete type T. Column structure is derived from the
        /// first item; subsequent items must have the same non-ignored properties in the same order.
        /// </summary>
        public TableBoundInsertBuilder<TTable> FromObjects<T>(IEnumerable<T> items)
        {
            Guard.NotNull(items, nameof(items));
            var list = items as IList<T> ?? new List<T>(items);
            Guard.Against(list.Count == 0,
                "FromObjects requires at least one item. The collection was empty.");

            var isFirst = true;
            foreach (var item in list)
            {
                if (isFirst)
                {
                    FromObject(item);
                    isFirst = false;
                }
                else
                {
                    var props = PocoReflector.GetProperties(item);
                    _extraValueRows.Add(BuildRowPlaceholders(props));
                }
            }
            return this;
        }

        /// <summary>
        /// Builds a raw (non-parameterized) INSERT statement.
        /// </summary>
        public string Build()
        {
            Validate();
            return BuildSql();
        }

        /// <summary>
        /// Builds a parameterized INSERT statement ready for Dapper or ADO.NET.
        /// </summary>
        public BuiltQuery BuildWithParameters(Action<string> logSql = null)
        {
            Guard.Against(_builtQuery == null,
                $"This QBuilder was created with parameterize: false. Use {nameof(Build)}() instead, or create a new QBuilder with parameterize: true.");
            Validate();
            _builtQuery.ParameterizedSql = BuildSql();
            logSql?.Invoke(_builtQuery.ParameterizedSql);
            return _builtQuery;
        }

        // Core insert logic shared by the expression-based Value() and the reflection-based FromObject().
        // columnName: the SQL column name (may be overridden by [QColumn]).
        // paramSeed: the C# property name used as the base for parameter name generation.
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

        // Builds the value-placeholder list for one additional row in a bulk INSERT.
        // Does NOT touch _columns — those are set once from the first item.
        // Re-uses _builtQuery so ConditionMaker's collision avoidance sees all previous parameters.
        private List<string> BuildRowPlaceholders(IReadOnlyList<PocoReflector.PocoProperty> props)
        {
            var rowPlaceholders = new List<string>();
            foreach (var p in props)
            {
                if (p.IsIgnored) continue;
                if (_builtQuery != null)
                {
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

        private string BuildSql()
        {
            var tableName = IdentifierQuoter.QuoteTable(QBuilder.TableNameResolver(typeof(TTable)), QBuilder.Dialect);
            var cols = string.Join(", ", _columns);

            if (_extraValueRows.Count == 0)
            {
                var vals = string.Join(", ", _valuePlaceholders);
                return $"Insert Into {tableName} ({cols}) Values ({vals})";
            }

            var allRows = new List<string>(1 + _extraValueRows.Count);
            allRows.Add($"({string.Join(", ", _valuePlaceholders)})");
            foreach (var row in _extraValueRows)
                allRows.Add($"({string.Join(", ", row)})");

            return $"Insert Into {tableName} ({cols}) Values {string.Join(", ", allRows)}";
        }

        private void Validate()
        {
            Guard.Against(_columns.Count == 0,
                "No values specified for INSERT. At least one .Value(col, value) call is required.");
        }
    }
}
