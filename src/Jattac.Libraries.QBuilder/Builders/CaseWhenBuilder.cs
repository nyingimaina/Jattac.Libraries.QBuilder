namespace Jattac.Libraries.QBuilder.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Jattac.Libraries.QBuilder.Helpers;
    using Jattac.Libraries.QBuilder.Models;

    /// <summary>
    /// Builds a SQL <c>CASE WHEN … THEN … ELSE … END</c> expression for use in a SELECT clause.
    /// </summary>
    /// <example>
    /// <code>
    /// var caseExpr = CaseWhenBuilder.For&lt;User&gt;()
    ///     .When(u => u.Status, FilterOperator.EqualTo, "active").Then("Active")
    ///     .When(u => u.Status, FilterOperator.EqualTo, "banned").Then("Banned")
    ///     .Else("Unknown");
    ///
    /// qb.UseSelector().SelectCaseWhen(caseExpr, alias: "StatusLabel");
    /// </code>
    /// </example>
    public class CaseWhenBuilder
    {
        private readonly string _tableAlias;
        private readonly List<CaseWhenClause> _clauses = new List<CaseWhenClause>();
        private readonly FieldNameResolver _fieldNameResolver = new FieldNameResolver();
        private string _pendingCondition;
        private string _elseValue;

        private CaseWhenBuilder(string tableAlias)
        {
            _tableAlias = tableAlias;
        }

        /// <summary>
        /// Creates a new <see cref="CaseWhenBuilder"/> scoped to <typeparamref name="TTable"/>.
        /// The table alias is derived from the type name.
        /// </summary>
        /// <typeparam name="TTable">The primary table for conditions in this CASE expression.</typeparam>
        public static CaseWhenBuilder For<TTable>()
        {
            return new CaseWhenBuilder($"t{typeof(TTable).Name}");
        }

        /// <summary>
        /// Adds a WHEN condition using a lambda field selector and operator/value.
        /// Must be followed immediately by <see cref="Then(string)"/>.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <typeparam name="TField">Type of the field.</typeparam>
        /// <param name="fieldSelector">Lambda selecting the column, e.g. <c>u => u.Status</c>.</param>
        /// <param name="op">Comparison operator.</param>
        /// <param name="value">Value to compare against.</param>
        public CaseWhenBuilder When<TTable, TField>(Expression<Func<TTable, TField>> fieldSelector, FilterOperator op, object value)
        {
            var field = _fieldNameResolver.GetFieldName(fieldSelector);
            var alias = $"t{typeof(TTable).Name}";
            var condition = new ConditionMaker().GetCondition(field, op, value);
            _pendingCondition = $"{alias}.{field} {condition}";
            return this;
        }

        /// <summary>
        /// Adds a WHEN condition using a raw field name string and operator/value.
        /// Must be followed immediately by <see cref="Then(string)"/>.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <param name="field">Column name.</param>
        /// <param name="op">Comparison operator.</param>
        /// <param name="value">Value to compare against.</param>
        public CaseWhenBuilder When<TTable>(string field, FilterOperator op, object value)
        {
            var alias = $"t{typeof(TTable).Name}";
            var condition = new ConditionMaker().GetCondition(field, op, value);
            _pendingCondition = $"{alias}.{field} {condition}";
            return this;
        }

        /// <summary>
        /// Sets the THEN value for the most recently added WHEN condition.
        /// </summary>
        /// <param name="result">The value to return when the WHEN condition is true.</param>
        public CaseWhenBuilder Then(string result)
        {
            if (_pendingCondition == null)
            {
                throw new InvalidOperationException("Call When() before Then().");
            }
            _clauses.Add(new CaseWhenClause
            {
                WhenCondition = _pendingCondition,
                ThenValue = result,
            });
            _pendingCondition = null;
            return this;
        }

        /// <summary>
        /// Sets the ELSE value returned when no WHEN condition matches.
        /// </summary>
        /// <param name="value">The fallback value.</param>
        public CaseWhenBuilder Else(string value)
        {
            _elseValue = value;
            return this;
        }

        /// <summary>
        /// Builds the complete <c>CASE WHEN … END</c> SQL expression string.
        /// </summary>
        public string Build()
        {
            if (_clauses.Count == 0)
            {
                throw new InvalidOperationException("A CaseWhenBuilder must have at least one When/Then pair before calling Build().");
            }

            var sql = "Case";
            foreach (var clause in _clauses)
            {
                sql += $" When {clause.WhenCondition} Then '{clause.ThenValue}'";
            }

            if (_elseValue != null)
            {
                sql += $" Else '{_elseValue}'";
            }

            sql += " End";
            return sql;
        }
    }
}
