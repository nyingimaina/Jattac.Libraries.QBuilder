using System;
using System.Collections.Generic;
using System.Text;

namespace Jattac.Libraries.QBuilder.Helpers
{
    internal static class WhereInFilterMaker
    {
        public static string GetWhereInSectionArguments<TValueType>(IEnumerable<TValueType> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values), "Cannot build a where clause from a null list of values");
            }

            var valuesToList = new List<TValueType>(values);

            if (valuesToList.Count == 0)
            {
                throw new ArgumentException("No list of values provided. This condition is ambiguous and would produce unpredictable results.", nameof(values));
            }

            using (var uniqueValueResolver = new UniqueValueResolver<TValueType>())
            {
                values = uniqueValueResolver.GetUnique(valuesToList);
            }

            var args = string.Empty;
            foreach (var value in values)
            {
                var escaped = value?.ToString()?.Replace("'", "''") ?? string.Empty;
                args += $",'{escaped}'";
            }

            args = $"({args.Substring(1)})";
            return args;
        }
    }
}