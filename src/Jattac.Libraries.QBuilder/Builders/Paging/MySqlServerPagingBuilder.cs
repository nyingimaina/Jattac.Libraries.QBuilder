namespace Jattac.Libraries.QBuilder.Builders.Paging
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Text;
    using Jattac.Libraries.QBuilder.Enums;
    using Jattac.Libraries.QBuilder.Helpers;

    public class MySqlServerPagingBuilder<TTable> : BuilderBase, IPagingBuilder<TTable>
    {
        public MySqlServerPagingBuilder(QBuilder qBuilder)
            : base(qBuilder)
        {
        }

        public QBuilder PageBy<TField>(Expression<Func<TTable, TField>> fieldNameDescriber, uint page, ushort pageSize, bool orderAscending = true)
        {
            var fieldName = new FieldNameResolver().GetFieldName(fieldNameDescriber);
            var range = PageRangeCalculator.GetPageRange(0, page, pageSize);
            var qField = IdentifierQuoter.QuoteIdentifier(fieldName, Dialect.MySql);
            var direction = orderAscending ? "Asc" : "Desc";
            QBuilder.SetSuffix($" Order By {qField} {direction} Limit {range.Start},{range.PageSize}");
            return QBuilder;
        }
    }
}