namespace Jattac.Libraries.QBuilder.Builders
{
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using Jattac.Libraries.QBuilder.Helpers;
    using Jattac.Libraries.QBuilder.Models;

    /// <summary>
    /// Builds the WHERE (or HAVING) clause of a SQL query.
    /// Supports typed conditions, IN / NOT IN, IS NULL, BETWEEN, EXISTS, and nested parentheses.
    /// </summary>
    public class WhereBuilder : BuilderBase
    {
        private List<WhereDescription> _wheres = new List<WhereDescription>();

        private FieldNameResolver _fieldNameResolver;

        private string _nextConjuntion = "And";

        private WhereConjunctionBuilder _whereConjunctionBuilder;

        private List<ParenthesesDescription> _parentheses = new List<ParenthesesDescription>();

        private readonly ParenthesesDescription _implicitParentheses = new ParenthesesDescription
        {
            Id = default(Guid)
        };

        private ParenthesesDescription CurrentParentheses
        {
            get
            {
                var explicitParentheses = _parentheses.LastOrDefault(a => a.Closed == false);
                return explicitParentheses ?? _implicitParentheses;
            }
        }

        private readonly BuiltQuery builtQuery;
        private readonly string _keyword;

        /// <summary>
        /// Initializes a new <see cref="WhereBuilder"/> attached to <paramref name="qBuilder"/>.
        /// </summary>
        /// <param name="qBuilder">The owning query builder.</param>
        /// <param name="builtQuery">Pass a <see cref="BuiltQuery"/> to enable parameterization; <c>null</c> for plain SQL.</param>
        /// <param name="keyword">SQL keyword that opens the clause — <c>"Where"</c> for WHERE, <c>"Having"</c> for HAVING.</param>
        public WhereBuilder(QBuilder qBuilder, BuiltQuery builtQuery = null, string keyword = "Where")
            : base(qBuilder)
        {
            this.builtQuery = builtQuery;
            _keyword = keyword;
            _whereConjunctionBuilder = new WhereConjunctionBuilder(this, qBuilder);
            _fieldNameResolver = new FieldNameResolver();
        }

        /// <summary>Returns the conjunction builder for chaining AND / OR predicates.</summary>
        public WhereConjunctionBuilder UseConjunction()
        {
            return _whereConjunctionBuilder;
        }

        /// <summary>
        /// Opens a parenthesis group. Nested groups are supported — each call must be paired with <see cref="CloseParentheses"/>.
        /// </summary>
        public WhereBuilder OpenParentheses()
        {
            _parentheses.Add(new ParenthesesDescription
            {
                Closed = false,
                Id = Guid.NewGuid()
            });
            return this;
        }

        /// <summary>Closes the most recently opened parenthesis group.</summary>
        public WhereBuilder CloseParentheses()
        {
            var noOpenParentheses = CurrentParentheses == null || CurrentParentheses.Id == _implicitParentheses.Id;
            DataValidator.EvaluateImmediate(noOpenParentheses, "There is currently no open parentheses. Nothing to close.");
            CurrentParentheses.Closed = true;
            return this;
        }

        /// <summary>
        /// Adds a filter if <paramref name="filterDescription"/> has a value set; otherwise does nothing.
        /// </summary>
        public WhereConjunctionBuilder Where<TTable>(FilterDescription<TTable> filterDescription)
        {
            if (filterDescription.FilterSet)
            {
                return Where<TTable>(filterDescription.FieldName, filterDescription.Filter);
            }
            return _whereConjunctionBuilder;
        }

        /// <summary>
        /// Adds a typed condition using a <see cref="FilterOperator"/> and value.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <param name="field">Column name.</param>
        /// <param name="op">Comparison operator.</param>
        /// <param name="value">Value to compare against. Not required for <see cref="FilterOperator.IsNull"/> / <see cref="FilterOperator.IsNotNull"/>.</param>
        public WhereConjunctionBuilder Where<TTable>(string field, FilterOperator op, object value)
        {
            var condition = new ConditionMaker().GetCondition(field, op, value, builtQuery);
            return Where<TTable>(field, condition);
        }

        /// <summary>Adds a raw condition string for the given field and table.</summary>
        public WhereConjunctionBuilder Where<TTable>(string field, string condition)
        {
            return Where(typeof(TTable), field, condition);
        }

        /// <summary>Adds a raw condition string for the given table type, field, and condition.</summary>
        public WhereConjunctionBuilder Where(Type tableType, string field, string condition)
        {
            _wheres.Add(new WhereDescription
            {
                Clause = $"{QBuilder.TableNameAliaser.GetTableAlias(QBuilder.TableNameResolver(tableType))}.{field} {condition}",
                Conjunction = _nextConjuntion,
                ParenthesesId = CurrentParentheses.Id,
            });
            return _whereConjunctionBuilder;
        }

        /// <summary>
        /// Adds an IS NULL predicate for the specified column.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <param name="field">Column name.</param>
        public WhereConjunctionBuilder WhereIsNull<TTable>(string field)
        {
            var condition = new ConditionMaker().GetCondition(field, FilterOperator.IsNull, null, builtQuery);
            return Where<TTable>(field, condition);
        }

        /// <summary>
        /// Adds an IS NOT NULL predicate for the specified column.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <param name="field">Column name.</param>
        public WhereConjunctionBuilder WhereIsNotNull<TTable>(string field)
        {
            var condition = new ConditionMaker().GetCondition(field, FilterOperator.IsNotNull, null, builtQuery);
            return Where<TTable>(field, condition);
        }

        /// <summary>
        /// Adds a BETWEEN predicate: <c>field BETWEEN from AND to</c>.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <param name="field">Column name.</param>
        /// <param name="from">Inclusive lower bound.</param>
        /// <param name="to">Inclusive upper bound.</param>
        public WhereConjunctionBuilder WhereBetween<TTable>(string field, object from, object to)
        {
            var condition = new ConditionMaker().GetBetweenCondition(field, false, from, to, builtQuery);
            return Where<TTable>(field, condition);
        }

        /// <summary>
        /// Adds a NOT BETWEEN predicate: <c>field NOT BETWEEN from AND to</c>.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <param name="field">Column name.</param>
        /// <param name="from">Inclusive lower bound.</param>
        /// <param name="to">Inclusive upper bound.</param>
        public WhereConjunctionBuilder WhereNotBetween<TTable>(string field, object from, object to)
        {
            var condition = new ConditionMaker().GetBetweenCondition(field, true, from, to, builtQuery);
            return Where<TTable>(field, condition);
        }

        /// <summary>
        /// Adds an EXISTS (subquery) predicate.
        /// </summary>
        /// <param name="subQuery">A <see cref="QBuilder"/> whose <c>Build()</c> result is used as the subquery.</param>
        public WhereConjunctionBuilder WhereExists(QBuilder subQuery)
        {
            var sql = subQuery.Build();
            _wheres.Add(new WhereDescription
            {
                Clause = $"Exists ({sql})",
                Conjunction = _nextConjuntion,
                ParenthesesId = CurrentParentheses.Id,
            });
            return _whereConjunctionBuilder;
        }

        /// <summary>
        /// Adds a NOT EXISTS (subquery) predicate.
        /// </summary>
        /// <param name="subQuery">A <see cref="QBuilder"/> whose <c>Build()</c> result is used as the subquery.</param>
        public WhereConjunctionBuilder WhereNotExists(QBuilder subQuery)
        {
            var sql = subQuery.Build();
            _wheres.Add(new WhereDescription
            {
                Clause = $"Not Exists ({sql})",
                Conjunction = _nextConjuntion,
                ParenthesesId = CurrentParentheses.Id,
            });
            return _whereConjunctionBuilder;
        }

        /// <summary>
        /// Adds an IN predicate. Silently skips when <paramref name="values"/> is null or empty.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <typeparam name="TValueType">The element type of the values collection.</typeparam>
        /// <param name="field">Column name.</param>
        /// <param name="values">Values to match against.</param>
        public WhereConjunctionBuilder WhereIn<TTable, TValueType>(string field, IEnumerable<TValueType> values)
        {
            if (values == null)
            {
                return _whereConjunctionBuilder;
            }

            var valuesList = values.ToList();
            if (!valuesList.Any())
            {
                return _whereConjunctionBuilder;
            }

            if (builtQuery != null)
            {
                var whereInCriteria = string.Empty;

                foreach (var specificValue in valuesList)
                {
                    var parameterName = ConditionMaker.GetParameterName(field, builtQuery);
                    builtQuery.Parameters.Add(parameterName, specificValue);
                    whereInCriteria += $"{parameterName}, ";
                }

                whereInCriteria = whereInCriteria.TrimEnd(',', ' ');
                return Where<TTable>(field, $" in ({whereInCriteria})");
            }
            else
            {
                var criteria = WhereInFilterMaker.GetWhereInSectionArguments(valuesList);
                if (string.IsNullOrEmpty(criteria))
                {
                    return _whereConjunctionBuilder;
                }
                return Where<TTable>(field, $" in {criteria}");
            }
        }

        /// <summary>
        /// Adds a NOT IN predicate. Silently skips when <paramref name="values"/> is null or empty.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <typeparam name="TValueType">The element type of the values collection.</typeparam>
        /// <param name="field">Column name.</param>
        /// <param name="values">Values to exclude.</param>
        public WhereConjunctionBuilder WhereNotIn<TTable, TValueType>(string field, IEnumerable<TValueType> values)
        {
            if (values == null)
            {
                return _whereConjunctionBuilder;
            }

            var valuesList = values.ToList();
            if (!valuesList.Any())
            {
                return _whereConjunctionBuilder;
            }

            if (builtQuery != null)
            {
                var whereNotInCriteria = string.Empty;

                foreach (var specificValue in valuesList)
                {
                    var parameterName = ConditionMaker.GetParameterName(field, builtQuery);
                    builtQuery.Parameters.Add(parameterName, specificValue);
                    whereNotInCriteria += $"{parameterName}, ";
                }

                whereNotInCriteria = whereNotInCriteria.TrimEnd(',', ' ');
                return Where<TTable>(field, $" not in ({whereNotInCriteria})");
            }
            else
            {
                var criteria = WhereInFilterMaker.GetWhereInSectionArguments(valuesList);
                if (string.IsNullOrEmpty(criteria))
                {
                    return _whereConjunctionBuilder;
                }
                return Where<TTable>(field, $" not in {criteria}");
            }
        }

        /// <summary>
        /// Adds a filter only when <paramref name="fnResolveCondition"/> returns a non-empty string.
        /// Useful for building optional filters conditionally.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <param name="field">Column name.</param>
        /// <param name="fnResolveCondition">Returns a SQL condition fragment or <see cref="string.Empty"/> to skip.</param>
        public WhereConjunctionBuilder OptionalWhere<TTable>(string field, Func<string> fnResolveCondition)
        {
            var condition = fnResolveCondition();
            if (!string.IsNullOrEmpty(condition))
            {
                return Where<TTable>(field, condition);
            }
            return _whereConjunctionBuilder;
        }

        /// <summary>
        /// Injects a raw SQL criteria string directly. Not available in parameterized mode — use a typed <c>Where</c> overload instead.
        /// </summary>
        /// <param name="criteria">Raw SQL fragment, e.g. <c>"tUsers.Age &gt; 21"</c>.</param>
        /// <exception cref="InvalidOperationException">Thrown when parameterization is enabled.</exception>
        public WhereConjunctionBuilder WhereExplicitly(string criteria)
        {
            if (builtQuery != null)
            {
                throw new InvalidOperationException(
                    $"{nameof(WhereExplicitly)} cannot be used in parameterized mode because it embeds raw SQL without parameter binding. " +
                    "Use a typed Where overload instead.");
            }
            _wheres.Add(new WhereDescription
            {
                Clause = criteria,
                Conjunction = _nextConjuntion,
                ParenthesesId = CurrentParentheses.Id,
            });
            return _whereConjunctionBuilder;
        }

        internal void SetNextConjunction(string conjunction)
        {
            _nextConjuntion = conjunction;
        }

        internal string Build()
        {
            var where = string.Empty;
            var unClosedParenthesesExists = CurrentParentheses != null && CurrentParentheses.Id != _implicitParentheses.Id;
            DataValidator.EvaluateImmediate(unClosedParenthesesExists, "An unclosed parentheses was found. Please check your query.");
            var currentParenthesesId = _implicitParentheses.Id;

            foreach (var whereDescription in _wheres)
            {
                var parenthesesIdIsDifferent = currentParenthesesId != whereDescription.ParenthesesId;
                var exitingExplicitParentheses = parenthesesIdIsDifferent && currentParenthesesId != _implicitParentheses.Id;
                var enteringImplicitParentheses = whereDescription.ParenthesesId == _implicitParentheses.Id;

                if (exitingExplicitParentheses)
                {
                    where += ")";
                }

                if (!string.IsNullOrEmpty(where))
                {
                    where += $" {whereDescription.Conjunction} ";
                }

                if (parenthesesIdIsDifferent)
                {
                    if (!enteringImplicitParentheses)
                    {
                        where += " (";
                    }
                    currentParenthesesId = whereDescription.ParenthesesId;
                }

                where += $"{whereDescription.Clause}{Environment.NewLine}";
            }

            where = GetWithFinalParenthesesTerminatedIfRequired(currentParenthesesId, where);

            if (string.IsNullOrEmpty(where))
            {
                return string.Empty;
            }

            return $"{_keyword} {where}";
        }

        private string GetWithFinalParenthesesTerminatedIfRequired(Guid currentParenthesesId, string where)
        {
            if (currentParenthesesId != _implicitParentheses.Id)
            {
                where += ") ";
            }
            return where;
        }
    }
}
