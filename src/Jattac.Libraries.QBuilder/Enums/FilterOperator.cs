namespace Jattac.Libraries.QBuilder
{
    /// <summary>
    /// Comparison and pattern-matching operators for WHERE and HAVING clauses.
    /// </summary>
    public enum FilterOperator : int
    {
        /// <summary>field &lt; value</summary>
        LessThan = 1,

        /// <summary>field &lt;= value</summary>
        LessThanOrEqualTo = 2,

        /// <summary>field = value</summary>
        EqualTo = 3,

        /// <summary>field >= value</summary>
        GreaterThanOrEqualTo = 4,

        /// <summary>field > value</summary>
        GreaterThan = 5,

        /// <summary>field &lt;> value</summary>
        NotEqualTo = 6,

        /// <summary>field LIKE 'value%'</summary>
        StartsWith = 7,

        /// <summary>field LIKE '%value%'</summary>
        Contains = 8,

        /// <summary>field LIKE '%value'</summary>
        EndsWith = 9,

        /// <summary>field IS NULL — no value argument required; use WhereIsNull.</summary>
        IsNull = 10,

        /// <summary>field IS NOT NULL — no value argument required; use WhereIsNotNull.</summary>
        IsNotNull = 11,

        /// <summary>field BETWEEN fromValue AND toValue — use WhereBetween.</summary>
        Between = 12,

        /// <summary>field NOT BETWEEN fromValue AND toValue — use WhereNotBetween.</summary>
        NotBetween = 13,
    }
}