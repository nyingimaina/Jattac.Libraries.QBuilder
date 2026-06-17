namespace Jattac.Libraries.QBuilder.Config
{
    using System;
    using Jattac.Libraries.QBuilder.Enums;

    /// <summary>
    /// Configuration options for a <see cref="QBuilder"/> instance.
    /// Set globally via <see cref="QBuilderConfig.ConfigureDefault"/> or per-name via <see cref="QBuilderConfig.Configure"/>.
    /// </summary>
    public class QBuilderOptions
    {
        /// <summary>Database dialect — controls identifier quoting and automatic paging strategy.</summary>
        public Dialect Dialect { get; set; } = Dialect.None;

        /// <summary>
        /// Maps a CLR type to its SQL table name.
        /// Default: <c>t => t.Name</c> (e.g. <c>User</c> → <c>User</c>).
        /// </summary>
        public Func<Type, string> TableNameResolver { get; set; } = t => t.Name;

        /// <summary>Prefix used when auto-aliasing tables (e.g. <c>"t"</c> → <c>tUser</c>).</summary>
        public string AliasPrefix { get; set; } = "t";
    }
}
