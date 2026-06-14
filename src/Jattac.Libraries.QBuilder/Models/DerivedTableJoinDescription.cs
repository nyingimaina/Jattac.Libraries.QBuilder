namespace Jattac.Libraries.QBuilder.Models
{
    internal class DerivedTableJoinDescription
    {
        public string RightTable { get; set; }

        public string LeftField { get; set; }

        public QBuilder QBuilder { get; set; }

        public string RightField { get; set; }

        public string JoinType { get; set; }
    }
}