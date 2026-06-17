namespace Jattac.Libraries.QBuilder.Builders.DML
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Jattac.Libraries.QBuilder.Helpers;

    /// <summary>
    /// Abstract CRTP base that provides the full WHERE predicate surface for DML builders.
    /// All methods return <typeparamref name="TBuilder"/> so callers stay on the correct derived type.
    /// Column qualifiers use the actual table name (e.g. <c>User.Id</c>) rather than the
    /// SELECT alias (<c>tUser.Id</c>), which would be invalid in DML statements.
    /// </summary>
    public abstract class DmlWhereBuilder<TTable, TBuilder> : BuilderBase
        where TBuilder : DmlWhereBuilder<TTable, TBuilder>
    {
        private readonly WhereBuilder _wb;
        protected readonly FieldNameResolver Fnr = new FieldNameResolver();
        protected bool HasWhere;

        private readonly string _tableName;
        private TBuilder Me => (TBuilder)this;

        internal DmlWhereBuilder(QBuilder qBuilder, WhereBuilder whereBuilder)
            : base(qBuilder)
        {
            _wb = whereBuilder;
            var full = qBuilder.TableNameResolver(typeof(TTable));
            var dot = full.LastIndexOf('.');
            _tableName = dot >= 0 ? full.Substring(dot + 1) : full;
        }

        /// <summary>Builds and returns the WHERE clause string (empty string when no conditions were added).</summary>
        protected string BuildWhereClause() => _wb.Build();

        // ─── core helpers ─────────────────────────────────────────────────────────────

        private TBuilder And<TField>(Expression<Func<TTable, TField>> d, FilterOperator op, object value)
        {
            _wb.SetNextConjunction("And");
            _wb.Where<TTable>(Fnr.GetFieldName(d), op, value, _tableName);
            HasWhere = true;
            return Me;
        }

        private TBuilder Or<TField>(Expression<Func<TTable, TField>> d, FilterOperator op, object value)
        {
            _wb.SetNextConjunction("Or");
            _wb.Where<TTable>(Fnr.GetFieldName(d), op, value, _tableName);
            HasWhere = true;
            return Me;
        }

        private TBuilder NullCheckAnd<TField>(Expression<Func<TTable, TField>> d, bool isNull)
        {
            _wb.SetNextConjunction("And");
            if (isNull) _wb.WhereIsNull<TTable>(Fnr.GetFieldName(d), _tableName);
            else _wb.WhereIsNotNull<TTable>(Fnr.GetFieldName(d), _tableName);
            HasWhere = true;
            return Me;
        }

        private TBuilder NullCheckOr<TField>(Expression<Func<TTable, TField>> d, bool isNull)
        {
            _wb.SetNextConjunction("Or");
            if (isNull) _wb.WhereIsNull<TTable>(Fnr.GetFieldName(d), _tableName);
            else _wb.WhereIsNotNull<TTable>(Fnr.GetFieldName(d), _tableName);
            HasWhere = true;
            return Me;
        }

        private TBuilder RangeAnd<TField>(Expression<Func<TTable, TField>> d, object from, object to, bool negate)
        {
            _wb.SetNextConjunction("And");
            if (negate) _wb.WhereNotBetween<TTable>(Fnr.GetFieldName(d), from, to, _tableName);
            else _wb.WhereBetween<TTable>(Fnr.GetFieldName(d), from, to, _tableName);
            HasWhere = true;
            return Me;
        }

        private TBuilder RangeOr<TField>(Expression<Func<TTable, TField>> d, object from, object to, bool negate)
        {
            _wb.SetNextConjunction("Or");
            if (negate) _wb.WhereNotBetween<TTable>(Fnr.GetFieldName(d), from, to, _tableName);
            else _wb.WhereBetween<TTable>(Fnr.GetFieldName(d), from, to, _tableName);
            HasWhere = true;
            return Me;
        }

        private TBuilder InAnd<TField, TVal>(Expression<Func<TTable, TField>> d, IEnumerable<TVal> values, bool negate)
        {
            _wb.SetNextConjunction("And");
            if (negate) _wb.WhereNotIn<TTable, TVal>(Fnr.GetFieldName(d), values, _tableName);
            else _wb.WhereIn<TTable, TVal>(Fnr.GetFieldName(d), values, _tableName);
            HasWhere = true;
            return Me;
        }

        private TBuilder InOr<TField, TVal>(Expression<Func<TTable, TField>> d, IEnumerable<TVal> values, bool negate)
        {
            _wb.SetNextConjunction("Or");
            if (negate) _wb.WhereNotIn<TTable, TVal>(Fnr.GetFieldName(d), values, _tableName);
            else _wb.WhereIn<TTable, TVal>(Fnr.GetFieldName(d), values, _tableName);
            HasWhere = true;
            return Me;
        }

        private TBuilder ExistsAnd(QBuilder sub, bool negate)
        {
            _wb.SetNextConjunction("And");
            if (negate) _wb.WhereNotExists(sub); else _wb.WhereExists(sub);
            HasWhere = true;
            return Me;
        }

        private TBuilder ExistsOr(QBuilder sub, bool negate)
        {
            _wb.SetNextConjunction("Or");
            if (negate) _wb.WhereNotExists(sub); else _wb.WhereExists(sub);
            HasWhere = true;
            return Me;
        }

        /// <summary>
        /// Adds an AND WHERE [column] = [value] condition using a raw column name string
        /// rather than an expression tree. For internal use by FromObject only.
        /// Protected so derived builders can call it but external code cannot.
        /// </summary>
        protected TBuilder AndEqualToByName(string columnName, object value)
        {
            _wb.SetNextConjunction("And");
            _wb.Where<TTable>(columnName, FilterOperator.EqualTo, value, _tableName);
            HasWhere = true;
            return Me;
        }

        // ─── parentheses ──────────────────────────────────────────────────────────────

        public TBuilder OpenGroup() { _wb.OpenParentheses(); return Me; }
        public TBuilder CloseGroup() { _wb.CloseParentheses(); return Me; }

        // ─── equality ─────────────────────────────────────────────────────────────────

        public TBuilder WhereEqualTo<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.EqualTo, v);
        public TBuilder AndWhereEqualTo<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.EqualTo, v);
        public TBuilder OrWhereEqualTo<TField>(Expression<Func<TTable, TField>> d, object v) => Or(d, FilterOperator.EqualTo, v);

        public TBuilder WhereNotEqualTo<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.NotEqualTo, v);
        public TBuilder AndWhereNotEqualTo<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.NotEqualTo, v);
        public TBuilder OrWhereNotEqualTo<TField>(Expression<Func<TTable, TField>> d, object v) => Or(d, FilterOperator.NotEqualTo, v);

        // ─── comparison ───────────────────────────────────────────────────────────────

        public TBuilder WhereLessThan<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.LessThan, v);
        public TBuilder AndWhereLessThan<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.LessThan, v);
        public TBuilder OrWhereLessThan<TField>(Expression<Func<TTable, TField>> d, object v) => Or(d, FilterOperator.LessThan, v);

        public TBuilder WhereLessThanOrEqualTo<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.LessThanOrEqualTo, v);
        public TBuilder AndWhereLessThanOrEqualTo<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.LessThanOrEqualTo, v);
        public TBuilder OrWhereLessThanOrEqualTo<TField>(Expression<Func<TTable, TField>> d, object v) => Or(d, FilterOperator.LessThanOrEqualTo, v);

        public TBuilder WhereGreaterThan<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.GreaterThan, v);
        public TBuilder AndWhereGreaterThan<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.GreaterThan, v);
        public TBuilder OrWhereGreaterThan<TField>(Expression<Func<TTable, TField>> d, object v) => Or(d, FilterOperator.GreaterThan, v);

        public TBuilder WhereGreaterThanOrEqualTo<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.GreaterThanOrEqualTo, v);
        public TBuilder AndWhereGreaterThanOrEqualTo<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.GreaterThanOrEqualTo, v);
        public TBuilder OrWhereGreaterThanOrEqualTo<TField>(Expression<Func<TTable, TField>> d, object v) => Or(d, FilterOperator.GreaterThanOrEqualTo, v);

        // ─── LIKE ─────────────────────────────────────────────────────────────────────

        public TBuilder WhereContains<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.Contains, v);
        public TBuilder AndWhereContains<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.Contains, v);
        public TBuilder OrWhereContains<TField>(Expression<Func<TTable, TField>> d, object v) => Or(d, FilterOperator.Contains, v);

        public TBuilder WhereStartsWith<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.StartsWith, v);
        public TBuilder AndWhereStartsWith<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.StartsWith, v);
        public TBuilder OrWhereStartsWith<TField>(Expression<Func<TTable, TField>> d, object v) => Or(d, FilterOperator.StartsWith, v);

        public TBuilder WhereEndsWith<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.EndsWith, v);
        public TBuilder AndWhereEndsWith<TField>(Expression<Func<TTable, TField>> d, object v) => And(d, FilterOperator.EndsWith, v);
        public TBuilder OrWhereEndsWith<TField>(Expression<Func<TTable, TField>> d, object v) => Or(d, FilterOperator.EndsWith, v);

        // ─── IS NULL / IS NOT NULL ────────────────────────────────────────────────────

        public TBuilder WhereIsNull<TField>(Expression<Func<TTable, TField>> d) => NullCheckAnd(d, true);
        public TBuilder AndWhereIsNull<TField>(Expression<Func<TTable, TField>> d) => NullCheckAnd(d, true);
        public TBuilder OrWhereIsNull<TField>(Expression<Func<TTable, TField>> d) => NullCheckOr(d, true);

        public TBuilder WhereIsNotNull<TField>(Expression<Func<TTable, TField>> d) => NullCheckAnd(d, false);
        public TBuilder AndWhereIsNotNull<TField>(Expression<Func<TTable, TField>> d) => NullCheckAnd(d, false);
        public TBuilder OrWhereIsNotNull<TField>(Expression<Func<TTable, TField>> d) => NullCheckOr(d, false);

        // ─── IN / NOT IN ──────────────────────────────────────────────────────────────

        public TBuilder WhereIn<TField, TVal>(Expression<Func<TTable, TField>> d, IEnumerable<TVal> values) => InAnd(d, values, false);
        public TBuilder AndWhereIn<TField, TVal>(Expression<Func<TTable, TField>> d, IEnumerable<TVal> values) => InAnd(d, values, false);
        public TBuilder OrWhereIn<TField, TVal>(Expression<Func<TTable, TField>> d, IEnumerable<TVal> values) => InOr(d, values, false);

        public TBuilder WhereNotIn<TField, TVal>(Expression<Func<TTable, TField>> d, IEnumerable<TVal> values) => InAnd(d, values, true);
        public TBuilder AndWhereNotIn<TField, TVal>(Expression<Func<TTable, TField>> d, IEnumerable<TVal> values) => InAnd(d, values, true);
        public TBuilder OrWhereNotIn<TField, TVal>(Expression<Func<TTable, TField>> d, IEnumerable<TVal> values) => InOr(d, values, true);

        // ─── BETWEEN / NOT BETWEEN ────────────────────────────────────────────────────

        public TBuilder WhereBetween<TField>(Expression<Func<TTable, TField>> d, object from, object to) => RangeAnd(d, from, to, false);
        public TBuilder AndWhereBetween<TField>(Expression<Func<TTable, TField>> d, object from, object to) => RangeAnd(d, from, to, false);
        public TBuilder OrWhereBetween<TField>(Expression<Func<TTable, TField>> d, object from, object to) => RangeOr(d, from, to, false);

        public TBuilder WhereNotBetween<TField>(Expression<Func<TTable, TField>> d, object from, object to) => RangeAnd(d, from, to, true);
        public TBuilder AndWhereNotBetween<TField>(Expression<Func<TTable, TField>> d, object from, object to) => RangeAnd(d, from, to, true);
        public TBuilder OrWhereNotBetween<TField>(Expression<Func<TTable, TField>> d, object from, object to) => RangeOr(d, from, to, true);

        // ─── EXISTS / NOT EXISTS ──────────────────────────────────────────────────────

        public TBuilder WhereExists(QBuilder sub) => ExistsAnd(sub, false);
        public TBuilder AndWhereExists(QBuilder sub) => ExistsAnd(sub, false);
        public TBuilder OrWhereExists(QBuilder sub) => ExistsOr(sub, false);

        public TBuilder WhereNotExists(QBuilder sub) => ExistsAnd(sub, true);
        public TBuilder AndWhereNotExists(QBuilder sub) => ExistsAnd(sub, true);
        public TBuilder OrWhereNotExists(QBuilder sub) => ExistsOr(sub, true);
    }
}
