namespace Jattac.Libraries.QBuilder.Enums
{
    /// <summary>
    /// SQL join types supported by the fluent API.
    /// </summary>
    public enum JoinType
    {
        /// <summary>INNER JOIN — rows where both sides match.</summary>
        Inner,

        /// <summary>LEFT JOIN — all left rows, matched right rows or NULL.</summary>
        Left,

        /// <summary>RIGHT JOIN — all right rows, matched left rows or NULL.</summary>
        Right,

        /// <summary>FULL OUTER JOIN — all rows from both sides, NULL where unmatched.</summary>
        FullOuter,

        /// <summary>CROSS JOIN — cartesian product, no ON clause.</summary>
        Cross,
    }
}
