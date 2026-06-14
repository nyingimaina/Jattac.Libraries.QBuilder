namespace Jattac.Libraries.QBuilder.Builders
{
    using System;
    using System.Linq.Expressions;
    using Jattac.Libraries.QBuilder.Helpers;

    public class TableBoundJoinBuilder<TLeftTable, TRightTable> : BuilderBase
    {
        private FieldNameResolver _fieldNameResolver;

        public TableBoundJoinBuilder(QBuilder qBuilder) : base(qBuilder)
        {
            _fieldNameResolver = new FieldNameResolver();
        }

        public QBuilder InnerJoin<TLeftField, TRightField>(Expression<Func<TLeftTable, TLeftField>> leftFieldNameDescriptor, Expression<Func<TRightTable, TRightField>> rightFieldNameDescriptor, string leftAlias = null, string rightAlias = null)
        {
            var leftField = _fieldNameResolver.GetFieldName(leftFieldNameDescriptor);
            var rightField = _fieldNameResolver.GetFieldName(rightFieldNameDescriptor);
            QBuilder.UseJoiner().InnerJoin<TLeftTable, TRightTable>(leftField, rightField, leftAlias, rightAlias);
            return QBuilder;
        }

        public QBuilder FullJoin<TLeftField, TRightField>(Expression<Func<TLeftTable, TLeftField>> leftFieldNameDescriptor, Expression<Func<TRightTable, TRightField>> rightFieldNameDescriptor, string leftAlias = null, string rightAlias = null)
        {
            var leftField = _fieldNameResolver.GetFieldName(leftFieldNameDescriptor);
            var rightField = _fieldNameResolver.GetFieldName(rightFieldNameDescriptor);
            QBuilder.UseJoiner().FullJoin<TLeftTable, TRightTable>(leftField, rightField, leftAlias, rightAlias);
            return QBuilder;
        }

        public QBuilder LeftJoin<TLeftField, TRightField>(Expression<Func<TLeftTable, TLeftField>> leftFieldNameDescriptor, Expression<Func<TRightTable, TRightField>> rightFieldNameDescriptor, string leftAlias = null, string rightAlias = null)
        {
            var leftField = _fieldNameResolver.GetFieldName(leftFieldNameDescriptor);
            var rightField = _fieldNameResolver.GetFieldName(rightFieldNameDescriptor);
            QBuilder.UseJoiner().LeftJoin<TLeftTable, TRightTable>(leftField, rightField, leftAlias, rightAlias);
            return QBuilder;
        }

        public QBuilder RightJoin<TLeftField, TRightField>(Expression<Func<TLeftTable, TLeftField>> leftFieldNameDescriptor, Expression<Func<TRightTable, TRightField>> rightFieldNameDescriptor, string leftAlias = null, string rightAlias = null)
        {
            var leftField = _fieldNameResolver.GetFieldName(leftFieldNameDescriptor);
            var rightField = _fieldNameResolver.GetFieldName(rightFieldNameDescriptor);
            QBuilder.UseJoiner().RightJoin<TLeftTable, TRightTable>(leftField, rightField, leftAlias, rightAlias);
            return QBuilder;
        }
    }
}
