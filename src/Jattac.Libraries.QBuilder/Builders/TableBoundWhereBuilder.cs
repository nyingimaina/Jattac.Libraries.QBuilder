namespace Jattac.Libraries.QBuilder.Builders
{
    using Jattac.Libraries.QBuilder.Helpers;
    using Jattac.Libraries.QBuilder.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    public class TableBoundWhereBuilder<TTable> : BuilderBase
    {
        private WhereBuilder _whereBuilder;

        private FieldNameResolver _fieldNameResolver;

        public TableBoundWhereBuilder(WhereBuilder whereBuilder, QBuilder qBuilder)
            : base(qBuilder)
        {
            _whereBuilder = whereBuilder;
            _fieldNameResolver = new FieldNameResolver();
        }

        public WhereConjunctionBuilder Where(FilterDescription<TTable> filterDescription)
        {
            return _whereBuilder.Where(filterDescription);
        }

        public WhereConjunctionBuilder Where<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, string condition)
        {
            var fieldName = _fieldNameResolver.GetFieldName(fieldNameDescriptor);
            return _whereBuilder.Where<TTable>(fieldName, condition);
        }

        public WhereConjunctionBuilder Where<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, FilterOperator op, object value)
        {
            var fieldName = _fieldNameResolver.GetFieldName(fieldNameDescriptor);
            return _whereBuilder.Where<TTable>(fieldName, op, value);
        }

        public WhereConjunctionBuilder WhereIn<TField, TValueType>(Expression<Func<TTable, TField>> fieldNameDescriptor, IEnumerable<TValueType> values)
        {
            var fieldName = _fieldNameResolver.GetFieldName(fieldNameDescriptor);
            return _whereBuilder.WhereIn<TTable, TValueType>(fieldName, values);
        }

        public WhereConjunctionBuilder WhereNotIn<TField, TValueType>(Expression<Func<TTable, TField>> fieldNameDescriptor, IEnumerable<TValueType> values)
        {
            var fieldName = _fieldNameResolver.GetFieldName(fieldNameDescriptor);
            return _whereBuilder.WhereNotIn<TTable, TValueType>(fieldName, values);
        }

        public WhereConjunctionBuilder OptionalWhere<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, Func<string> fnResolveCondition)
        {
            var fieldName = _fieldNameResolver.GetFieldName(fieldNameDescriptor);
            return _whereBuilder.OptionalWhere<TTable>(fieldName, fnResolveCondition);
        }

        public WhereConjunctionBuilder WhereExplicitly(string criteria)
        {
            return _whereBuilder.WhereExplicitly(criteria);
        }

        public TableBoundWhereBuilder<TTable> OpenParentheses()
        {
            _whereBuilder.OpenParentheses();
            return this;
        }

        public TableBoundWhereBuilder<TTable> CloseParentheses()
        {
            _whereBuilder.CloseParentheses();
            return this;
        }

        public WhereConjunctionBuilder WhereEqualTo<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, object value)
        {
            return Where(fieldNameDescriptor, FilterOperator.EqualTo, value);
        }

        public WhereConjunctionBuilder WhereLessThan<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, object value)
        {
            return Where(fieldNameDescriptor, FilterOperator.LessThan, value);
        }

        public WhereConjunctionBuilder WhereLessThanOrEqualTo<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, object value)
        {
            return Where(fieldNameDescriptor, FilterOperator.LessThanOrEqualTo, value);
        }

        public WhereConjunctionBuilder WhereGreaterThan<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, object value)
        {
            return Where(fieldNameDescriptor, FilterOperator.GreaterThan, value);
        }

        public WhereConjunctionBuilder WhereGreaterThanOrEqualTo<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, object value)
        {
            return Where(fieldNameDescriptor, FilterOperator.GreaterThanOrEqualTo, value);
        }

        public WhereConjunctionBuilder WhereStartsWith<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, object value)
        {
            return Where(fieldNameDescriptor, FilterOperator.StartsWith, value);
        }

        public WhereConjunctionBuilder WhereContains<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, object value)
        {
            return Where(fieldNameDescriptor, FilterOperator.Contains, value);
        }

        public WhereConjunctionBuilder WhereEndsWith<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, object value)
        {
            return Where(fieldNameDescriptor, FilterOperator.EndsWith, value);
        }

        public WhereConjunctionBuilder WhereNotEqualTo<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, object value)
        {
            return Where(fieldNameDescriptor, FilterOperator.NotEqualTo, value);
        }
    }
}