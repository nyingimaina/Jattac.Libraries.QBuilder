namespace Jattac.Libraries.QBuilder
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using Jattac.Libraries.QBuilder.Helpers;

    /// <summary>
    /// Combines multiple parameterized DML statements into a single round-trip.
    /// Statements are joined with ";\n". Parameters from all statements are merged
    /// into a single dictionary; any name collisions are automatically renamed.
    /// QBatch does NOT provide a transaction — wrap execution in an ADO.NET transaction if needed.
    /// </summary>
    /// <example>
    /// <code>
    /// var batch = QBatch.New()
    ///     .Add(Q.New().UseTableBoundUpdate&lt;User&gt;().FromObject(user1).BuildWithParameters())
    ///     .Add(Q.New().UseTableBoundUpdate&lt;User&gt;().FromObject(user2).BuildWithParameters())
    ///     .Build();
    ///
    /// connection.Execute(batch.ParameterizedSql, batch.Parameters);
    /// </code>
    /// </example>
    public sealed class QBatch
    {
        private readonly List<BuiltQuery> _queries = new List<BuiltQuery>();

        private QBatch() { }

        /// <summary>Creates a new empty batch.</summary>
        public static QBatch New() => new QBatch();

        /// <summary>
        /// Adds a parameterized query to the batch.
        /// The query must have been built with <c>BuildWithParameters()</c>.
        /// </summary>
        public QBatch Add(BuiltQuery query)
        {
            Guard.NotNull(query, nameof(query));
            Guard.Against(
                string.IsNullOrEmpty(query.ParameterizedSql),
                "Cannot add a query with an empty SQL string to a batch. " +
                "Ensure BuildWithParameters() has been called before Add().");
            _queries.Add(query);
            return this;
        }

        /// <summary>Adds multiple queries to the batch.</summary>
        public QBatch AddRange(IEnumerable<BuiltQuery> queries)
        {
            Guard.NotNull(queries, nameof(queries));
            foreach (var q in queries)
                Add(q);
            return this;
        }

        /// <summary>
        /// Combines all added queries into a single <see cref="BuiltQuery"/>.
        /// SQL statements are joined with ";\n".
        /// Parameters from all queries are merged; any name that collides with an
        /// already-registered parameter is automatically renamed in both the SQL and
        /// the parameter dictionary.
        /// </summary>
        public BuiltQuery Build()
        {
            Guard.Against(_queries.Count == 0,
                "No queries have been added to this batch. Call Add() at least once before Build().");

            var mergedParameters = new Dictionary<string, object>(StringComparer.Ordinal);
            var sqlParts = new List<string>(_queries.Count);

            foreach (var query in _queries)
            {
                var sql = query.ParameterizedSql;

                foreach (var kvp in query.Parameters)
                {
                    if (!mergedParameters.ContainsKey(kvp.Key))
                    {
                        mergedParameters[kvp.Key] = kvp.Value;
                    }
                    else
                    {
                        var newName = FindUniqueName(kvp.Key, mergedParameters);
                        sql = ReplaceParamName(sql, kvp.Key, newName);
                        mergedParameters[newName] = kvp.Value;
                    }
                }

                sqlParts.Add(sql);
            }

            return new BuiltQuery
            {
                ParameterizedSql = string.Join(";\n", sqlParts),
                Parameters = mergedParameters
            };
        }

        // ── Private helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Increments the trailing integer suffix of a parameter name until the
        /// resulting name is not already in <paramref name="existing"/>.
        /// Example: "@Id0" with "@Id0" taken → tries "@Id1", "@Id2", ...
        /// </summary>
        private static string FindUniqueName(string original, Dictionary<string, object> existing)
        {
            var match = Regex.Match(original, @"^(.+?)(\d+)$");
            if (!match.Success)
            {
                var fallback = original + "_b";
                while (existing.ContainsKey(fallback)) fallback += "_b";
                return fallback;
            }

            var prefix = match.Groups[1].Value;
            var index = int.Parse(match.Groups[2].Value) + 1;

            string candidate;
            do { candidate = $"{prefix}{index++}"; }
            while (existing.ContainsKey(candidate));

            return candidate;
        }

        /// <summary>
        /// Replaces all occurrences of <paramref name="oldName"/> in <paramref name="sql"/>
        /// that are not followed by a word character — preventing @Id0 from matching inside @Id01.
        /// </summary>
        private static string ReplaceParamName(string sql, string oldName, string newName)
        {
            var pattern = Regex.Escape(oldName) + @"(?![a-zA-Z0-9_])";
            return Regex.Replace(sql, pattern, newName);
        }
    }
}
