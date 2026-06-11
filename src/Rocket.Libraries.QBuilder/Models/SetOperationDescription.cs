namespace Rocket.Libraries.Qurious.Models
{
    using Rocket.Libraries.Qurious.Enums;

    /// <summary>Describes a UNION / UNION ALL / INTERSECT / EXCEPT appended to the main query.</summary>
    internal class SetOperationDescription
    {
        /// <summary>The pre-built SQL of the right-hand query.</summary>
        public string Sql { get; set; }

        /// <summary>Which set operation to apply.</summary>
        public SetOperationType OperationType { get; set; }
    }
}
