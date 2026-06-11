namespace Rocket.Libraries.Qurious.Builders
{
    public class WhereConjunctionBuilder : BuilderBase
    {
        private readonly WhereBuilder _whereBuilder;

        public WhereConjunctionBuilder(WhereBuilder whereBuilder, QBuilder qBuilder)
            : this(qBuilder)
        {
            _whereBuilder = whereBuilder;
        }

        public WhereConjunctionBuilder(QBuilder qBuilder)
            : base(qBuilder)
        {
        }

        public WhereBuilder And()
        {
            _whereBuilder.SetNextConjunction("And");
            return _whereBuilder;
        }

        public WhereBuilder Or()
        {
            _whereBuilder.SetNextConjunction("Or");
            return _whereBuilder;
        }

        public TableBoundWhereBuilder<TTable> And<TTable>()
        {
            And();
            return QBuilder.UseTableBoundFilter<TTable>();
        }

        public TableBoundWhereBuilder<TTable> Or<TTable>()
        {
            Or();
            return QBuilder.UseTableBoundFilter<TTable>();
        }

        public WhereConjunctionBuilder OpenParentheses()
        {
            _whereBuilder.OpenParentheses();
            return this;
        }

        public WhereConjunctionBuilder CloseParentheses()
        {
            _whereBuilder.CloseParentheses();
            return this;
        }
    }
}