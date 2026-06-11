namespace Rocket.Libraries.Qurious.Enums
{
    /// <summary>
    /// SQL set operations that combine results from two queries.
    /// </summary>
    public enum SetOperationType
    {
        /// <summary>UNION — distinct rows from both queries.</summary>
        Union,

        /// <summary>UNION ALL — all rows from both queries, including duplicates.</summary>
        UnionAll,

        /// <summary>INTERSECT — rows that appear in both queries.</summary>
        Intersect,

        /// <summary>EXCEPT — rows in the first query that do not appear in the second.</summary>
        Except,
    }
}
