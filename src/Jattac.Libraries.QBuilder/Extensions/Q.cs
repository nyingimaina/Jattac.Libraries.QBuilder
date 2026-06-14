namespace Jattac.Libraries.QBuilder
{
    using System;

    /// <summary>
    /// Static entry point for building SQL queries with zero boilerplate.
    /// </summary>
    /// <example>
    /// <code>
    /// var result = Q.Build(parameterize: true)
    ///     .Select&lt;User&gt;(u => u.Id)
    ///     .InnerJoin&lt;User, Order&gt;(u => u.Id, o => o.UserId)
    ///     .Where&lt;User&gt;(u => u.Active, FilterOperator.EqualTo, true)
    ///     .OrderBy&lt;User&gt;(u => u.Name)
    ///     .BuildWithParameters();
    /// </code>
    /// </example>
    public static class Q
    {
        /// <summary>
        /// Creates a new <see cref="QBuilder"/> with the default type-name resolver.
        /// </summary>
        /// <param name="parameterize">
        /// <c>true</c> (default) — all values are bound as parameters; call <see cref="QBuilder.BuildWithParameters"/> to retrieve the result.<br/>
        /// <c>false</c> — values are inlined as literals; call <see cref="QBuilder.Build"/> to retrieve the result.
        /// </param>
        /// <returns>A new <see cref="QBuilder"/> ready for chaining.</returns>
        public static QBuilder Build(bool parameterize = true)
        {
            return new QBuilder(parameterize);
        }

        /// <summary>
        /// Creates a new <see cref="QBuilder"/> with a custom table-name resolver.
        /// Use when your table names differ from the CLR type names (e.g. plural names, schema prefixes).
        /// </summary>
        /// <param name="tableNameResolver">
        /// A function that maps a CLR type to its SQL table name, e.g. <c>t => "dbo." + t.Name + "s"</c>.
        /// </param>
        /// <param name="parameterize">Whether to use parameterized queries (default <c>true</c>).</param>
        /// <returns>A new <see cref="QBuilder"/> ready for chaining.</returns>
        public static QBuilder Build(Func<Type, string> tableNameResolver, bool parameterize = true)
        {
            return new QBuilder(tableNameResolver, "t", parameterize);
        }
    }
}
