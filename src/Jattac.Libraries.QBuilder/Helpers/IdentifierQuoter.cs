namespace Jattac.Libraries.QBuilder.Helpers
{
    using System.Linq;
    using Jattac.Libraries.QBuilder.Enums;

    /// <summary>
    /// Wraps SQL identifiers in the dialect-specific quote characters.
    /// All methods are no-ops when <see cref="Dialect.None"/> is in effect.
    /// </summary>
    internal static class IdentifierQuoter
    {
        /// <summary>
        /// Quotes each part of a potentially schema-qualified table name.
        /// <c>"dbo.Users"</c> → <c>"[dbo].[Users]"</c> (SqlServer).
        /// </summary>
        internal static string QuoteTable(string tableName, Dialect dialect)
        {
            if (dialect == Dialect.None)
            {
                return tableName;
            }
            var parts = tableName.Split('.');
            return string.Join(".", parts.Select(p => QuoteIdentifier(p, dialect)));
        }

        /// <summary>
        /// Quotes a single identifier (alias, column, table name without schema).
        /// Returns the value unchanged for <see cref="Dialect.None"/>.
        /// </summary>
        internal static string QuoteIdentifier(string name, Dialect dialect)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            return dialect switch
            {
                Dialect.None => name,
                Dialect.SqlServer => $"[{name}]",       // MsSql is an alias — same value
                Dialect.MySql or Dialect.MariaDb => $"`{name}`",
                Dialect.Sqlite or Dialect.Postgres or Dialect.Generic => $"\"{name}\"",
                _ => name,
            };
        }
    }
}
