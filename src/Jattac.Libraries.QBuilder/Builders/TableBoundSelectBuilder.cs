namespace Jattac.Libraries.QBuilder.Builders
{
    using System;
    using System.Linq.Expressions;

    public class TableBoundSelectBuilder<TTable> : BuilderBase
    {
        private SelectBuilder _selectBuilder;

        public TableBoundSelectBuilder(QBuilder qBuilder, SelectBuilder selectBuilder)
            : this(qBuilder)
        {
            _selectBuilder = selectBuilder;
        }

        public TableBoundSelectBuilder(QBuilder qBuilder)
            : base(qBuilder)
        {
        }

        public TableBoundSelectBuilder<TTable> SelectTop(long count)
        {
            _selectBuilder.SelectTop(count);
            return this;
        }

        public TableBoundSelectBuilder<TTable> Select<TField>(Expression<Func<TTable, TField>> fieldNameResolver)
        {
            _selectBuilder.Select(fieldNameResolver);
            return this;
        }

        public TableBoundSelectBuilder<TTable> Select<TField>(Expression<Func<TTable, TField>> fieldNameResolver, string fieldAlias)
        {
            // Pass null explicitly so C# resolves to the 3-param overload (fieldAlias, explicitTableAlias)
            // rather than the 2-param overload (explicitTableAlias) which would treat the alias as a table prefix.
            _selectBuilder.Select(fieldNameResolver, fieldAlias, null);
            return this;
        }

        public TableBoundSelectBuilder<TTable> SelectAggregated<TField>(Expression<Func<TTable, TField>> fieldNameResolver, string fieldAlias, string aggregateFunction)
        {
            _selectBuilder.SelectAggregated(fieldNameResolver, fieldAlias, aggregateFunction);
            return this;
        }

        public TableBoundSelectBuilder<TTable> SelectDistinctRows()
        {
            _selectBuilder.SelectDistinctRows();
            return this;
        }
    }
}