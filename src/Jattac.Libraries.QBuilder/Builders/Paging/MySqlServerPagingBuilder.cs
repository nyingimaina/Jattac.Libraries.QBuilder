namespace Jattac.Libraries.QBuilder.Builders.Paging
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Text;
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
            var tableName = QBuilder.TableNameAliaser.GetTableAlias<TTable>();
            var range = PageRangeCalculator.GetPageRange(0, page, pageSize);
            var orderClause = $"Order By `{fieldName}`";
            if (orderAscending)
            {
                orderClause += " Asc";
            }
            else
            {
                orderClause += " Desc";
            }
            QBuilder.SetSuffix($" {orderClause} Limit {range.Start},{range.PageSize}");
            return QBuilder;
        }
    }
}