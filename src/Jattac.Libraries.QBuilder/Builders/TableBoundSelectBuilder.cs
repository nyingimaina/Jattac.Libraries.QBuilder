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
            _selectBuilder.Select(fieldNameResolver, fieldAlias);
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