namespace Jattac.Libraries.QBuilder.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Jattac.Libraries.QBuilder.Enums;
    using Jattac.Libraries.QBuilder.Helpers;
    using Jattac.Libraries.QBuilder.Models;

    public class JoinBuilder : BuilderBase
    {
        private FieldNameResolver _fieldNameResolver = new FieldNameResolver();

        private List<string> _alreadyAliasedTables = new List<string>();

        private List<string> _joinedDerivedTables = new List<string>();

        public JoinBuilder(QBuilder qBuilder)
            : base(qBuilder)
        {
        }

        internal string FirstTableName
        {
            get
            {
                var firstJoin = Joins.FirstOrDefault();
                Guard.Against(firstJoin == null, "Could not find a join to use for first table");
                if (firstJoin.IsInitialDerivedTableJoin)
                {
                    return firstJoin.RightTable;
                }
                else
                {
                    return firstJoin.LeftTable;
                }
            }
        }

        internal bool JoinsExist => Joins.Count > 0;

        internal string FirstTableAlias
        {
            get
            {
                var firstJoin = Joins.FirstOrDefault();
                if (firstJoin == null || firstJoin.IsInitialDerivedTableJoin)
                {
                    return null;
                }
                return firstJoin.RegularLeftAlias;
            }
        }

        internal InnerSelectDescription InnerSelectDescription { get; set; }

        internal List<string> JoinedDerivedTables => _joinedDerivedTables;

        private List<JoinDescription> Joins { get; set; } = new List<JoinDescription>();

        public JoinBuilder InnerJoin<TLeftTable, TLeftField, TRightTable, TRightField>(Expression<Func<TLeftTable, TLeftField>> leftFieldNameDescriptor, Expression<Func<TRightTable, TRightField>> rightFieldNameDescriptor, string joinType, string leftAlias = null, string rightAlias = null)
        {
            QueueJoin(leftFieldNameDescriptor, rightFieldNameDescriptor, joinType, leftAlias, rightAlias);
            return this;
        }

        public DerivedTableJoiner<TOuterTable> UseDerivedTableJoiner<TOuterTable>()
        {
            return new DerivedTableJoiner<TOuterTable>(this);
        }

        public JoinBuilder InnerJoin<TLeftTable, TRightTable>(string leftField, string rightField, string leftAlias = null, string rightAlias = null)
        {
            QueueJoin<TLeftTable, TRightTable>(leftField, rightField, JoinTypes.Inner, leftAlias, rightAlias);
            return this;
        }

        public JoinBuilder FullJoin<TLeftTable, TRightTable>(string leftField, string rightField, string leftAlias = null, string rightAlias = null)
        {
            QueueJoin<TLeftTable, TRightTable>(leftField, rightField, JoinTypes.Full, leftAlias, rightAlias);
            return this;
        }

        public JoinBuilder LeftJoin<TLeftTable, TRightTable>(string leftField, string rightField, string leftAlias = null, string rightAlias = null)
        {
            QueueJoin<TLeftTable, TRightTable>(leftField, rightField, JoinTypes.LeftJoin, leftAlias, rightAlias);
            return this;
        }

        public JoinBuilder RightJoin<TLeftTable, TRightTable>(string leftField, string rightField, string leftAlias = null, string rightAlias = null)
        {
            QueueJoin<TLeftTable, TRightTable>(leftField, rightField, JoinTypes.RightJoin, leftAlias, rightAlias);
            return this;
        }

        /// <summary>
        /// Adds a CROSS JOIN between two tables, producing the Cartesian product of their rows.
        /// No ON condition is required or emitted.
        /// </summary>
        /// <typeparam name="TLeftTable">The left (driving) table.</typeparam>
        /// <typeparam name="TRightTable">The right table to cross-join with.</typeparam>
        public JoinBuilder CrossJoin<TLeftTable, TRightTable>()
        {
            Joins.Add(new JoinDescription
            {
                LeftTable = QBuilder.TableNameResolver(typeof(TLeftTable)),
                RightTable = QBuilder.TableNameResolver(typeof(TRightTable)),
                JoinType = JoinTypes.Cross,
            });
            return this;
        }

        /*[Obsolete("This is an awfully painful way of joining to derived tables. Use DerivedTableJoiner instead")]
        public QBuilder BeginInnerJoinToDerivedTable(string derivedTableName, string innerField, string field)
        {
            InnerSelectDescription = new InnerSelectDescription
            {
                Field = field,
                InnerField = innerField,
                QBuilder = new QBuilder(QBuilder.TableNameResolver, derivedTableName),
                Parent = this,
                DerivedTableName = derivedTableName,
            };
            InnerSelectDescription.QBuilder.InnerSelectDescription = InnerSelectDescription;
            return InnerSelectDescription.QBuilder;
        }*/

        public override QBuilder Then()
        {
            //DataValidator.EvaluateImmediate(QBuilder.InnerSelectDescription != null, $"You are currently in a '{nameof(BeginInnerJoinToDerivedTable)}' section. Please call '{nameof(QBuilder.FinishJoinToDerivedTable)}' instead");
            return base.Then();
        }

        internal JoinBuilder JoinDerivedTable<TRightTable, TRightField>(Expression<Func<TRightTable, TRightField>> rightFieldNameDescriptor, QBuilder derivedTable, string derivedFieldName, string joinType)
        {
            var alreadyUsedDerivedTableInPreviousJoin = JoinedDerivedTables.FirstOrDefault(a => a.Equals(derivedTable.DerivedTableName, StringComparison.InvariantCultureIgnoreCase))
                != null;
            var rightField = _fieldNameResolver.GetFieldName(rightFieldNameDescriptor);
            var rightTable = QBuilder.TableNameResolver(typeof(TRightTable));
            if (alreadyUsedDerivedTableInPreviousJoin)
            {
                SecondaryDerivedTableJoin(rightField, rightTable, derivedTable, derivedFieldName, joinType);
            }
            else
            {
                InitialDerivedTableJoin(rightField, rightTable, derivedTable, derivedFieldName, joinType);
            }

            return this;
        }

        internal void SecondaryDerivedTableJoin(string rightField, string rightTable, QBuilder derivedTable, string derivedFieldName, string joinType)
        {
            var joinDescription = new JoinDescription
            {
                LeftTable = rightTable,
                LeftField = rightField,
                ExplicitRightTableAlias = DerivedTableWrapperNameResolver.GetWrapperName(derivedTable.DerivedTableName),
                RightField = derivedFieldName,
                JoinType = joinType,
            };
            Joins.Add(joinDescription);
        }

        internal void InitialDerivedTableJoin(string rightField, string rightTable, QBuilder derivedTable, string derivedFieldName, string joinType)
        {
            var derivedTableJoinDescription = new DerivedTableJoinDescription
            {
                RightField = rightField,
                RightTable = rightTable,
                LeftField = derivedFieldName,
                QBuilder = derivedTable,
                JoinType = joinType,
            };
            JoinedDerivedTables.Add(derivedTable.DerivedTableName);
            TranslateToJoinDescription(derivedTableJoinDescription);
        }

        internal bool TableNotKnown(string table)
        {
            var match = Joins.FirstOrDefault(a => TableFoundInJoin(table, a));
            var isRawTableJoin = match != null;
            var isDerivedTable = _joinedDerivedTables.FirstOrDefault(a => a.Equals(table, StringComparison.InvariantCultureIgnoreCase)) != null;
            return isDerivedTable == false && isRawTableJoin == false;
        }

        internal bool TableFoundInJoin(string table, JoinDescription joinDescription)
        {
            var leftTable = string.Empty;
            var rightTable = joinDescription.RightTable;
            if (joinDescription.IsInitialDerivedTableJoin)
            {
                leftTable = DerivedTableWrapperNameResolver.GetWrapperName(joinDescription.ExplicitLeftTableAlias);
            }
            else
            {
                leftTable = joinDescription.LeftTable;
            }

            return leftTable.Equals(table, StringComparison.CurrentCultureIgnoreCase)
            || rightTable.Equals(table, StringComparison.CurrentCultureIgnoreCase);
        }

        internal string Build()
        {
            _alreadyAliasedTables.Add(QBuilder.FirstTableName);
            var joins = string.Empty;
            foreach (var joinDescription in Joins)
            {
                joins += GetJoinLine(joinDescription);
            }

            Joins = new List<JoinDescription>();
            return joins;
        }

        private void TranslateToJoinDescription(DerivedTableJoinDescription derivedTableJoinDescription)
        {
            var joinDescription = new JoinDescription
            {
                JoinType = derivedTableJoinDescription.JoinType,
                RightField = derivedTableJoinDescription.RightField,
                RightTable = derivedTableJoinDescription.RightTable,
                LeftField = derivedTableJoinDescription.LeftField,
                ExplicitLeftTableAlias = derivedTableJoinDescription.QBuilder.DerivedTableName,
                DerivedTable = derivedTableJoinDescription.QBuilder.Build(),
            };
            Joins.Add(joinDescription);
        }

        private void QueueJoin<TLeftTable, TLeftField, TRightTable, TRightField>(Expression<Func<TLeftTable, TLeftField>> leftFieldNameDescriptor, Expression<Func<TRightTable, TRightField>> rightFieldNameDescriptor, string joinType, string leftAlias = null, string rightAlias = null)
        {
            var leftField = _fieldNameResolver.GetFieldName(leftFieldNameDescriptor);
            var rightField = _fieldNameResolver.GetFieldName(rightFieldNameDescriptor);
            QueueJoin<TLeftTable, TRightTable>(leftField, rightField, joinType, leftAlias, rightAlias);
        }

        private void QueueJoin<TLeftTable, TRightTable>(string leftField, string rightField, string joinType, string leftAlias = null, string rightAlias = null)
        {
            Joins.Add(new JoinDescription
            {
                LeftField = leftField,
                LeftTable = QBuilder.TableNameResolver(typeof(TLeftTable)),
                RightField = rightField,
                RightTable = QBuilder.TableNameResolver(typeof(TRightTable)),
                JoinType = joinType,
                RegularLeftAlias = leftAlias,
                RegularRightAlias = rightAlias,
            });
        }

        private string GetLeftTableAlias(JoinDescription joinDescription)
        {
            var leftTableAlias = joinDescription.ExplicitLeftTableAlias;
            if (string.IsNullOrEmpty(leftTableAlias))
            {
                leftTableAlias = QBuilder.TableNameAliaser.GetTableAlias(joinDescription.LeftTable);
            }

            return leftTableAlias;
        }

        private string GetJoinLine(JoinDescription joinDescription)
        {
            if (joinDescription.IsInitialDerivedTableJoin)
            {
                return GetInitialDerivedTableJoin(joinDescription);
            }
            else if (joinDescription.IsSecondaryDerivedTableJoin)
            {
                if (joinDescription.Consumed == false)
                {
                    return GetSecondaryDerivedTableJoin(joinDescription);
                }
                else
                {
                    return string.Empty;
                }
            }
            else
            {
                return GetNonDerivedTableJoinLine(joinDescription);
            }
        }

        private string GetInitialDerivedTableJoin(JoinDescription joinDescription)
        {
            var derivedTableAlias = DerivedTableWrapperNameResolver.GetWrapperName(GetLeftTableAlias(joinDescription));
            var joinPrefix = GetJoinPrefix(joinDescription);
            var secondaryJoins = Joins
                .Where(a => a.Id != joinDescription.Id)
                .Where(a =>
            {
                var sameOuterTable = a.LeftTable.Equals(joinDescription.RightTable, StringComparison.InvariantCultureIgnoreCase);
                var hasDerivedTable = !string.IsNullOrEmpty(a.ExplicitRightTableAlias);
                var sameDerivedTable = hasDerivedTable && a.ExplicitRightTableAlias.Equals(derivedTableAlias, StringComparison.InvariantCultureIgnoreCase);
                return sameOuterTable && sameDerivedTable;
            }).ToList();

            var rightTableAlias = QBuilder.TableNameAliaser.GetTableAlias(joinDescription.RightTable);
            var line = $"{joinPrefix} join ({joinDescription.DerivedTable}) as {derivedTableAlias} on {derivedTableAlias}.{joinDescription.LeftField} = {rightTableAlias}.{joinDescription.RightField}";
            line += Environment.NewLine;
            foreach (var secJoin in secondaryJoins)
            {
                secJoin.Consumed = true;
                line += $" and {rightTableAlias}.{secJoin.LeftField} = {secJoin.ExplicitRightTableAlias}.{secJoin.RightField}";
            }

            return line;
        }

        private string GetSecondaryDerivedTableJoin(JoinDescription joinDescription)
        {
            FlipTablesIfLeftTableAlreadyAliased(joinDescription);
            var joinPrefix = GetJoinPrefix(joinDescription);
            var rightTableAlias = joinDescription.ExplicitRightTableAlias;
            var leftTableAlias = QBuilder.TableNameAliaser.GetTableAlias(joinDescription.LeftTable);
            var line = $"{joinPrefix}join {joinDescription.LeftTable} {leftTableAlias} on {leftTableAlias}.{joinDescription.LeftField}";
            line += $" = {rightTableAlias}.{joinDescription.RightField}{Environment.NewLine}";
            return line;
        }

        private string GetNonDerivedTableJoinLine(JoinDescription joinDescription)
        {
            var dialect = QBuilder.Dialect;
            var isCrossJoin = joinDescription.JoinType == JoinTypes.Cross;
            if (isCrossJoin)
            {
                var crossRightAlias = joinDescription.RegularRightAlias ?? QBuilder.TableNameAliaser.GetTableAlias(joinDescription.RightTable);
                var qCrossRight = IdentifierQuoter.QuoteTable(joinDescription.RightTable, dialect);
                var qCrossRightAlias = IdentifierQuoter.QuoteIdentifier(crossRightAlias, dialect);
                return $"Cross join {qCrossRight} {qCrossRightAlias}{Environment.NewLine}";
            }

            var hasExplicitAliases = !string.IsNullOrEmpty(joinDescription.RegularLeftAlias) && !string.IsNullOrEmpty(joinDescription.RegularRightAlias);
            if (!hasExplicitAliases)
            {
                FlipTablesIfLeftTableAlreadyAliased(joinDescription);
            }

            var joinPrefix = GetJoinPrefix(joinDescription);
            var resolvedLeftAlias = joinDescription.RegularLeftAlias ?? QBuilder.TableNameAliaser.GetTableAlias(joinDescription.LeftTable);
            var resolvedRightAlias = joinDescription.RegularRightAlias ?? QBuilder.TableNameAliaser.GetTableAlias(joinDescription.RightTable);
            var qLeft = IdentifierQuoter.QuoteTable(joinDescription.LeftTable, dialect);
            var qLeftAlias = IdentifierQuoter.QuoteIdentifier(resolvedLeftAlias, dialect);
            var qRightAlias = IdentifierQuoter.QuoteIdentifier(resolvedRightAlias, dialect);
            var qLeftField = IdentifierQuoter.QuoteIdentifier(joinDescription.LeftField, dialect);
            var qRightField = IdentifierQuoter.QuoteIdentifier(joinDescription.RightField, dialect);
            var line = $"{joinPrefix}join {qLeft} {qLeftAlias} on {qLeftAlias}.{qLeftField}";
            line += $" = {qRightAlias}.{qRightField}{Environment.NewLine}";
            return line;
        }

        private void FlipTablesIfLeftTableAlreadyAliased(JoinDescription joinDescription)
        {
            var searchResult = _alreadyAliasedTables.FirstOrDefault(a => a.Equals(joinDescription.LeftTable, StringComparison.InvariantCultureIgnoreCase));
            var leftTableAlreadyAliased = searchResult != null;
            if (leftTableAlreadyAliased)
            {
                new JoinDescriptionFlipper().Flip(joinDescription);
            }
        }

        private string GetJoinPrefix(JoinDescription joinDescription)
        {
            switch (joinDescription.JoinType)
            {
                default:
                    var joinTypeIsUnsupported = true;
                    Guard.Against(joinTypeIsUnsupported, $"Unsupported join type '{joinDescription.JoinType}'");
                    break;

                case JoinTypes.Full:
                    return "Full Outer ";

                case JoinTypes.Inner:
                    return string.Empty;

                case JoinTypes.RightJoin:
                    return "Right ";

                case JoinTypes.LeftJoin:
                    return "Left ";

                case JoinTypes.Cross:
                    return "Cross ";
            }
            return string.Empty;
        }
    }
}