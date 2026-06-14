namespace Jattac.Libraries.QBuilder.Models
{
    /// <summary>Describes one column in an ORDER BY clause.</summary>
    internal class OrderDescription
    {
        public string TableAlias { get; set; }
        public string Field { get; set; }
        public string Mode { get; set; }
        public bool QualifyWithTableName { get; set; } = true;
    }
}
