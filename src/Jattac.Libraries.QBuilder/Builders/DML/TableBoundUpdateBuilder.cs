namespace Jattac.Libraries.QBuilder.Builders.DML
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Jattac.Libraries.QBuilder.Enums;
    using Jattac.Libraries.QBuilder.Helpers;

    /// <summary>
    /// Fluent UPDATE builder scoped to <typeparamref name="TTable"/>.
    /// Emits <c>UPDATE {table} SET col = value [, ...] WHERE {conditions}</c>.
    /// At least one <see cref="Set{TField}"/> call is required.
    /// A WHERE clause is required by default — call <see cref="ForEntireTable"/> to explicitly
    /// update every row without a condition.
    /// </summary>
    /// <typeparam name="TTable">The table to update.</typeparam>
    public class TableBoundUpdateBuilder<TTable>
        : DmlWhereBuilder<TTable, TableBoundUpdateBuilder<TTable>>
    {
        private readonly BuiltQuery _builtQuery;
        private readonly List<string> _setClauses = new List<string>();
        private bool _forEntireTable;

        internal TableBoundUpdateBuilder(QBuilder qBuilder, WhereBuilder whereBuilder, BuiltQuery builtQuery)
            : base(qBuilder, whereBuilder)
        {
            _builtQuery = builtQuery;
        }

        /// <summary>
        /// Adds a SET assignment: <c>column = value</c>.
        /// </summary>
        public TableBoundUpdateBuilder<TTable> Set<TField>(Expression<Func<TTable, TField>> descriptor, object value)
        {
            var col = Fnr.GetFieldName(descriptor);
            var quotedCol = IdentifierQuoter.QuoteIdentifier(col, QBuilder.Dialect);
            if (_builtQuery != null)
            {
                var paramName = ConditionMaker.GetParameterName(col, _builtQuery);
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

        /// <summary>
        /// Opts out of the no-WHERE guard. Use this when deliberately updating every row in the table.
        /// </summary>
        public TableBoundUpdateBuilder<TTable> ForEntireTable()
        {
            _forEntireTable = true;
            return this;
        }

        /// <summary>
        /// Builds a raw (non-parameterized) UPDATE statement.
        /// </summary>
        public string Build()
        {
            Validate();
            var tableName = IdentifierQuoter.QuoteTable(QBuilder.TableNameResolver(typeof(TTable)), QBuilder.Dialect);
            var setClause = string.Join(", ", _setClauses);
            return ($"Update {tableName} Set {setClause} " + BuildWhereClause()).Trim();
        }

        /// <summary>
        /// Builds a parameterized UPDATE statement ready for Dapper or ADO.NET.
        /// </summary>
        public BuiltQuery BuildWithParameters(Action<string> logSql = null)
        {
            Guard.Against(_builtQuery == null,
                $"This QBuilder was created with parameterize: false. Use {nameof(Build)}() instead, or create a new QBuilder with parameterize: true.");
            Validate();
            var tableName = IdentifierQuoter.QuoteTable(QBuilder.TableNameResolver(typeof(TTable)), QBuilder.Dialect);
            var setClause = string.Join(", ", _setClauses);
            _builtQuery.ParameterizedSql = ($"Update {tableName} Set {setClause} " + BuildWhereClause()).Trim();
            logSql?.Invoke(_builtQuery.ParameterizedSql);
            return _builtQuery;
        }

        private void Validate()
        {
            Guard.Against(_setClauses.Count == 0,
                "No columns specified for the SET clause. At least one .Set(col, value) call is required.");
            Guard.Against(!_forEntireTable && !HasWhere,
                "Update without a WHERE clause updates every row. Call .WhereEqualTo(...) or any other predicate to scope the update, or call .ForEntireTable() to explicitly confirm a full-table update.");
        }
    }
}
