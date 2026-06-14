namespace Jattac.Libraries.QBuilder.Builders
{
    using System;
    using System.Linq.Expressions;
    using Jattac.Libraries.QBuilder.Helpers;

    public class TableBoundGroupBuilder<TTable> : BuilderBase
    {
        public TableBoundGroupBuilder(QBuilder qBuilder)
         : base(qBuilder)
        {
        }

        public QBuilder GroupBy<TField>(Expression<Func<TTable, TField>> fieldNameDescriber)
        {
            var fieldName = new FieldNameResolver().GetFieldName(fieldNameDescriber);
            QBuilder.UseGrouper()
            .GroupBy<TTable>(fieldName);
            return QBuilder;
        }
    }
}