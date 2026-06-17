namespace Jattac.Libraries.QBuilder.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Jattac.Libraries.QBuilder.Helpers;

    /// <summary>
    /// Fluent, fully type-inferred HAVING clause builder scoped to <typeparamref name="TTable"/>.
    /// Mirrors <see cref="TableBoundWhereBuilder{TTable}"/> but operates on the HAVING clause.
    /// Use after <c>UseTableBoundGrouper&lt;T&gt;()</c>.
    /// </summary>
    public class TableBoundHavingBuilder<TTable> : BuilderBase
    {
        private readonly HavingBuilder _havingBuilder;
        private readonly FieldNameResolver _fieldNameResolver;

        internal TableBoundHavingBuilder(HavingBuilder havingBuilder, QBuilder qBuilder)
            : base(qBuilder)
        {
            _havingBuilder = havingBuilder;
            _fieldNameResolver = new FieldNameResolver();
        }

        private TableBoundHavingBuilder<TTable> AddAnd<TField>(
            Expression<Func<TTable, TField>> descriptor, FilterOperator op, object value)
        {
            _havingBuilder.SetNextConjunction("And");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _havingBuilder.Where<TTable>(field, op, value);
            return this;
        }

        private TableBoundHavingBuilder<TTable> AddOr<TField>(
            Expression<Func<TTable, TField>> descriptor, FilterOperator op, object value)
        {
            _havingBuilder.SetNextConjunction("Or");
            var field = _fieldNameResolver.GetFieldName(descriptor);
            _havingBuilder.Where<TTable>(field, op, value);
            return this;
        }

        // ─── equality ─────────────────────────────────────────────────────────────

        public TableBoundHavingBuilder<TTable> HavingEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.EqualTo, value);

        public TableBoundHavingBuilder<TTable> HavingNotEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.NotEqualTo, value);

        public TableBoundHavingBuilder<TTable> AndHavingEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.EqualTo, value);

        public TableBoundHavingBuilder<TTable> OrHavingEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddOr(descriptor, FilterOperator.EqualTo, value);

        // ─── comparison ───────────────────────────────────────────────────────────

        public TableBoundHavingBuilder<TTable> HavingGreaterThan<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.GreaterThan, value);

        public TableBoundHavingBuilder<TTable> HavingGreaterThanOrEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.GreaterThanOrEqualTo, value);

        public TableBoundHavingBuilder<TTable> HavingLessThan<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.LessThan, value);

        public TableBoundHavingBuilder<TTable> HavingLessThanOrEqualTo<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.LessThanOrEqualTo, value);

        public TableBoundHavingBuilder<TTable> AndHavingGreaterThan<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.GreaterThan, value);

        public TableBoundHavingBuilder<TTable> AndHavingLessThan<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddAnd(descriptor, FilterOperator.LessThan, value);

        public TableBoundHavingBuilder<TTable> OrHavingGreaterThan<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddOr(descriptor, FilterOperator.GreaterThan, value);

        public TableBoundHavingBuilder<TTable> OrHavingLessThan<TField>(Expression<Func<TTable, TField>> descriptor, object value)
            => AddOr(descriptor, FilterOperator.LessThan, value);

        // ─── null ────────────────────────────────────────────────────────────────

        public TableBoundHavingBuilder<TTable> HavingIsNull<TField>(Expression<Func<TTable, TField>> descriptor)
        {
            _havingBuilder.SetNextConjunction("And");
            _havingBuilder.WhereIsNull<TTable>(_fieldNameResolver.GetFieldName(descriptor));
            return this;
        }

        public TableBoundHavingBuilder<TTable> HavingIsNotNull<TField>(Expression<Func<TTable, TField>> descriptor)
        {
            _havingBuilder.SetNextConjunction("And");
            _havingBuilder.WhereIsNotNull<TTable>(_fieldNameResolver.GetFieldName(descriptor));
            return this;
        }

        public TableBoundHavingBuilder<TTable> OrHavingIsNull<TField>(Expression<Func<TTable, TField>> descriptor)
        {
            _havingBuilder.SetNextConjunction("Or");
            _havingBuilder.WhereIsNull<TTable>(_fieldNameResolver.GetFieldName(descriptor));
            return this;
        }

        public TableBoundHavingBuilder<TTable> OrHavingIsNotNull<TField>(Expression<Func<TTable, TField>> descriptor)
        {
            _havingBuilder.SetNextConjunction("Or");
            _havingBuilder.WhereIsNotNull<TTable>(_fieldNameResolver.GetFieldName(descriptor));
            return this;
        }
    }
}
