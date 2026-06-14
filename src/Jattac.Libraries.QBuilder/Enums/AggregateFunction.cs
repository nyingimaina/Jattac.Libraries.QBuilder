namespace Jattac.Libraries.QBuilder.Enums
{
    /// <summary>
    /// SQL aggregate functions for use in SELECT clauses.
    /// </summary>
    public enum AggregateFunction
    {
        /// <summary>COUNT(*) — total number of rows.</summary>
        Count,

        /// <summary>SUM(field) — sum of all non-null values.</summary>
        Sum,

        /// <summary>AVG(field) — arithmetic mean of non-null values.</summary>
        Avg,

        /// <summary>MIN(field) — smallest non-null value.</summary>
        Min,

        /// <summary>MAX(field) — largest non-null value.</summary>
        Max,

        /// <summary>COUNT(DISTINCT field) — number of distinct non-null values.</summary>
        CountDistinct,
    }
}
