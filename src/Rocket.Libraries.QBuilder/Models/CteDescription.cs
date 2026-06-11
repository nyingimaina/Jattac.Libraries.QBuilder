namespace Rocket.Libraries.Qurious.Models
{
    /// <summary>Describes one named CTE in a WITH clause.</summary>
    internal class CteDescription
    {
        /// <summary>The CTE name referenced later in the query.</summary>
        public string Name { get; set; }

        /// <summary>The pre-built SQL for this CTE's body.</summary>
        public string Sql { get; set; }
    }
}
