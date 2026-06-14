namespace Jattac.Libraries.QBuilder
{
    using System;

    /// <summary>
    /// Static entry point for composing SQL queries with zero boilerplate.
    /// </summary>
    /// <example>
    /// <code>
    /// // Parameterized (recommended)
    /// var result = Q.New()
    ///     .Select((User u) => u.Id)
    ///     .InnerJoin((User u) => u.Id, (Order o) => o.UserId)
    ///     .Where((User u) => u.Active, FilterOperator.EqualTo, true)
    ///     .OrderBy((User u) => u.Name)
    ///     .BuildWithParameters();
    ///
    /// // Literal (no parameters)
    /// var sql = Q.New(parameterize: false)
    ///     .Select((User u) => u.Name)
    ///     .Build();
    /// </code>
    /// </example>
    public static class Q
    {
        /// <summary>
        /// Creates a new <see cref="QBuilder"/> with the default type-name resolver.
        /// Table names are derived from the CLR type name (e.g. <c>User</c> → <c>User</c>).
        /// </summary>
        /// <param name="parameterize">
        /// <c>true</c> (default) — values are bound as named parameters; call <see cref="QBuilder.BuildWithParameters"/> to retrieve the SQL and parameter dictionary.<br/>
        /// <c>false</c> — values are inlined as literals; call <see cref="QBuilder.Build"/> to retrieve the SQL string.
        /// </param>
        /// <returns>A new <see cref="QBuilder"/> ready for chaining.</returns>
        public static QBuilder New(bool parameterize = true)
        {
            return new QBuilder(parameterize);
        }

        /// <summary>
        /// Creates a new <see cref="QBuilder"/> with a custom table-name resolver.
        /// Use when your table names differ from the CLR type names (e.g. plural names, schema prefixes).
        /// </summary>
        /// <param name="tableNameResolver">
        /// A function that maps a CLR type to its SQL table name,
        /// e.g. <c>t => "dbo." + t.Name + "s"</c> maps <c>User</c> → <c>dbo.Users</c>.
        /// </param>
        /// <param name="parameterize">Whether to use parameterized queries (default <c>true</c>).</param>
        /// <returns>A new <see cref="QBuilder"/> ready for chaining.</returns>
        public static QBuilder New(Func<Type, string> tableNameResolver, bool parameterize = true)
        {
            return new QBuilder(tableNameResolver, "t", parameterize);
        }
    }
}
