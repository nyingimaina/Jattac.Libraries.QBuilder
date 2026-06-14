namespace Jattac.Libraries.QBuilder.Builders
{
    using System;
    using System.Linq.Expressions;

    public class DerivedTableJoiner<TOuterTable>
    {
        private JoinBuilder _joinBuilder;

        public DerivedTableJoiner(JoinBuilder joinBuilder)
        {
            _joinBuilder = joinBuilder;
        }

        public JoinBuilder InnerJoin<TOuterField>(Expression<Func<TOuterTable, TOuterField>> outerFieldDescription, QBuilder derivedTable, string derivedFieldName)
        {
            _joinBuilder.JoinDerivedTable(outerFieldDescription, derivedTable, derivedFieldName, JoinTypes.Inner);
            return _joinBuilder;
        }

        public JoinBuilder LeftJoin<TOuterField>(Expression<Func<TOuterTable, TOuterField>> outerFieldDescription, QBuilder derivedTable, string derivedFieldName)
        {
            _joinBuilder.JoinDerivedTable(outerFieldDescription, derivedTable, derivedFieldName, JoinTypes.LeftJoin);
            return _joinBuilder;
        }

        public JoinBuilder RightJoin<TOuterField>(Expression<Func<TOuterTable, TOuterField>> outerFieldDescription, QBuilder derivedTable, string derivedFieldName)
        {
            _joinBuilder.JoinDerivedTable(outerFieldDescription, derivedTable, derivedFieldName, JoinTypes.RightJoin);
            return _joinBuilder;
        }

        public JoinBuilder FullJoin<TOuterField>(Expression<Func<TOuterTable, TOuterField>> outerFieldDescription, QBuilder derivedTable, string derivedFieldName)
        {
            _joinBuilder.JoinDerivedTable(outerFieldDescription, derivedTable, derivedFieldName, JoinTypes.Full);
            return _joinBuilder;
        }
    }
}