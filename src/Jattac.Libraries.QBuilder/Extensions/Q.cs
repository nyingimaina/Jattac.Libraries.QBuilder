namespace Jattac.Libraries.QBuilder
{
    using System;
    using Jattac.Libraries.QBuilder.Config;

    /// <summary>
    /// Static entry point for composing SQL queries with zero boilerplate.
    /// Reads dialect and table-name resolver from <see cref="QBuilderConfig"/> — configure once at startup.
    /// </summary>
    /// <example>
    /// <code>
    /// // Parameterized (recommended) — uses default config
    /// var result = Q.New()
    ///     .UseTableBoundSelector&lt;User&gt;().Column(u => u.Id).Then()
    ///     .BuildWithParameters();
    ///
    /// // Named config
    /// var q = Q.New("mysql");
    ///
    /// // Inline resolver (backward-compatible)
    /// var q = Q.New(t => "dbo." + t.Name, parameterize: false);
    /// </code>
    /// </example>
    public static class Q
    {
        /// <summary>
        /// Creates a new <see cref="QBuilder"/> using the default configuration set by
        /// <see cref="QBuilderConfig.ConfigureDefault"/>. If no default has been configured,
        /// uses <c>Dialect.None</c> and the CLR type name as the table name.
        /// </summary>
        public static QBuilder New(bool parameterize = true)
        {
            return new QBuilder(QBuilderRegistry.GetDefault(), parameterize);
        }

        /// <summary>
        /// Creates a new <see cref="QBuilder"/> using a named configuration registered via
        /// <see cref="QBuilderConfig.Configure"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if <paramref name="name"/> has not been registered.</exception>
        public static QBuilder New(string name, bool parameterize = true)
        {
            return new QBuilder(QBuilderRegistry.Get(name), parameterize);
        }

        /// <summary>
        /// Creates a new <see cref="QBuilder"/> with an inline table-name resolver.
        /// Bypasses the config registry — useful for one-off custom resolvers or migration scenarios.
        /// </summary>
        public static QBuilder New(Func<Type, string> tableNameResolver, bool parameterize = true)
        {
            return new QBuilder(new QBuilderOptions { TableNameResolver = tableNameResolver }, parameterize);
        }

        /// <summary>
        /// Creates a new <see cref="QBuilder"/> directly from a <see cref="QBuilderOptions"/> instance.
        /// Use when you build options programmatically without going through the registry.
        /// </summary>
        public static QBuilder New(QBuilderOptions options, bool parameterize = true)
        {
            return new QBuilder(options, parameterize);
        }
    }
}
