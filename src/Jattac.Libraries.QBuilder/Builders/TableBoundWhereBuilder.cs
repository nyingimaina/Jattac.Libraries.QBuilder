namespace Jattac.Libraries.QBuilder.Builders
{
    using Jattac.Libraries.QBuilder.Helpers;
    using Jattac.Libraries.QBuilder.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;

    /// <summary>
    /// Fluent, fully type-inferred WHERE clause builder scoped to <typeparamref name="TTable"/>.
    /// All predicate methods return <c>this</c> so the chain stays in one expression.
    /// Use <see cref="BuilderBase.Then"/> to return to the parent <see cref="QBuilder"/>.
    /// </summary>
    public class TableBoundWhereBuilder<TTable> : BuilderBase
    {
        private WhereBuilder _whereBuilder;
        private FieldNameResolver _fieldNameResolver;

        internal TableBoundWhereBuilder(WhereBuilder whereBuilder, QBuilder qBuilder)
            : base(qBuilder)
        {
            _whereBuilder = whereBuilder;
            _fieldNameResolver = new FieldNameResolver();
        }

        // ─── helpers ──────────────────────────────────────────────────────────────

        private TableBoundWhereBuilder<TTable> AddAnd<TField>(
            Expression<Func<TTable, TField>> descriptor, FilterOperator op, object value, string tableAlias = null)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.Where<TTable>(field, op, value, tableAlias);
            return this;
        }

        private TableBoundWhereBuilder<TTable> AddOr<TField>(
            Expression<Func<TTable, TField>> descriptor, FilterOperator op, object value, string tableAlias = null)
        {
            _whereBuilder.SetNextConjunction("Or");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.Where<TTable>(field, op, value, tableAlias);
            return this;
        }

        // ─── base Where ───────────────────────────────────────────────────────────

        public TableBoundWhereBuilder<TTable> Where(FilterDescription<TTable> filterDescription)
        {
            _whereBuilder.SetNextConjunction("And");
            _whereBuilder.Where(filterDescription);
            return this;
        }

        public TableBoundWhereBuilder<TTable> Where<TField>(Expression<Func<TTable, TField>> descriptor, string condition)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.Where<TTable>(field, condition);
            return this;
        }

        public TableBoundWhereBuilder<TTable> Where<TField>(Expression<Func<TTable, TField>> descriptor, FilterOperator op, object value)
            => AddAnd(descriptor, op, value);

        public TableBoundWhereBuilder<TTable> OptionalWhere<TField>(Expression<Func<TTable, TField>> descriptor, Func<string> fnResolveCondition)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.OptionalWhere<TTable>(field, fnResolveCondition);
            return this;
        }

        public TableBoundWhereBuilder<TTable> WhereExplicitly(string criteria)
        {
            _whereBuilder.SetNextConjunction("And");
            _whereBuilder.WhereExplicitly(criteria);
            return this;
        }

        // ─── equality ─────────────────────────────────────────────────────────────

        public TableBoundWhereBuilder<TTable> WhereEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.EqualTo, value);

        public TableBoundWhereBuilder<TTable> WhereNotEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.NotEqualTo, value);

        public TableBoundWhereBuilder<TTable> AndWhereEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.EqualTo, value);

        public TableBoundWhereBuilder<TTable> AndWhereNotEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.NotEqualTo, value);

        public TableBoundWhereBuilder<TTable> OrWhereEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddOr(descriptor, FilterOperator.EqualTo, value);

        public TableBoundWhereBuilder<TTable> OrWhereNotEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddOr(descriptor, FilterOperator.NotEqualTo, value);

        // ─── comparison ───────────────────────────────────────────────────────────

        public TableBoundWhereBuilder<TTable> WhereLessThan<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.LessThan, value);

        public TableBoundWhereBuilder<TTable> WhereLessThanOrEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.LessThanOrEqualTo, value);

        public TableBoundWhereBuilder<TTable> WhereGreaterThan<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.GreaterThan, value);

        public TableBoundWhereBuilder<TTable> WhereGreaterThanOrEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.GreaterThanOrEqualTo, value);

        public TableBoundWhereBuilder<TTable> AndWhereLessThan<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.LessThan, value);

        public TableBoundWhereBuilder<TTable> AndWhereLessThanOrEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.LessThanOrEqualTo, value);

        public TableBoundWhereBuilder<TTable> AndWhereGreaterThan<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.GreaterThan, value);

        public TableBoundWhereBuilder<TTable> AndWhereGreaterThanOrEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.GreaterThanOrEqualTo, value);

        public TableBoundWhereBuilder<TTable> OrWhereLessThan<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddOr(descriptor, FilterOperator.LessThan, value);

        public TableBoundWhereBuilder<TTable> OrWhereLessThanOrEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddOr(descriptor, FilterOperator.LessThanOrEqualTo, value);

        public TableBoundWhereBuilder<TTable> OrWhereGreaterThan<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddOr(descriptor, FilterOperator.GreaterThan, value);

        public TableBoundWhereBuilder<TTable> OrWhereGreaterThanOrEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddOr(descriptor, FilterOperator.GreaterThanOrEqualTo, value);

        // ─── string search ────────────────────────────────────────────────────────

        public TableBoundWhereBuilder<TTable> WhereContains<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.Contains, value);

        public TableBoundWhereBuilder<TTable> WhereStartsWith<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.StartsWith, value);

        public TableBoundWhereBuilder<TTable> WhereEndsWith<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.EndsWith, value);

        public TableBoundWhereBuilder<TTable> AndWhereContains<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.Contains, value);

        public TableBoundWhereBuilder<TTable> AndWhereStartsWith<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.StartsWith, value);

        public TableBoundWhereBuilder<TTable> AndWhereEndsWith<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.EndsWith, value);

        public TableBoundWhereBuilder<TTable> OrWhereContains<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddOr(descriptor, FilterOperator.Contains, value);

        public TableBoundWhereBuilder<TTable> OrWhereStartsWith<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddOr(descriptor, FilterOperator.StartsWith, value);

        public TableBoundWhereBuilder<TTable> OrWhereEndsWith<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddOr(descriptor, FilterOperator.EndsWith, value);

        // ─── null checks ──────────────────────────────────────────────────────────

        public TableBoundWhereBuilder<TTable> WhereIsNull<TField>(Expression<Func<TTable, TField>> descriptor)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereIsNull<TTable>(field);
            return this;
        }

        public TableBoundWhereBuilder<TTable> WhereIsNotNull<TField>(Expression<Func<TTable, TField>> descriptor)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereIsNotNull<TTable>(field);
            return this;
        }

        public TableBoundWhereBuilder<TTable> AndWhereIsNull<TField>(Expression<Func<TTable, TField>> descriptor)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereIsNull<TTable>(field);
            return this;
        }

        public TableBoundWhereBuilder<TTable> AndWhereIsNotNull<TField>(Expression<Func<TTable, TField>> descriptor)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereIsNotNull<TTable>(field);
            return this;
        }

        public TableBoundWhereBuilder<TTable> OrWhereIsNull<TField>(Expression<Func<TTable, TField>> descriptor)
        {
            _whereBuilder.SetNextConjunction("Or");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereIsNull<TTable>(field);
            return this;
        }

        public TableBoundWhereBuilder<TTable> OrWhereIsNotNull<TField>(Expression<Func<TTable, TField>> descriptor)
        {
            _whereBuilder.SetNextConjunction("Or");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereIsNotNull<TTable>(field);
            return this;
        }

        // ─── IN / NOT IN ──────────────────────────────────────────────────────────

        public TableBoundWhereBuilder<TTable> WhereIn<TField, TValueType>(Expression<Func<TTable, TField>> descriptor, IEnumerable<TValueType> values)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereIn<TTable, TValueType>(field, values);
            return this;
        }

        public TableBoundWhereBuilder<TTable> WhereNotIn<TField, TValueType>(Expression<Func<TTable, TField>> descriptor, IEnumerable<TValueType> values)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereNotIn<TTable, TValueType>(field, values);
            return this;
        }

        public TableBoundWhereBuilder<TTable> AndWhereIn<TField, TValueType>(Expression<Func<TTable, TField>> descriptor, IEnumerable<TValueType> values)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereIn<TTable, TValueType>(field, values);
            return this;
        }

        public TableBoundWhereBuilder<TTable> AndWhereNotIn<TField, TValueType>(Expression<Func<TTable, TField>> descriptor, IEnumerable<TValueType> values)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereNotIn<TTable, TValueType>(field, values);
            return this;
        }

        public TableBoundWhereBuilder<TTable> OrWhereIn<TField, TValueType>(Expression<Func<TTable, TField>> descriptor, IEnumerable<TValueType> values)
        {
            _whereBuilder.SetNextConjunction("Or");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereIn<TTable, TValueType>(field, values);
            return this;
        }

        public TableBoundWhereBuilder<TTable> OrWhereNotIn<TField, TValueType>(Expression<Func<TTable, TField>> descriptor, IEnumerable<TValueType> values)
        {
            _whereBuilder.SetNextConjunction("Or");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereNotIn<TTable, TValueType>(field, values);
            return this;
        }

        // ─── BETWEEN ──────────────────────────────────────────────────────────────

        public TableBoundWhereBuilder<TTable> WhereBetween<TField>(Expression<Func<TTable, TField>> descriptor, object from, object to)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereBetween<TTable>(field, from, to);
            return this;
        }

        public TableBoundWhereBuilder<TTable> WhereNotBetween<TField>(Expression<Func<TTable, TField>> descriptor, object from, object to)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereNotBetween<TTable>(field, from, to);
            return this;
        }

        public TableBoundWhereBuilder<TTable> AndWhereBetween<TField>(Expression<Func<TTable, TField>> descriptor, object from, object to)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereBetween<TTable>(field, from, to);
            return this;
        }

        public TableBoundWhereBuilder<TTable> AndWhereNotBetween<TField>(Expression<Func<TTable, TField>> descriptor, object from, object to)
        {
            _whereBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereNotBetween<TTable>(field, from, to);
            return this;
        }

        public TableBoundWhereBuilder<TTable> OrWhereBetween<TField>(Expression<Func<TTable, TField>> descriptor, object from, object to)
        {
            _whereBuilder.SetNextConjunction("Or");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereBetween<TTable>(field, from, to);
            return this;
        }

        public TableBoundWhereBuilder<TTable> OrWhereNotBetween<TField>(Expression<Func<TTable, TField>> descriptor, object from, object to)
        {
            _whereBuilder.SetNextConjunction("Or");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _whereBuilder.WhereNotBetween<TTable>(field, from, to);
            return this;
        }

        // ─── EXISTS ───────────────────────────────────────────────────────────────

        public TableBoundWhereBuilder<TTable> WhereExists(QBuilder subQuery)
        {
            _whereBuilder.SetNextConjunction("And");
            _whereBuilder.WhereExists(subQuery);
            return this;
        }

        public TableBoundWhereBuilder<TTable> WhereNotExists(QBuilder subQuery)
        {
            _whereBuilder.SetNextConjunction("And");
            _whereBuilder.WhereNotExists(subQuery);
            return this;
        }

        public TableBoundWhereBuilder<TTable> AndWhereExists(QBuilder subQuery)
        {
            _whereBuilder.SetNextConjunction("And");
            _whereBuilder.WhereExists(subQuery);
            return this;
        }

        public TableBoundWhereBuilder<TTable> AndWhereNotExists(QBuilder subQuery)
        {
            _whereBuilder.SetNextConjunction("And");
            _whereBuilder.WhereNotExists(subQuery);
            return this;
        }

        public TableBoundWhereBuilder<TTable> OrWhereExists(QBuilder subQuery)
        {
            _whereBuilder.SetNextConjunction("Or");
            _whereBuilder.WhereExists(subQuery);
            return this;
        }

        public TableBoundWhereBuilder<TTable> OrWhereNotExists(QBuilder subQuery)
        {
            _whereBuilder.SetNextConjunction("Or");
            _whereBuilder.WhereNotExists(subQuery);
            return this;
        }

        // ─── grouping ─────────────────────────────────────────────────────────────

        public TableBoundWhereBuilder<TTable> OpenGroup()
        {
            _whereBuilder.OpenParentheses();
            return this;
        }

        public TableBoundWhereBuilder<TTable> CloseGroup()
        {
            _whereBuilder.CloseParentheses();
            return this;
        }

        // ─── conditional builder (FEAT-2) ─────────────────────────────────────────

        /// <summary>
        /// Applies <paramref name="builder"/> only when <paramref name="condition"/> is true; otherwise no-ops.
        /// Keeps the fluent chain unbroken for optional filters.
        /// </summary>
        public TableBoundWhereBuilder<TTable> If(
            bool condition,
            Func<TableBoundWhereBuilder<TTable>, TableBoundWhereBuilder<TTable>> builder)
        {
            if (condition)
                builder(this);
            return this;
        }

        // ─── parameterized escape hatch (QoL-4) ───────────────────────────────────

        /// <summary>
        /// Injects a raw SQL fragment with optional named parameters.
        /// Parameter values from the anonymous <paramref name="parameters"/> object are merged into
        /// <see cref="BuiltQuery.Parameters"/> and safe for parameterized execution.
        /// When <paramref name="parameters"/> is null this behaves like the string-only overload.
        /// </summary>
        public TableBoundWhereBuilder<TTable> WhereExplicitly(string sql, object parameters)
        {
            _whereBuilder.SetNextConjunction("And");
            if (parameters != null)
            {
                var builtQuery = _whereBuilder.BuiltQueryRef;
                if (builtQuery != null)
                {
                    foreach (var prop in parameters.GetType().GetProperties())
                    {
                        builtQuery.Parameters["@" + prop.Name] = prop.GetValue(parameters);
                    }
                }
            }
            _whereBuilder.WhereExplicitlyRaw(sql);
            return this;
        }
    }
}
