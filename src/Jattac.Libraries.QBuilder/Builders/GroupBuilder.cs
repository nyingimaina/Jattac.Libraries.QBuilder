namespace Jattac.Libraries.QBuilder.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Jattac.Libraries.QBuilder.Helpers;
    using Jattac.Libraries.QBuilder.Models;

    /// <summary>
    /// Builds the GROUP BY clause of a SQL query.
    /// </summary>
    public class GroupBuilder : BuilderBase
    {
        private readonly FieldNameResolver _fieldNameResolver = new FieldNameResolver();

        /// <summary>Initializes a new <see cref="GroupBuilder"/> attached to <paramref name="qBuilder"/>.</summary>
        public GroupBuilder(QBuilder qBuilder)
            : base(qBuilder)
        { }

        private List<GroupDescription> GroupFields { get; set; } = new List<GroupDescription>();

        /// <summary>
        /// Adds a column to the GROUP BY clause using a field name string.
        /// </summary>
        /// <typeparam name="TTable">The table the field belongs to.</typeparam>
        /// <param name="field">The column name.</param>
        /// <param name="tableAlias">Optional alias override for the table reference (e.g. for self-joins).</param>
        public GroupBuilder GroupBy<TTable>(string field, string tableAlias = null)
        {
            GroupFields.Add(new GroupDescription
            {
                FieldName = field,
                TableName = QBuilder.TableNameResolver(typeof(TTable)),
                ExplicitAlias = tableAlias,
            });
            return this;
        }

        /// <summary>
        /// Adds a column to the GROUP BY clause via a lambda expression.
        /// </summary>
        /// <typeparam name="TTable">The table the field belongs to.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <param name="fieldSelector">Lambda selecting the field, e.g. <c>o => o.UserId</c>.</param>
        /// <param name="tableAlias">Optional alias override for the table reference (e.g. for self-joins).</param>
        public GroupBuilder GroupBy<TTable, TField>(Expression<Func<TTable, TField>> fieldSelector, string tableAlias = null)
        {
            return GroupBy<TTable>(_fieldNameResolver.GetFieldName(fieldSelector), tableAlias);
        }

        internal string Build()
        {
            if (GroupFields.Count == 0)
            {
                return string.Empty;
            }

            var grouping = $"{Environment.NewLine} Group By ";
            foreach (var groupField in GroupFields)
            {
                var alias = groupField.ExplicitAlias ?? QBuilder.TableNameAliaser.GetTableAlias(groupField.TableName);
                grouping += $"{alias}.{groupField.FieldName},";
            }

            return grouping.Substring(0, grouping.Length - 1) + Environment.NewLine;
        }
    }
}
