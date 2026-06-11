namespace Rocket.Libraries.Qurious.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Rocket.Libraries.Qurious.Helpers;
    using Rocket.Libraries.Validation.Services;

    public class SelectBuilder : BuilderBase
    {
        private List<SelectDescription> _selects = new List<SelectDescription>();

        private bool _distinctRows = false;

        private long? _top;

        private FieldNameResolver _fieldNameResolver;

        private string _selectPrefix;

        internal SelectBuilder(QBuilder qBuilder, string selectAlias)
            : base(qBuilder)
        {
            SelectAlias = selectAlias;
            _fieldNameResolver = new FieldNameResolver();
        }

        internal string FirstTableName => _selects.FirstOrDefault()?.Table;

        private string SelectAlias { get; }

        public SelectBuilder SelectTop(long count)
        {
            _top = count;
            return this;
        }

        public SelectBuilder Select<TTable, TField>(Expression<Func<TTable, TField>> fieldNameResolver)
        {
            var field = _fieldNameResolver.GetFieldName(fieldNameResolver);
            return Select<TTable>(field);
        }

        public SelectBuilder Select<TTable>(string field)
        {
            return Select<TTable>(field, string.Empty);
        }

        public SelectBuilder Select<TTable, TField>(Expression<Func<TTable, TField>> fieldNameResolver, string fieldAlias)
        {
            var field = _fieldNameResolver.GetFieldName(fieldNameResolver);
            return Select<TTable>(field, fieldAlias);
        }

        public SelectBuilder Select<TTable>(string field, string fieldAlias)
        {
            return SelectAggregated<TTable>(field, fieldAlias, string.Empty);
        }

        public SelectBuilder SelectAggregated<TTable, TField>(Expression<Func<TTable, TField>> fieldNameResolver, string fieldAlias, string aggregateFunction)
        {
            var field = _fieldNameResolver.GetFieldName(fieldNameResolver);
            return SelectAggregated<TTable>(field, fieldAlias, aggregateFunction);
        }

        public SelectBuilder SelectAggregated<TTable>(string field, string fieldAlias, string aggregateFunction)
        {
            var table = QBuilder.TableNameResolver(typeof(TTable));
            return SelectExplicit(table, field, fieldAlias, aggregateFunction, false);
        }

        public SelectBuilder SelectExplicit(string table, string field, string fieldAlias, string aggregateFunction, bool preventTableNameAliasing, bool qualifyFieldWithTableName = true)
        {
            _selects.Add(new SelectDescription
            {
                Field = field,
                Table = table,
                FieldAlias = fieldAlias,
                AggregateFunction = aggregateFunction,
                TableNameAliasingPrevented = preventTableNameAliasing,
                QualifyFieldWithTableName = qualifyFieldWithTableName
            });
            return this;
        }

        /// <summary>
        /// Adds a <c>CASE WHEN … END AS alias</c> expression to the SELECT list.
        /// Use <see cref="CaseWhenBuilder"/> to construct the expression.
        /// </summary>
        /// <param name="caseWhen">A <see cref="CaseWhenBuilder"/> that has at least one When/Then pair.</param>
        /// <param name="alias">The column alias for the CASE expression in the result set.</param>
        public SelectBuilder SelectCaseWhen(CaseWhenBuilder caseWhen, string alias)
        {
            var sql = caseWhen.Build();
            return SelectExplicit(string.Empty, sql, alias, string.Empty, preventTableNameAliasing: true, qualifyFieldWithTableName: false);
        }

        public SelectBuilder SelectDistinctRows()
        {
            _distinctRows = true;
            return this;
        }

        internal SelectBuilder SetSelectPrefix(string field)
        {
            _selectPrefix = field;
            return this;
        }

        internal string Build()
        {
            new DataValidator().EvaluateImmediate(_selects.Count == 0, "There are no fields queued for selection. Nothing to return");

            var selects = "Select ";
            if (_top.HasValue)
            {
                selects += $" Top {_top.Value} ";
            }
            if (_distinctRows)
            {
                selects += $" Distinct {Environment.NewLine}";
            }

            var hasPrefix = !string.IsNullOrEmpty(_selectPrefix);
            if (hasPrefix)
            {
                selects += $" {_selectPrefix}";
            }

            foreach (var selectDescription in _selects)
            {
                new DataValidator().EvaluateImmediate(TableNotKnown(selectDescription.Table), $"Table '{selectDescription.Table}' has not been queued as a datasource. Cannot show fields from it");
                var tableName = GetTableName(selectDescription);

                var qualifiedField = selectDescription.QualifyFieldWithTableName ? $"{tableName}.{selectDescription.Field}" : selectDescription.Field;
                var finalField = GetAggregatedFieldIfRequired(qualifiedField, selectDescription.AggregateFunction);
                selects += $"{Environment.NewLine}{finalField}";
                var hasAlias = !string.IsNullOrEmpty(selectDescription.FieldAlias);
                if (hasAlias)
                {
                    selects += $" as {selectDescription.FieldAlias}";
                }

                selects += ",";
            }

            var firstTableName = QBuilder.FirstTableName;
            selects = selects.Substring(0, selects.Length - 1) + $" From {firstTableName} " + QBuilder.TableNameAliaser.GetTableAlias(firstTableName);
            _selects = new List<SelectDescription>();
            return selects + Environment.NewLine;
        }

        private string GetTableName(SelectDescription selectDescription)
        {
            if (selectDescription.TableNameAliasingPrevented)
            {
                return selectDescription.Table;
            }
            else
            {
                return QBuilder.TableNameAliaser.GetTableAlias(selectDescription.Table);
            }
        }

        private string GetAggregatedFieldIfRequired(string qualifiedField, string aggregateFunction)
        {
            if (string.IsNullOrEmpty(aggregateFunction))
            {
                return qualifiedField;
            }
            else
            {
                return $"{aggregateFunction}({qualifiedField})";
            }
        }

        private bool TableNotKnown(string table)
        {
            var joiner = QBuilder.UseJoiner();
            if (joiner.JoinsExist == false)
            {
                return false;
            }
            else
            {
                return joiner.TableNotKnown(table);
            }
        }
    }
}