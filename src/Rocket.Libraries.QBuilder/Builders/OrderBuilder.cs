namespace Rocket.Libraries.Qurious.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Rocket.Libraries.Qurious.Helpers;
    using Rocket.Libraries.Qurious.Models;

    /// <summary>
    /// Builds the ORDER BY clause of a SQL query.
    /// Supports multiple columns with mixed ASC/DESC directions.
    /// </summary>
    public class OrderBuilder : BuilderBase
    {
        private readonly List<OrderDescription> _orders = new List<OrderDescription>();
        private readonly FieldNameResolver _fieldNameResolver = new FieldNameResolver();

        /// <summary>Initializes a new <see cref="OrderBuilder"/> attached to <paramref name="qBuilder"/>.</summary>
        public OrderBuilder(QBuilder qBuilder)
            : base(qBuilder)
        {
        }

        /// <summary>
        /// Adds an ascending ORDER BY column using a field name string.
        /// Call multiple times to order by additional columns.
        /// </summary>
        /// <typeparam name="TTable">The table the field belongs to.</typeparam>
        /// <param name="field">The column name.</param>
        public QBuilder OrderBy<TTable>(string field)
        {
            Append<TTable>(field, "Asc");
            return QBuilder;
        }

        /// <summary>
        /// Adds a descending ORDER BY column using a field name string.
        /// Call multiple times to order by additional columns.
        /// </summary>
        /// <typeparam name="TTable">The table the field belongs to.</typeparam>
        /// <param name="field">The column name.</param>
        public QBuilder OrderByDescending<TTable>(string field)
        {
            Append<TTable>(field, "Desc");
            return QBuilder;
        }

        /// <summary>
        /// Adds an ascending ORDER BY column via a lambda expression.
        /// </summary>
        /// <typeparam name="TTable">The table the field belongs to.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <param name="fieldSelector">Lambda selecting the field, e.g. <c>u => u.Name</c>.</param>
        public QBuilder OrderBy<TTable, TField>(Expression<Func<TTable, TField>> fieldSelector)
        {
            Append<TTable>(_fieldNameResolver.GetFieldName(fieldSelector), "Asc");
            return QBuilder;
        }

        /// <summary>
        /// Adds a descending ORDER BY column via a lambda expression.
        /// </summary>
        /// <typeparam name="TTable">The table the field belongs to.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <param name="fieldSelector">Lambda selecting the field, e.g. <c>u => u.CreatedAt</c>.</param>
        public QBuilder OrderByDescending<TTable, TField>(Expression<Func<TTable, TField>> fieldSelector)
        {
            Append<TTable>(_fieldNameResolver.GetFieldName(fieldSelector), "Desc");
            return QBuilder;
        }

        /// <summary>
        /// Prevents the next-appended ORDER BY column from being qualified with the table alias.
        /// Useful for ordering by a computed alias defined in the SELECT clause.
        /// </summary>
        public OrderBuilder DoNotQualifyWithTableName()
        {
            _pendingQualify = false;
            return this;
        }

        private bool _pendingQualify = true;

        private void Append<TTable>(string field, string mode)
        {
            _orders.Add(new OrderDescription
            {
                TableAlias = QBuilder.TableNameAliaser.GetTableAlias<TTable>(),
                Field = field,
                Mode = mode,
                QualifyWithTableName = _pendingQualify,
            });
            _pendingQualify = true;
        }

        internal string Build()
        {
            if (!_orders.Any())
            {
                return string.Empty;
            }

            var columns = string.Join(", ", _orders.Select(o =>
            {
                var col = o.QualifyWithTableName ? $"{o.TableAlias}.{o.Field}" : o.Field;
                return $"{col} {o.Mode}";
            }));

            return $"{Environment.NewLine}Order By {columns}";
        }
    }
}
