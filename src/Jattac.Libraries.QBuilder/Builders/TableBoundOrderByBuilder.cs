namespace Jattac.Libraries.QBuilder.Builders
{
    using System;
    using System.Linq.Expressions;

    /// <summary>
    /// Fluent, fully type-inferred ORDER BY builder scoped to <typeparamref name="TTable"/>.
    /// Use <see cref="BuilderBase.Then"/> to return to the parent <see cref="QBuilder"/>.
    /// </summary>
    public class TableBoundOrderByBuilder<TTable> : BuilderBase
    {
        public TableBoundOrderByBuilder(QBuilder qBuilder) : base(qBuilder)
        {
        }

        public TableBoundOrderByBuilder<TTable> Ascending<TField>(Expression<Func<TTable, TField>> fieldSelector, string tableAlias = null)
        {
            QBuilder.UseOrdering().OrderBy<TTable, TField>(fieldSelector, tableAlias);
            return this;
        }

        public TableBoundOrderByBuilder<TTable> Descending<TField>(Expression<Func<TTable, TField>> fieldSelector, string tableAlias = null)
        {
            QBuilder.UseOrdering().OrderByDescending<TTable, TField>(fieldSelector, tableAlias);
            return this;
        }

        public TableBoundOrderByBuilder<TTable> ThenAscending<TField>(Expression<Func<TTable, TField>> fieldSelector, string tableAlias = null)
            => Ascending(fieldSelector, tableAlias);

        public TableBoundOrderByBuilder<TTable> ThenDescending<TField>(Expression<Func<TTable, TField>> fieldSelector, string tableAlias = null)
            => Descending(fieldSelector, tableAlias);
    }
}
