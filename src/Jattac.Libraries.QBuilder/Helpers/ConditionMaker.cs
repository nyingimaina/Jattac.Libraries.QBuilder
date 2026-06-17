using System;
namespace Jattac.Libraries.QBuilder.Helpers
{
    using System.Globalization;

    internal class ConditionMaker
    {
        private static readonly FilterOperator[] NoValueOperators =
        {
            FilterOperator.IsNull,
            FilterOperator.IsNotNull,
        };

        private static bool IsNoValueOperator(FilterOperator op)
        {
            foreach (var o in NoValueOperators)
            {
                if (o == op) return true;
            }
            return false;
        }

        /// <summary>
        /// Builds a WHERE/HAVING condition string for single-value operators.
        /// For BETWEEN use <see cref="GetBetweenCondition"/>.
        /// For IS NULL / IS NOT NULL pass <c>value = null</c>.
        /// </summary>
        public string GetCondition(string field, FilterOperator op, object value, BuiltQuery builtQuery = null)
        {
            if (!IsNoValueOperator(op))
            {
                Guard.NotNull(value, "No value was provided for filter");
            }

            var sqlOperator = GetSqlOperator(op);

            if (IsNoValueOperator(op))
            {
                return $" {sqlOperator}";
            }

            var conditionTemplate = GetConditionTemplate(op);
            var condition = string.Empty;

            if (builtQuery == null)
            {
                condition = $" {sqlOperator} {string.Format(CultureInfo.InvariantCulture, conditionTemplate, value)}";
            }
            else
            {
                conditionTemplate = conditionTemplate.Replace("'", string.Empty).Replace("%", string.Empty);
                var parameterName = GetParameterName(field, builtQuery);
                condition = $" {sqlOperator} {string.Format(CultureInfo.InvariantCulture, conditionTemplate, parameterName)}";

                Func<object> getEffectiveValue = () =>
                {
                    value = value ?? string.Empty;
                    switch (op)
                    {
                        case FilterOperator.StartsWith:
                            return $"{value}%";
                        case FilterOperator.Contains:
                            return $"%{value}%";
                        case FilterOperator.EndsWith:
                            return $"%{value}";
                        default:
                            return value; // raw value — lets ADO.NET/Dapper handle type conversion
                    }
                };

                builtQuery.Parameters.Add(parameterName, getEffectiveValue());
            }

            return condition;
        }

        /// <summary>
        /// Builds a BETWEEN / NOT BETWEEN condition, handling parameterization for two values.
        /// </summary>
        public string GetBetweenCondition(string field, bool negate, object from, object to, BuiltQuery builtQuery = null)
        {
            Guard.NotNull(from, "No 'from' value provided for BETWEEN filter");
            Guard.NotNull(to, "No 'to' value provided for BETWEEN filter");

            var keyword = negate ? "Not Between" : "Between";

            if (builtQuery == null)
            {
                return $" {keyword} '{from}' And '{to}'";
            }

            var fromParam = GetParameterName(field, builtQuery);
            builtQuery.Parameters.Add(fromParam, from.ToString());
            var toParam = GetParameterName(field, builtQuery);
            builtQuery.Parameters.Add(toParam, to.ToString());
            return $" {keyword} {fromParam} And {toParam}";
        }

        public static string GetParameterName(string field, BuiltQuery builtQuery)
        {
            var parameterNameIndex = 0;
            string parameterName;
            do
            {
                parameterName = $"@{field}{parameterNameIndex}";
                parameterNameIndex++;
            }
            while (builtQuery.Parameters.ContainsKey(parameterName));
            return parameterName;
        }

        private string GetConditionTemplate(FilterOperator op)
        {
            switch (op)
            {
                default:
                    Guard.Against(true, $"Unknown operator '{op}'. Cannot build filter");
                    return string.Empty;

                case FilterOperator.LessThan:
                case FilterOperator.LessThanOrEqualTo:
                case FilterOperator.EqualTo:
                case FilterOperator.GreaterThanOrEqualTo:
                case FilterOperator.GreaterThan:
                case FilterOperator.NotEqualTo:
                    return "'{0}'";

                case FilterOperator.StartsWith:
                    return "'{0}%'";

                case FilterOperator.Contains:
                    return "'%{0}%'";

                case FilterOperator.EndsWith:
                    return "'%{0}'";
            }
        }

        private string GetSqlOperator(FilterOperator op)
        {
            switch (op)
            {
                default:
                    Guard.Against(true, $"Unknown operator '{op}'. Cannot build filter");
                    return string.Empty;

                case FilterOperator.LessThan:            return "<";
                case FilterOperator.LessThanOrEqualTo:   return "<=";
                case FilterOperator.EqualTo:             return "=";
                case FilterOperator.GreaterThanOrEqualTo: return ">=";
                case FilterOperator.GreaterThan:         return ">";
                case FilterOperator.NotEqualTo:          return "<>";
                case FilterOperator.StartsWith:
                case FilterOperator.Contains:
                case FilterOperator.EndsWith:            return "Like";
                case FilterOperator.IsNull:              return "IS NULL";
                case FilterOperator.IsNotNull:           return "IS NOT NULL";
            }
        }
    }
}
