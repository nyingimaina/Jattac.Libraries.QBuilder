namespace Rocket.Libraries.Qurious.Builders
{
    /// <summary>
    /// Builds the HAVING clause of a SQL query.
    /// Exposes the same filter methods as <see cref="WhereBuilder"/> but emits <c>HAVING</c> instead of <c>WHERE</c>.
    /// Use after <see cref="GroupBuilder"/> to filter aggregated rows.
    /// </summary>
    /// <example>
    /// <code>
    /// qb.UseGrouper().GroupBy&lt;Order&gt;("UserId");
    /// qb.UseHaving().Where&lt;Order&gt;("Amount", FilterOperator.GreaterThan, 100);
    /// </code>
    /// </example>
    public class HavingBuilder : WhereBuilder
    {
        /// <summary>Initializes a new <see cref="HavingBuilder"/> attached to <paramref name="qBuilder"/>.</summary>
        public HavingBuilder(QBuilder qBuilder, BuiltQuery builtQuery = null)
            : base(qBuilder, builtQuery, keyword: "Having")
        {
        }
    }
}
