namespace Rocket.Libraries.Qurious.Enums
{
    /// <summary>
    /// Database-specific paging syntax to use when calling <c>.Page()</c>.
    /// </summary>
    public enum PageFlavor
    {
        /// <summary>
        /// SQL Server ROW_NUMBER() OVER (...) paging wrapped in an outer SELECT.
        /// Compatible with SQL Server 2005+.
        /// </summary>
        SqlServer,

        /// <summary>
        /// SQL Server 2012+ / standard ANSI paging: ORDER BY … OFFSET n ROWS FETCH NEXT n ROWS ONLY.
        /// Simpler and more portable than ROW_NUMBER; requires an ORDER BY clause.
        /// </summary>
        SqlServerOffsetFetch,

        /// <summary>
        /// MySQL / MariaDB paging: ORDER BY … LIMIT n OFFSET n.
        /// </summary>
        MySql,
    }
}
