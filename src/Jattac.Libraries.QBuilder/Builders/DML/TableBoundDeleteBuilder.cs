namespace Jattac.Libraries.QBuilder.Builders.DML
{
    using System;
    using Jattac.Libraries.QBuilder.Enums;
    using Jattac.Libraries.QBuilder.Helpers;

    /// <summary>
    /// Fluent DELETE builder scoped to <typeparamref name="TTable"/>.
    /// Emits <c>DELETE FROM {table} WHERE {conditions}</c>.
    /// A WHERE clause is required by default — call <see cref="ForEntireTable"/> to explicitly
    /// delete all rows without a condition.
    /// </summary>
    /// <typeparam name="TTable">The table to delete from.</typeparam>
    public class TableBoundDeleteBuilder<TTable>
        : DmlWhereBuilder<TTable, TableBoundDeleteBuilder<TTable>>
    {
        private readonly BuiltQuery _builtQuery;
        private bool _forEntireTable;

        internal TableBoundDeleteBuilder(QBuilder qBuilder, WhereBuilder whereBuilder, BuiltQuery builtQuery)
            : base(qBuilder, whereBuilder)
        {
            _builtQuery = builtQuery;
        }

        /// <summary>
        /// Opts out of the no-WHERE guard. Use this when deliberately deleting every row in the table.
        /// </summary>
        public TableBoundDeleteBuilder<TTable> ForEntireTable()
        {
            _forEntireTable = true;
            return this;
        }

        /// <summary>
        /// Builds a raw (non-parameterized) DELETE statement.
        /// </summary>
        public string Build()
        {
            Validate();
            var tableName = IdentifierQuoter.QuoteTable(QBuilder.TableNameResolver(typeof(TTable)), QBuilder.Dialect);
            return ($"Delete From {tableName} " + BuildWhereClause()).Trim();
        }

        /// <summary>
        /// Builds a parameterized DELETE statement ready for Dapper or ADO.NET.
        /// </summary>
        public BuiltQuery BuildWithParameters(Action<string> logSql = null)
        {
            Guard.Against(_builtQuery == null,
                $"This QBuilder was created with parameterize: false. Use {nameof(Build)}() instead, or create a new QBuilder with parameterize: true.");
            Validate();
            var tableName = IdentifierQuoter.QuoteTable(QBuilder.TableNameResolver(typeof(TTable)), QBuilder.Dialect);
            _builtQuery.ParameterizedSql = ($"Delete From {tableName} " + BuildWhereClause()).Trim();
            logSql?.Invoke(_builtQuery.ParameterizedSql);
            return _builtQuery;
        }

        private void Validate()
        {
            Guard.Against(!_forEntireTable && !HasWhere,
                "Delete without a WHERE clause deletes every row. Call .WhereEqualTo(...) or any other predicate to scope the delete, or call .ForEntireTable() to explicitly confirm a full-table delete.");
        }
    }
}
