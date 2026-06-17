namespace Jattac.Libraries.QBuilder.Enums
{
    /// <summary>
    /// Identifies the target SQL database dialect.
    /// Controls identifier quoting style and automatic paging strategy.
    /// </summary>
    public enum Dialect
    {
        /// <summary>No quoting; paging uses OFFSET/FETCH. Backward-compatible default.</summary>
        None,

        /// <summary>Identifiers quoted with <c>[brackets]</c>; paging uses ROW_NUMBER().</summary>
        SqlServer,

        /// <summary>Identifiers quoted with <c>[brackets]</c>; paging uses ROW_NUMBER(). Alias for <see cref="SqlServer"/>.</summary>
        MsSql = SqlServer,

        /// <summary>Identifiers quoted with <c>`backticks`</c>; paging uses LIMIT.</summary>
        MySql,

        /// <summary>Identifiers quoted with <c>`backticks`</c>; paging uses LIMIT. Same quoting as <see cref="MySql"/>.</summary>
        MariaDb,

        /// <summary>Identifiers quoted with <c>"double quotes"</c>; paging uses OFFSET/FETCH.</summary>
        Sqlite,

        /// <summary>Identifiers quoted with <c>"double quotes"</c>; paging uses OFFSET/FETCH.</summary>
        Postgres,

        /// <summary>
        /// ANSI SQL quoting (<c>"double quotes"</c>) and OFFSET/FETCH paging.
        /// Use for databases not listed above that support the SQL standard.
        /// </summary>
        Generic,
    }
}
