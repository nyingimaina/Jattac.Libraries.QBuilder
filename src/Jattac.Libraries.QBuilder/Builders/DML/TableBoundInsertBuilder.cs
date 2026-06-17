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
            _columns.Add(IdentifierQuoter.QuoteIdentifier(col, QBuilder.Dialect));
            if (_builtQuery != null)
            {
                var paramName = ConditionMaker.GetParameterName(col, _builtQuery);
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

        private string BuildSql()
        {
            var tableName = IdentifierQuoter.QuoteTable(QBuilder.TableNameResolver(typeof(TTable)), QBuilder.Dialect);
            var cols = string.Join(", ", _columns);
            var vals = string.Join(", ", _valuePlaceholders);
            return $"Insert Into {tableName} ({cols}) Values ({vals})";
        }

        private void Validate()
        {
            Guard.Against(_columns.Count == 0,
                "No values specified for INSERT. At least one .Value(col, value) call is required.");
        }
    }
}
