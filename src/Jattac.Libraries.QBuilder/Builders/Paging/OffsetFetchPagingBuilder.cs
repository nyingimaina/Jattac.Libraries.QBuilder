namespace Jattac.Libraries.QBuilder.Builders.Paging
{
    using System;
    using System.Linq.Expressions;
    using Jattac.Libraries.QBuilder.Enums;
    using Jattac.Libraries.QBuilder.Helpers;

    /// <summary>
    /// Implements SQL Server 2012+ / ANSI SQL <c>OFFSET … ROWS FETCH NEXT … ROWS ONLY</c> paging.
    /// The ORDER BY, OFFSET, and FETCH are emitted on the outer query so they can reference any column
    /// in the result set — including computed aliases.
    /// </summary>
    /// <typeparam name="TTable">The table whose field drives the ORDER BY.</typeparam>
    public class OffsetFetchPagingBuilder<TTable> : BuilderBase, IPagingBuilder<TTable>
    {
        /// <summary>Initializes a new <see cref="OffsetFetchPagingBuilder{TTable}"/> attached to <paramref name="qBuilder"/>.</summary>
        public OffsetFetchPagingBuilder(QBuilder qBuilder)
            : base(qBuilder)
        {
        }

        /// <inheritdoc/>
        public byte AbsoluteFirstRecordIndex => 1;

        /// <summary>
        /// Applies OFFSET/FETCH paging sorted by <paramref name="fieldNameDescriber"/>.
        /// </summary>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <param name="fieldNameDescriber">Lambda selecting the ORDER BY field, e.g. <c>u => u.Name</c>.</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Number of rows per page (minimum 1).</param>
        /// <param name="orderAscending"><c>true</c> for ASC (default); <c>false</c> for DESC.</param>
        /// <returns>The owning <see cref="QBuilder"/> for continued chaining.</returns>
        public QBuilder PageBy<TField>(
            Expression<Func<TTable, TField>> fieldNameDescriber,
            uint page,
            ushort pageSize,
            bool orderAscending = true)
        {
            Guard.Range(page >= 1, nameof(page), $"Page '{page}' is invalid — pages must be >= 1.");
            Guard.Range(pageSize >= 1, nameof(pageSize), $"Page size '{pageSize}' is invalid — must be >= 1.");

            var fieldName = new FieldNameResolver().GetFieldName(fieldNameDescriber);
            var outerAlias = QBuilder.DerivedTableName;
            var direction = orderAscending ? "Asc" : "Desc";
            var offset = (page - 1) * (long)pageSize;
            var dialect = QBuilder.Dialect;
            var qAlias = IdentifierQuoter.QuoteIdentifier(outerAlias, dialect);
            var qField = IdentifierQuoter.QuoteIdentifier(fieldName, dialect);

            QBuilder.SetSuffix(
                $"Order By {qAlias}.{qField} {direction} " +
                $"Offset {offset} Rows Fetch Next {pageSize} Rows Only");

            return QBuilder;
        }
    }
}
