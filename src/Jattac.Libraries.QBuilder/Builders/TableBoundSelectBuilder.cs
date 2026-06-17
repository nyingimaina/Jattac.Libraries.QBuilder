namespace Jattac.Libraries.QBuilder.Builders
{
    using System;
    using System.Linq.Expressions;
    using Jattac.Libraries.QBuilder.Enums;
    using Jattac.Libraries.QBuilder.Helpers;

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

        public TableBoundSelectBuilder<TTable> Top(long count)
        {
            _selectBuilder.SelectTop(count);
            return this;
        }

        public TableBoundSelectBuilder<TTable> Distinct()
        {
            _selectBuilder.SelectDistinctRows();
            return this;
        }

        public TableBoundSelectBuilder<TTable> Column<TField>(Expression<Func<TTable, TField>> fieldSelector)
        {
            _selectBuilder.Select(fieldSelector);
            return this;
        }

        public TableBoundSelectBuilder<TTable> Column<TField>(Expression<Func<TTable, TField>> fieldSelector, string fieldAlias)
        {
            // Pass null explicitly so C# resolves to the 3-param overload (fieldAlias, explicitTableAlias)
            // rather than the 2-param overload (explicitTableAlias) which would treat the alias as a table prefix.
            _selectBuilder.Select(fieldSelector, fieldAlias, null);
            return this;
        }

        public TableBoundSelectBuilder<TTable> Aggregate<TField>(
            Expression<Func<TTable, TField>> fieldSelector,
            string alias,
            AggregateFunction function,
            string tableAlias = null)
        {
            var fnSql = function switch
            {
                AggregateFunction.Count => "Count",
                AggregateFunction.CountDistinct => "CountDistinct",
                AggregateFunction.Sum => "Sum",
                AggregateFunction.Avg => "Avg",
                AggregateFunction.Min => "Min",
                AggregateFunction.Max => "Max",
                _ => throw new ArgumentOutOfRangeException(nameof(function), function, null),
            };

            if (function == AggregateFunction.CountDistinct)
            {
                var fieldName = new FieldNameResolver().GetFieldName(fieldSelector);
                var table = tableAlias ?? QBuilder.TableNameAliaser.GetTableAlias<TTable>();
                _selectBuilder.SelectExplicit(
                    string.Empty,
                    $"Count(Distinct {table}.{fieldName})",
                    alias,
                    string.Empty,
                    preventTableNameAliasing: true,
                    qualifyFieldWithTableName: false);
            }
            else
            {
                _selectBuilder.SelectAggregated(fieldSelector, alias, fnSql, tableAlias);
            }

            return this;
        }

        public TableBoundSelectBuilder<TTable> CaseWhen(CaseWhenBuilder caseWhen, string alias)
        {
            _selectBuilder.SelectCaseWhen(caseWhen, alias);
            return this;
        }

        // ─── kept for backward compat within test suite ───────────────────────────

        /// <inheritdoc cref="Column{TField}(Expression{Func{TTable,TField}})"/>
        public TableBoundSelectBuilder<TTable> Select<TField>(Expression<Func<TTable, TField>> fieldSelector)
            => Column(fieldSelector);

        /// <inheritdoc cref="Column{TField}(Expression{Func{TTable,TField}}, string)"/>
        public TableBoundSelectBuilder<TTable> Select<TField>(Expression<Func<TTable, TField>> fieldSelector, string fieldAlias)
            => Column(fieldSelector, fieldAlias);

        public TableBoundSelectBuilder<TTable> SelectDistinctRows()
            => Distinct();

        public TableBoundSelectBuilder<TTable> SelectTop(long count)
            => Top(count);

        public TableBoundSelectBuilder<TTable> SelectAggregated<TField>(Expression<Func<TTable, TField>> fieldSelector, string fieldAlias, string aggregateFunction)
        {
            _selectBuilder.SelectAggregated(fieldSelector, fieldAlias, aggregateFunction);
            return this;
        }
    }
}