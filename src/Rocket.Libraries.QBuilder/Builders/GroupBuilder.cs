namespace Rocket.Libraries.Qurious.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Rocket.Libraries.Qurious.Helpers;
    using Rocket.Libraries.Qurious.Models;

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
        public GroupBuilder GroupBy<TTable>(string field)
        {
            GroupFields.Add(new GroupDescription
            {
                FieldName = field,
                TableName = QBuilder.TableNameResolver(typeof(TTable)),
            });
            return this;
        }

        /// <summary>
        /// Adds a column to the GROUP BY clause via a lambda expression.
        /// </summary>
        /// <typeparam name="TTable">The table the field belongs to.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <param name="fieldSelector">Lambda selecting the field, e.g. <c>o => o.UserId</c>.</param>
        public GroupBuilder GroupBy<TTable, TField>(Expression<Func<TTable, TField>> fieldSelector)
        {
            return GroupBy<TTable>(_fieldNameResolver.GetFieldName(fieldSelector));
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
                grouping += $"{QBuilder.TableNameAliaser.GetTableAlias(groupField.TableName)}.{groupField.FieldName},";
            }

            return grouping.Substring(0, grouping.Length - 1) + Environment.NewLine;
        }
    }
}
