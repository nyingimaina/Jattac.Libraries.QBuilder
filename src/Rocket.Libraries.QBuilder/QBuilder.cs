namespace Rocket.Libraries.Qurious
{

    using System.Collections.Generic;
    using Rocket.Libraries.Qurious.Builders;
    using Rocket.Libraries.Qurious.Builders.Paging;
    using Rocket.Libraries.Qurious.Enums;
    using Rocket.Libraries.Qurious.Models;
    using Rocket.Libraries.Validation.Services;
    using System;

    /// <summary>
    /// Provides a fluent interface for building SQL queries with joins, filters, ordering, and grouping.
    /// </summary>
    public class QBuilder : IDisposable
    {
        private SelectBuilder _selectBuilder;

        private OrderBuilder _orderBuilder;

        private JoinBuilder _joinBuilder;

        private WhereBuilder _whereBuilder;

        private HavingBuilder _havingBuilder;

        internal string _aliasTableName;

        private GroupBuilder _groupBuilder;

        private string _suffix;

        private BuiltQuery builtQuery;

        private bool _isBuilt;

        private List<CteDescription> _ctes = new List<CteDescription>();

        private List<SetOperationDescription> _setOperations = new List<SetOperationDescription>();

        /// <summary>
        /// Initializes a new instance of the <see cref="QBuilder"/> class with the parameterization option.
        /// </summary>
        /// <param name="parameterize">Determines whether the query should be parameterized for secure parameter binding.</param>
        public QBuilder(bool parameterize)
            : this("t", parameterize)

        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QBuilder"/> class with the specified alias table name and parameterization option.
        /// </summary>
        /// <param name="aliasTablename">The alias for the main table in the query.</param>
        /// <param name="parameterize">Determines whether the query should be parameterized for secure parameter binding.</param>
        public QBuilder(string aliasTablename, bool parameterize)
            : this(t => t.Name, aliasTablename, parameterize)

        {
        }

        public QBuilder(Func<Type, string> tableNameResolver, string aliasTablename, bool parameterize)
        {
            TableNameResolver = tableNameResolver;
            _aliasTableName = aliasTablename;
            TableNameAliaser = new TableNameAliaser(tableNameResolver);
            _orderBuilder = new OrderBuilder(this);
            _selectBuilder = new SelectBuilder(this, "t");
            _joinBuilder = new JoinBuilder(this);
            _groupBuilder = new GroupBuilder(this);
            if (parameterize)
            {
                builtQuery = new BuiltQuery();
            }
            _whereBuilder = new WhereBuilder(this, builtQuery);
            _havingBuilder = new HavingBuilder(this, builtQuery);
        }

        internal void SetSuffix(string suffix)
        {
            _suffix = suffix;
        }

        public string DerivedTableName => _aliasTableName;

        internal TableNameAliaser TableNameAliaser { get; set; }

        internal InnerSelectDescription InnerSelectDescription { get; set; }

        internal Func<Type, string> TableNameResolver { get; set; }

        internal string FirstTableName
        {
            get
            {
                if (UseJoiner().JoinsExist == false)
                {
                    return UseSelector().FirstTableName;
                }
                else
                {
                    return UseJoiner().FirstTableName;
                }
            }
        }

        private DataValidator DataValidator { get; } = new DataValidator();

        /// <summary>
        /// Gets the <see cref="SelectBuilder"/> instance for constructing the SELECT clause of the query.
        /// </summary>
        /// <returns>The <see cref="SelectBuilder"/> instance.</returns>
        public SelectBuilder UseSelector()
        {
            return _selectBuilder;
        }


        /// <summary>
        /// Gets the <see cref="TableBoundSelectBuilder{TTable}"/> instance for constructing the SELECT clause of the query for a specific table.
        /// </summary>
        /// <typeparam name="TTable">The type of the table to select from.</typeparam>
        /// <returns>The <see cref="TableBoundSelectBuilder{TTable}"/> instance.</returns>
        public TableBoundSelectBuilder<TTable> UseTableBoundSelector<TTable>()
        {
            return new TableBoundSelectBuilder<TTable>(this, _selectBuilder);
        }

        /// <summary>
        /// Gets the <see cref="TableBoundGroupBuilder{TTable}"/> instance for constructing the GROUP BY clause of the query for a specific table.
        /// </summary>
        /// <typeparam name="TTable">The type of the table to group by.</typeparam>
        /// <returns>The <see cref="TableBoundGroupBuilder{TTable}"/> instance.</returns>
        public TableBoundGroupBuilder<TTable> UseTableBoundGrouper<TTable>()
        {
            return new TableBoundGroupBuilder<TTable>(this);
        }

        /// <summary>
        /// Gets the <see cref="TableBoundJoinBuilder{TLeftTable, TRightTable}"/> instance for constructing a join between two tables.
        /// </summary>
        /// <typeparam name="TLeftTable">The type of the left table in the join.</typeparam>
        /// <typeparam name="TRightTable">The type of the right table in the join.</typeparam>
        /// <returns>The <see cref="TableBoundJoinBuilder{TLeftTable, TRightTable}"/> instance.</returns>
        public TableBoundJoinBuilder<TLeftTable, TRightTable> UseTableBoundJoinBuilder<TLeftTable, TRightTable>()
        {
            return new TableBoundJoinBuilder<TLeftTable, TRightTable>(this);
        }

        /// <summary>
        /// Gets the <see cref="TableBoundWhereBuilder{TTable}"/> instance for constructing the WHERE clause of the query for a specific table.
        /// </summary>
        /// <typeparam name="TTable">The type of the table to filter.</typeparam>
        /// <returns>The <see cref="TableBoundWhereBuilder{TTable}"/> instance.</returns>
        public TableBoundWhereBuilder<TTable> UseTableBoundFilter<TTable>()
        {
            return new TableBoundWhereBuilder<TTable>(_whereBuilder, this);
        }

        /// <summary>
        /// Gets the <see cref="DerivedTableSelector"/> instance for selecting from a derived table.
        /// </summary>
        /// <param name="derivedTable">The <see cref="QBuilder"/> instance representing the derived table.</param>
        /// <returns>The <see cref="DerivedTableSelector"/> instance.</returns>
        public DerivedTableSelector UseDerivedTableSelector(QBuilder derivedTable)
        {
            return new DerivedTableSelector(derivedTable, _selectBuilder);
        }

        /// <summary>
        /// Gets the <see cref="IPagingBuilder{TTable}"/> instance for constructing paging logic using SQL Server syntax.
        /// </summary>
        /// <typeparam name="TTable">The type of the table to apply paging to.</typeparam>
        /// <returns>The <see cref="IPagingBuilder{TTable}"/> instance.</returns>
        public IPagingBuilder<TTable> UseSqlServerPagingBuilder<TTable>()
        {
            return UsePagingBuilder<TTable, SqlServerPagingBuilder<TTable>>(new SqlServerPagingBuilder<TTable>(this));
        }

        /// <summary>
        /// Gets the <see cref="IPagingBuilder{TTable}"/> instance for constructing paging logic using MySQL Server syntax.
        /// </summary>
        /// <typeparam name="TTable">The type of the table to apply paging to.</typeparam>
        /// <returns>The <see cref="IPagingBuilder{TTable}"/> instance.</returns>
        public IPagingBuilder<TTable> UseMySqlServerPagingBuilder<TTable>()
        {
            return UsePagingBuilder<TTable, MySqlServerPagingBuilder<TTable>>(new MySqlServerPagingBuilder<TTable>(this));
        }

        /// <summary>
        /// Gets an <see cref="IPagingBuilder{TTable}"/> that uses SQL Server 2012+ / ANSI
        /// <c>OFFSET … ROWS FETCH NEXT … ROWS ONLY</c> syntax.
        /// </summary>
        /// <typeparam name="TTable">The table whose field drives the ORDER BY clause.</typeparam>
        /// <returns>The <see cref="OffsetFetchPagingBuilder{TTable}"/> instance.</returns>
        public IPagingBuilder<TTable> UseOffsetFetchPagingBuilder<TTable>()
        {
            return UsePagingBuilder<TTable, OffsetFetchPagingBuilder<TTable>>(new OffsetFetchPagingBuilder<TTable>(this));
        }

        /// <summary>
        /// Gets the <see cref="IPagingBuilder{TTable}"/> instance for constructing paging logic using a custom paging builder.
        /// </summary>
        /// <typeparam name="TTable">The type of the table to apply paging to.</typeparam>
        /// <typeparam name="TBuilderPagingBuilder">The type of the custom paging builder.</typeparam>
        /// <param name="pagingBuilder">The instance of the custom paging builder.</param>
        /// <returns>The <see cref="IPagingBuilder{TTable}"/> instance.</returns>
        public IPagingBuilder<TTable> UsePagingBuilder<TTable, TBuilderPagingBuilder>(TBuilderPagingBuilder pagingBuilder)
            where TBuilderPagingBuilder : BuilderBase, IPagingBuilder<TTable>
        {
            return pagingBuilder;
        }

        /// <summary>
        /// Gets the <see cref="JoinBuilder"/> instance for constructing joins between tables.
        /// </summary>
        /// <returns>The <see cref="JoinBuilder"/> instance.</returns>
        public JoinBuilder UseJoiner()
        {
            return _joinBuilder;
        }

        /// <summary>
        /// Gets the <see cref="WhereBuilder"/> instance for constructing the WHERE clause of the query.
        /// </summary>
        /// <returns>The <see cref="WhereBuilder"/> instance.</returns>
        public WhereBuilder UseFilter()
        {
            return _whereBuilder;
        }

        /// <summary>
        /// Gets the <see cref="OrderBuilder"/> instance for constructing the ORDER BY clause of the query.
        /// </summary>
        /// <returns>The <see cref="OrderBuilder"/> instance.</returns>
        public OrderBuilder UseOrdering()
        {
            return _orderBuilder;
        }

        /// <summary>
        /// Gets the <see cref="GroupBuilder"/> instance for constructing the GROUP BY clause of the query.
        /// </summary>
        /// <returns>The <see cref="GroupBuilder"/> instance.</returns>
        public GroupBuilder UseGrouper()
        {
            return _groupBuilder;
        }

        /// <summary>
        /// Gets the <see cref="HavingBuilder"/> instance for filtering grouped rows via a HAVING clause.
        /// Must be used after <see cref="UseGrouper"/> has added at least one GROUP BY column.
        /// </summary>
        /// <returns>The <see cref="HavingBuilder"/> instance.</returns>
        public HavingBuilder UseHaving()
        {
            return _havingBuilder;
        }

        /// <summary>
        /// Defines a named Common Table Expression (CTE) that precedes the main query.
        /// Call multiple times to define multiple CTEs — they are emitted in order as a comma-separated WITH block.
        /// </summary>
        /// <param name="name">The CTE name referenced in the main query or subsequent CTEs.</param>
        /// <param name="cteQuery">A <see cref="QBuilder"/> whose built SQL becomes the CTE body.</param>
        /// <returns>This <see cref="QBuilder"/> for continued chaining.</returns>
        public QBuilder WithCte(string name, QBuilder cteQuery)
        {
            _ctes.Add(new CteDescription { Name = name, Sql = cteQuery.Build() });
            return this;
        }

        /// <summary>
        /// Appends a UNION (distinct rows) of <paramref name="other"/> to this query.
        /// </summary>
        /// <param name="other">The right-hand query.</param>
        /// <returns>This <see cref="QBuilder"/> for continued chaining.</returns>
        public QBuilder Union(QBuilder other)
        {
            _setOperations.Add(new SetOperationDescription { Sql = other.Build(), OperationType = SetOperationType.Union });
            return this;
        }

        /// <summary>
        /// Appends a UNION ALL (all rows, including duplicates) of <paramref name="other"/> to this query.
        /// </summary>
        /// <param name="other">The right-hand query.</param>
        /// <returns>This <see cref="QBuilder"/> for continued chaining.</returns>
        public QBuilder UnionAll(QBuilder other)
        {
            _setOperations.Add(new SetOperationDescription { Sql = other.Build(), OperationType = SetOperationType.UnionAll });
            return this;
        }

        /// <summary>
        /// Appends an INTERSECT of <paramref name="other"/> — rows common to both queries.
        /// </summary>
        /// <param name="other">The right-hand query.</param>
        /// <returns>This <see cref="QBuilder"/> for continued chaining.</returns>
        public QBuilder Intersect(QBuilder other)
        {
            _setOperations.Add(new SetOperationDescription { Sql = other.Build(), OperationType = SetOperationType.Intersect });
            return this;
        }

        /// <summary>
        /// Appends an EXCEPT of <paramref name="other"/> — rows in this query that are absent from <paramref name="other"/>.
        /// </summary>
        /// <param name="other">The right-hand query.</param>
        /// <returns>This <see cref="QBuilder"/> for continued chaining.</returns>
        public QBuilder Except(QBuilder other)
        {
            _setOperations.Add(new SetOperationDescription { Sql = other.Build(), OperationType = SetOperationType.Except });
            return this;
        }

        /// <summary>
        /// Builds the SQL query string based on the configured components (SELECT, JOIN, WHERE, GROUP BY, ORDER BY).
        /// </summary>
        /// <returns>The SQL query string.</returns>
        public string Build()
        {
            if (_isBuilt)
            {
                throw new InvalidOperationException("Build() has already been called on this QBuilder instance. Create a new QBuilder to build a new query.");
            }
            _isBuilt = true;
            DataValidator.EvaluateImmediate(string.IsNullOrEmpty(FirstTableName), "There are no tables queued for data querying. Nothing to return");

            var query = UseSelector().Build()
                + UseJoiner().Build()
                + UseFilter().Build()
                + UseGrouper().Build()
                + UseHaving().Build()
                + UseOrdering().Build();
            var wrappedQuery = GetWrappedInSelectAlias(query);
            var finalQuery = GetWithInnerSelectJoinIfRequired(wrappedQuery);
            var suffixedQuery = (finalQuery + " " + _suffix).Trim();
            suffixedQuery = AppendSetOperations(suffixedQuery);
            suffixedQuery = PrependCtes(suffixedQuery);
            return suffixedQuery;
        }

        /// <summary>
        /// Builds the parameterized SQL query string based on the configured components (SELECT, JOIN, WHERE, GROUP BY, ORDER BY).
        /// </summary>
        /// <returns>The <see cref="BuiltQuery"/> object containing the parameterized SQL query string.</returns>
        public BuiltQuery BuildWithParameters()
        {
            if (builtQuery == null)
            {
                throw new InvalidOperationException($"This QBuilder was created with parameterize: false. Use {nameof(Build)}() instead, or create a new QBuilder with parameterize: true.");
            }
            builtQuery.ParameterizedSql = Build();
            return builtQuery;
        }



        private string AppendSetOperations(string sql)
        {
            foreach (var op in _setOperations)
            {
                var keyword = op.OperationType switch
                {
                    SetOperationType.Union => "Union",
                    SetOperationType.UnionAll => "Union All",
                    SetOperationType.Intersect => "Intersect",
                    SetOperationType.Except => "Except",
                    _ => throw new InvalidOperationException($"Unknown SetOperationType '{op.OperationType}'"),
                };
                sql += $"{Environment.NewLine}{keyword}{Environment.NewLine}{op.Sql}";
            }
            return sql;
        }

        private string PrependCtes(string sql)
        {
            if (_ctes.Count == 0)
            {
                return sql;
            }

            var cteBlock = "With ";
            for (var i = 0; i < _ctes.Count; i++)
            {
                var cte = _ctes[i];
                cteBlock += $"{cte.Name} As ({Environment.NewLine}{cte.Sql}{Environment.NewLine})";
                if (i < _ctes.Count - 1)
                {
                    cteBlock += ",";
                }
                cteBlock += Environment.NewLine;
            }

            return cteBlock + sql;
        }

        private string GetWrappedInSelectAlias(string query)
        {
            var result = $"Select * from ({query}) as {DerivedTableName}";
            result += $"{Environment.NewLine}";
            return result;
        }

        private string GetWithInnerSelectJoinIfRequired(string query)
        {
            var joiner = UseJoiner();
            if (joiner.InnerSelectDescription == null)
            {
                return query;
            }
            else
            {
                var innerSelect = joiner.InnerSelectDescription;
                var derivedTableQuery = innerSelect.QBuilder.Build();
                var resultQuery = $"{query} Join {derivedTableQuery}"
                    + $" on {innerSelect.DerivedTableName}.{innerSelect.InnerField} = {DerivedTableName}.{innerSelect.Field}";
                return resultQuery;
            }
        }

        public override string ToString()
        {
            var hasAlias = !string.IsNullOrEmpty(_aliasTableName);
            if (hasAlias)
            {
                return _aliasTableName;
            }
            else
            {
                return base.ToString();
            }
        }

        #region IDisposable Support

        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _selectBuilder = null;
                    _orderBuilder = null;
                    _joinBuilder = null;
                    _whereBuilder = null;
                    _havingBuilder = null;
                    _aliasTableName = string.Empty;
                    _groupBuilder = null;
                    _suffix = string.Empty;
                    TableNameResolver = null;
                    TableNameAliaser = null;
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}