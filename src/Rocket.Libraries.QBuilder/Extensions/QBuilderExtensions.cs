namespace Rocket.Libraries.Qurious
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Rocket.Libraries.Qurious.Builders;
    using Rocket.Libraries.Qurious.Builders.Paging;
    using Rocket.Libraries.Qurious.Enums;
    using Rocket.Libraries.Qurious.Helpers;

    /// <summary>
    /// Fluent extension methods on <see cref="QBuilder"/>.
    /// Import this namespace to unlock the zero-boilerplate chainable API.
    /// Every method returns the same <see cref="QBuilder"/> instance so calls can be chained without
    /// ever touching a sub-builder directly.
    /// </summary>
    public static class QBuilderExtensions
    {
        // ─── SELECT ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a column to the SELECT list via a lambda expression.
        /// </summary>
        /// <typeparam name="TTable">The table the field belongs to.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <param name="qb">The query builder.</param>
        /// <param name="fieldSelector">Lambda selecting the column, e.g. <c>u => u.Name</c>.</param>
        /// <returns>The same <see cref="QBuilder"/> for chaining.</returns>
        public static QBuilder Select<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector)
        {
            qb.UseSelector().Select<TTable, TField>(fieldSelector);
            return qb;
        }

        /// <summary>
        /// Adds a column with an alias to the SELECT list via a lambda expression.
        /// </summary>
        /// <typeparam name="TTable">The table the field belongs to.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <param name="qb">The query builder.</param>
        /// <param name="fieldSelector">Lambda selecting the column, e.g. <c>o => o.Amount</c>.</param>
        /// <param name="alias">The column alias in the result set.</param>
        /// <returns>The same <see cref="QBuilder"/> for chaining.</returns>
        public static QBuilder Select<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            string alias)
        {
            qb.UseSelector().Select<TTable, TField>(fieldSelector, alias);
            return qb;
        }

        /// <summary>
        /// Adds an aggregate expression (<c>COUNT</c>, <c>SUM</c>, etc.) to the SELECT list.
        /// </summary>
        /// <typeparam name="TTable">The table the field belongs to.</typeparam>
        /// <typeparam name="TField">The field type.</typeparam>
        /// <param name="qb">The query builder.</param>
        /// <param name="fieldSelector">Lambda selecting the column to aggregate.</param>
        /// <param name="alias">Column alias for the aggregate result.</param>
        /// <param name="function">The aggregate function to apply.</param>
        /// <returns>The same <see cref="QBuilder"/> for chaining.</returns>
        public static QBuilder Aggregate<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            string alias,
            AggregateFunction function)
        {
            var fnSql = function switch
            {
                AggregateFunction.Count => "Count",
                AggregateFunction.CountDistinct => "CountDistinct",
                AggregateFunction.Sum => "Sum",
                AggregateFunction.Avg => "Avg",
                AggregateFunction.Min => "Min",
                AggregateFunction.Max => "Max",
                _ => throw new ArgumentOutOfRangeException(nameof(function), function, null),
            };

            if (function == AggregateFunction.CountDistinct)
            {
                var fieldName = new FieldNameResolver().GetFieldName(fieldSelector);
                var table = qb.TableNameAliaser.GetTableAlias<TTable>();
                qb.UseSelector().SelectExplicit(string.Empty, $"Count(Distinct {table}.{fieldName})", alias, string.Empty, preventTableNameAliasing: true, qualifyFieldWithTableName: false);
            }
            else
            {
                qb.UseSelector().SelectAggregated<TTable, TField>(fieldSelector, alias, fnSql);
            }

            return qb;
        }

        /// <summary>
        /// Restricts the SELECT to the first <paramref name="count"/> rows (<c>SELECT TOP n</c>).
        /// </summary>
        /// <param name="qb">The query builder.</param>
        /// <param name="count">Number of rows to return.</param>
        /// <returns>The same <see cref="QBuilder"/> for chaining.</returns>
        public static QBuilder Top(this QBuilder qb, long count)
        {
            qb.UseSelector().SelectTop(count);
            return qb;
        }

        /// <summary>
        /// Adds <c>DISTINCT</c> to the SELECT clause, eliminating duplicate rows.
        /// </summary>
        /// <param name="qb">The query builder.</param>
        /// <returns>The same <see cref="QBuilder"/> for chaining.</returns>
        public static QBuilder Distinct(this QBuilder qb)
        {
            qb.UseSelector().SelectDistinctRows();
            return qb;
        }

        /// <summary>
        /// Adds a <c>CASE WHEN … END AS alias</c> expression to the SELECT list.
        /// Build the expression with <see cref="CaseWhenBuilder"/>.
        /// </summary>
        /// <param name="qb">The query builder.</param>
        /// <param name="caseWhen">A configured <see cref="CaseWhenBuilder"/>.</param>
        /// <param name="alias">Column alias for the CASE expression.</param>
        /// <returns>The same <see cref="QBuilder"/> for chaining.</returns>
        public static QBuilder SelectCaseWhen(this QBuilder qb, CaseWhenBuilder caseWhen, string alias)
        {
            qb.UseSelector().SelectCaseWhen(caseWhen, alias);
            return qb;
        }

        // ─── JOIN ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds an INNER JOIN between <typeparamref name="TLeft"/> and <typeparamref name="TRight"/>.
        /// </summary>
        /// <typeparam name="TLeft">Left table.</typeparam>
        /// <typeparam name="TRight">Right table.</typeparam>
        /// <typeparam name="TLeftField">Join key type on the left table.</typeparam>
        /// <typeparam name="TRightField">Join key type on the right table.</typeparam>
        /// <param name="qb">The query builder.</param>
        /// <param name="leftField">Lambda selecting the join key on the left table, e.g. <c>u => u.Id</c>.</param>
        /// <param name="rightField">Lambda selecting the join key on the right table, e.g. <c>o => o.UserId</c>.</param>
        /// <returns>The same <see cref="QBuilder"/> for chaining.</returns>
        public static QBuilder InnerJoin<TLeft, TRight, TLeftField, TRightField>(
            this QBuilder qb,
            Expression<Func<TLeft, TLeftField>> leftField,
            Expression<Func<TRight, TRightField>> rightField)
        {
            qb.UseTableBoundJoinBuilder<TLeft, TRight>().InnerJoin(leftField, rightField);
            return qb;
        }

        /// <summary>
        /// Adds a LEFT JOIN between <typeparamref name="TLeft"/> and <typeparamref name="TRight"/>.
        /// Rows from <typeparamref name="TLeft"/> that have no matching row in <typeparamref name="TRight"/>
        /// are included with NULL values for <typeparamref name="TRight"/> columns.
        /// </summary>
        public static QBuilder LeftJoin<TLeft, TRight, TLeftField, TRightField>(
            this QBuilder qb,
            Expression<Func<TLeft, TLeftField>> leftField,
            Expression<Func<TRight, TRightField>> rightField)
        {
            qb.UseTableBoundJoinBuilder<TLeft, TRight>().LeftJoin(leftField, rightField);
            return qb;
        }

        /// <summary>
        /// Adds a RIGHT JOIN between <typeparamref name="TLeft"/> and <typeparamref name="TRight"/>.
        /// All rows from <typeparamref name="TRight"/> are returned, with NULLs for unmatched <typeparamref name="TLeft"/> columns.
        /// </summary>
        public static QBuilder RightJoin<TLeft, TRight, TLeftField, TRightField>(
            this QBuilder qb,
            Expression<Func<TLeft, TLeftField>> leftField,
            Expression<Func<TRight, TRightField>> rightField)
        {
            qb.UseTableBoundJoinBuilder<TLeft, TRight>().RightJoin(leftField, rightField);
            return qb;
        }

        /// <summary>
        /// Adds a FULL OUTER JOIN between <typeparamref name="TLeft"/> and <typeparamref name="TRight"/>.
        /// All rows from both tables are returned; unmatched sides are filled with NULLs.
        /// </summary>
        public static QBuilder FullOuterJoin<TLeft, TRight, TLeftField, TRightField>(
            this QBuilder qb,
            Expression<Func<TLeft, TLeftField>> leftField,
            Expression<Func<TRight, TRightField>> rightField)
        {
            qb.UseTableBoundJoinBuilder<TLeft, TRight>().FullJoin(leftField, rightField);
            return qb;
        }

        /// <summary>
        /// Adds a CROSS JOIN between <typeparamref name="TLeft"/> and <typeparamref name="TRight"/>,
        /// producing the Cartesian product of their rows. No ON condition is required or emitted.
        /// </summary>
        public static QBuilder CrossJoin<TLeft, TRight>(this QBuilder qb)
        {
            qb.UseJoiner().CrossJoin<TLeft, TRight>();
            return qb;
        }

        // ─── WHERE ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds the first WHERE predicate.
        /// For subsequent predicates use <see cref="AndWhere{TTable,TField}"/> or <see cref="OrWhere{TTable,TField}"/>.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <typeparam name="TField">Field type.</typeparam>
        /// <param name="qb">The query builder.</param>
        /// <param name="fieldSelector">Lambda selecting the column.</param>
        /// <param name="op">Comparison operator.</param>
        /// <param name="value">Value to compare against. May be <c>null</c> for <see cref="FilterOperator.IsNull"/> / <see cref="FilterOperator.IsNotNull"/>.</param>
        /// <returns>The same <see cref="QBuilder"/> for chaining.</returns>
        public static QBuilder Where<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            FilterOperator op,
            object value = null)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().Where<TTable>(field, op, value);
            return qb;
        }

        /// <summary>Appends an AND WHERE predicate.</summary>
        public static QBuilder AndWhere<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            FilterOperator op,
            object value = null)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().UseConjunction().And().Where<TTable>(field, op, value);
            return qb;
        }

        /// <summary>Appends an OR WHERE predicate.</summary>
        public static QBuilder OrWhere<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            FilterOperator op,
            object value = null)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().UseConjunction().Or().Where<TTable>(field, op, value);
            return qb;
        }

        /// <summary>
        /// Adds a WHERE <c>field IS NULL</c> predicate.
        /// </summary>
        public static QBuilder WhereIsNull<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().WhereIsNull<TTable>(field);
            return qb;
        }

        /// <summary>Appends an AND WHERE <c>field IS NULL</c> predicate.</summary>
        public static QBuilder AndWhereIsNull<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().UseConjunction().And().WhereIsNull<TTable>(field);
            return qb;
        }

        /// <summary>
        /// Adds a WHERE <c>field IS NOT NULL</c> predicate.
        /// </summary>
        public static QBuilder WhereIsNotNull<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().WhereIsNotNull<TTable>(field);
            return qb;
        }

        /// <summary>Appends an AND WHERE <c>field IS NOT NULL</c> predicate.</summary>
        public static QBuilder AndWhereIsNotNull<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().UseConjunction().And().WhereIsNotNull<TTable>(field);
            return qb;
        }

        /// <summary>
        /// Adds a WHERE <c>field BETWEEN from AND to</c> predicate.
        /// </summary>
        /// <param name="from">Inclusive lower bound.</param>
        /// <param name="to">Inclusive upper bound.</param>
        public static QBuilder WhereBetween<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            object from,
            object to)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().WhereBetween<TTable>(field, from, to);
            return qb;
        }

        /// <summary>Appends an AND WHERE BETWEEN predicate.</summary>
        public static QBuilder AndWhereBetween<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            object from,
            object to)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().UseConjunction().And().WhereBetween<TTable>(field, from, to);
            return qb;
        }

        /// <summary>
        /// Adds a WHERE <c>field IN (values)</c> predicate.
        /// Silently no-ops when <paramref name="values"/> is null or empty.
        /// </summary>
        public static QBuilder WhereIn<TTable, TField, TValue>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            IEnumerable<TValue> values)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().WhereIn<TTable, TValue>(field, values);
            return qb;
        }

        /// <summary>Appends an AND WHERE IN predicate.</summary>
        public static QBuilder AndWhereIn<TTable, TField, TValue>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            IEnumerable<TValue> values)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().UseConjunction().And().WhereIn<TTable, TValue>(field, values);
            return qb;
        }

        /// <summary>
        /// Adds a WHERE <c>field NOT IN (values)</c> predicate.
        /// Silently no-ops when <paramref name="values"/> is null or empty.
        /// </summary>
        public static QBuilder WhereNotIn<TTable, TField, TValue>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            IEnumerable<TValue> values)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().WhereNotIn<TTable, TValue>(field, values);
            return qb;
        }

        /// <summary>Appends an AND WHERE NOT IN predicate.</summary>
        public static QBuilder AndWhereNotIn<TTable, TField, TValue>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            IEnumerable<TValue> values)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseFilter().UseConjunction().And().WhereNotIn<TTable, TValue>(field, values);
            return qb;
        }

        /// <summary>
        /// Adds a WHERE EXISTS (subquery) predicate.
        /// </summary>
        /// <param name="qb">The query builder.</param>
        /// <param name="subQuery">A <see cref="QBuilder"/> whose <c>Build()</c> result is the subquery body.</param>
        public static QBuilder WhereExists(this QBuilder qb, QBuilder subQuery)
        {
            qb.UseFilter().WhereExists(subQuery);
            return qb;
        }

        /// <summary>Appends an AND WHERE EXISTS predicate.</summary>
        public static QBuilder AndWhereExists(this QBuilder qb, QBuilder subQuery)
        {
            qb.UseFilter().UseConjunction().And().WhereExists(subQuery);
            return qb;
        }

        /// <summary>Adds a WHERE NOT EXISTS predicate.</summary>
        public static QBuilder WhereNotExists(this QBuilder qb, QBuilder subQuery)
        {
            qb.UseFilter().WhereNotExists(subQuery);
            return qb;
        }

        /// <summary>Appends an AND WHERE NOT EXISTS predicate.</summary>
        public static QBuilder AndWhereNotExists(this QBuilder qb, QBuilder subQuery)
        {
            qb.UseFilter().UseConjunction().And().WhereNotExists(subQuery);
            return qb;
        }

        /// <summary>
        /// Opens a parenthesis group in the WHERE clause.
        /// Every <c>OpenGroup</c> must be paired with a <c>CloseGroup</c>.
        /// Nested groups are fully supported.
        /// </summary>
        public static QBuilder OpenGroup(this QBuilder qb)
        {
            qb.UseFilter().OpenParentheses();
            return qb;
        }

        /// <summary>Closes the most recently opened parenthesis group in the WHERE clause.</summary>
        public static QBuilder CloseGroup(this QBuilder qb)
        {
            qb.UseFilter().CloseParentheses();
            return qb;
        }

        // ─── HAVING ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a HAVING predicate to filter grouped rows.
        /// Call after at least one <see cref="GroupBy{TTable,TField}(QBuilder, Expression{Func{TTable,TField}})"/>.
        /// </summary>
        public static QBuilder Having<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            FilterOperator op,
            object value)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseHaving().Where<TTable>(field, op, value);
            return qb;
        }

        /// <summary>Appends an AND HAVING predicate.</summary>
        public static QBuilder AndHaving<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            FilterOperator op,
            object value)
        {
            var field = new FieldNameResolver().GetFieldName(fieldSelector);
            qb.UseHaving().UseConjunction().And().Where<TTable>(field, op, value);
            return qb;
        }

        // ─── GROUP BY ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds a column to the GROUP BY clause via a lambda expression.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <typeparam name="TField">Field type.</typeparam>
        /// <param name="qb">The query builder.</param>
        /// <param name="fieldSelector">Lambda selecting the column, e.g. <c>o => o.UserId</c>.</param>
        /// <returns>The same <see cref="QBuilder"/> for chaining.</returns>
        public static QBuilder GroupBy<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector)
        {
            qb.UseGrouper().GroupBy<TTable, TField>(fieldSelector);
            return qb;
        }

        // ─── ORDER BY ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Adds an ascending ORDER BY column via a lambda expression.
        /// Call multiple times to sort by additional columns.
        /// </summary>
        /// <typeparam name="TTable">Table the field belongs to.</typeparam>
        /// <typeparam name="TField">Field type.</typeparam>
        /// <param name="qb">The query builder.</param>
        /// <param name="fieldSelector">Lambda selecting the column, e.g. <c>u => u.Name</c>.</param>
        /// <returns>The same <see cref="QBuilder"/> for chaining.</returns>
        public static QBuilder OrderBy<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector)
        {
            qb.UseOrdering().OrderBy<TTable, TField>(fieldSelector);
            return qb;
        }

        /// <summary>Adds a descending ORDER BY column.</summary>
        public static QBuilder OrderByDescending<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector)
        {
            qb.UseOrdering().OrderByDescending<TTable, TField>(fieldSelector);
            return qb;
        }

        /// <summary>
        /// Adds an additional ascending sort column.
        /// Semantically equivalent to <see cref="OrderBy{TTable,TField}(QBuilder,Expression{Func{TTable,TField}})"/>
        /// but makes secondary-sort intent explicit.
        /// </summary>
        public static QBuilder ThenBy<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector)
        {
            return OrderBy<TTable, TField>(qb, fieldSelector);
        }

        /// <summary>Adds an additional descending sort column.</summary>
        public static QBuilder ThenByDescending<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector)
        {
            return OrderByDescending<TTable, TField>(qb, fieldSelector);
        }

        // ─── PAGING ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Applies SQL Server ROW_NUMBER() paging.
        /// Compatible with SQL Server 2005+ and Azure SQL.
        /// </summary>
        /// <typeparam name="TTable">Table whose field drives the ORDER BY inside the row-number window.</typeparam>
        /// <typeparam name="TField">Field type.</typeparam>
        /// <param name="qb">The query builder.</param>
        /// <param name="fieldSelector">Lambda selecting the ORDER BY field.</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Rows per page (min 1).</param>
        /// <param name="orderAscending"><c>true</c> for ASC (default); <c>false</c> for DESC.</param>
        /// <returns>The same <see cref="QBuilder"/> for chaining.</returns>
        public static QBuilder PageSqlServer<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            uint page,
            ushort pageSize,
            bool orderAscending = true)
        {
            qb.UseSqlServerPagingBuilder<TTable>().PageBy(fieldSelector, page, pageSize, orderAscending);
            return qb;
        }

        /// <summary>
        /// Applies OFFSET / FETCH NEXT paging (SQL Server 2012+ / ANSI SQL).
        /// Requires an ORDER BY clause — call <see cref="OrderBy{TTable,TField}(QBuilder,Expression{Func{TTable,TField}})"/> first,
        /// or let this method emit it via <paramref name="fieldSelector"/>.
        /// </summary>
        /// <typeparam name="TTable">Table whose field drives the ORDER BY.</typeparam>
        /// <typeparam name="TField">Field type.</typeparam>
        /// <param name="qb">The query builder.</param>
        /// <param name="fieldSelector">Lambda selecting the ORDER BY field.</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Rows per page (min 1).</param>
        /// <param name="orderAscending"><c>true</c> for ASC (default); <c>false</c> for DESC.</param>
        public static QBuilder PageOffsetFetch<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            uint page,
            ushort pageSize,
            bool orderAscending = true)
        {
            qb.UseOffsetFetchPagingBuilder<TTable>().PageBy(fieldSelector, page, pageSize, orderAscending);
            return qb;
        }

        /// <summary>
        /// Applies MySQL LIMIT / OFFSET paging.
        /// </summary>
        /// <typeparam name="TTable">Table whose field drives the ORDER BY.</typeparam>
        /// <typeparam name="TField">Field type.</typeparam>
        /// <param name="qb">The query builder.</param>
        /// <param name="fieldSelector">Lambda selecting the ORDER BY field.</param>
        /// <param name="page">1-based page number.</param>
        /// <param name="pageSize">Rows per page (min 1).</param>
        /// <param name="orderAscending"><c>true</c> for ASC (default); <c>false</c> for DESC.</param>
        public static QBuilder PageMySql<TTable, TField>(
            this QBuilder qb,
            Expression<Func<TTable, TField>> fieldSelector,
            uint page,
            ushort pageSize,
            bool orderAscending = true)
        {
            qb.UseMySqlServerPagingBuilder<TTable>().PageBy(fieldSelector, page, pageSize, orderAscending);
            return qb;
        }
    }
}
