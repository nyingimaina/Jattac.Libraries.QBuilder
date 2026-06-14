using System;

namespace Jattac.Libraries.QBuilder
{
    public class JoinDescription
    {
        private Guid _id;

        public string LeftTable { get; set; } = string.Empty;

        public string LeftField { get; set; } = string.Empty;

        public string RightField { get; set; } = string.Empty;

        public string JoinType { get; set; } = string.Empty;

        public string ExplicitLeftTableAlias { get; set; } = string.Empty;

        public string DerivedTable { get; set; } = string.Empty;

        public string RightTable { get; set; } = string.Empty;

        public bool IsInitialDerivedTableJoin =>
            !string.IsNullOrEmpty(ExplicitLeftTableAlias)
            && !string.IsNullOrEmpty(DerivedTable)
            && string.IsNullOrEmpty(LeftTable);

        public bool IsSecondaryDerivedTableJoin =>
            !string.IsNullOrEmpty(ExplicitRightTableAlias);

        public string ExplicitRightTableAlias { get; internal set; }

        public bool Consumed { get; internal set; }

        internal Guid Id
        {
            get
            {
                if (_id == default(Guid))
                {
                    _id = Guid.NewGuid();
                }

                return _id;
            }
        }

        public override string ToString()
        {
            var leftTableDisplayName = GetTableDisplayName(LeftTable, ExplicitLeftTableAlias);
            var rightTableDisplayName = GetTableDisplayName(RightTable, ExplicitRightTableAlias);

            return $"{JoinType} {leftTableDisplayName}.{LeftField} = {rightTableDisplayName}.{RightField}";
        }

        private string GetTableDisplayName(string tableName, string explicitAlias)
        {
            var hasExplicitAlias = !string.IsNullOrEmpty(explicitAlias);
            if (hasExplicitAlias)
            {
                return explicitAlias;
            }
            else
            {
                return tableName;
            }
        }
    }
}