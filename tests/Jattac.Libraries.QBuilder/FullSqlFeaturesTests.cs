using System;
using System.Linq;
using Jattac.Libraries.QBuilder;
using Jattac.Libraries.QBuilder.Builders;
using Jattac.Libraries.QBuilder.Enums;
using Jattac.QBuilderTests.Models;
using Xunit;

namespace Jattac.QBuilderTests
{
    /// <summary>
    /// Tests for every new SQL feature added in v7.0:
    /// IS NULL / IS NOT NULL, BETWEEN, EXISTS, nested groups, HAVING, CTE, set ops, CASE WHEN, CROSS JOIN.
    /// </summary>
    public class FullSqlFeaturesTests
    {
        private static string Norm(string sql) =>
            sql.Replace("\r\n", "\n").Replace("\r", "\n").Trim();

        // ── IS NULL / IS NOT NULL ─────────────────────────────────────────────

        [Fact]
        public void WhereIsNull_EmitsCorrectSql()
        {
            var sql = new QBuilder(parameterize: false)
                .UseSelector().Select<Order>("Id").Then()
                .UseFilter().WhereIsNull<Order>("DeletedAt")
                .Then().Build();

            Assert.Contains("IS NULL", sql);
            Assert.Contains("DeletedAt", sql);
        }

        [Fact]
        public void WhereIsNotNull_EmitsCorrectSql()
        {
            var sql = new QBuilder(parameterize: false)
                .UseSelector().Select<Order>("Id").Then()
                .UseFilter().WhereIsNotNull<Order>("DeletedAt")
                .Then().Build();

            Assert.Contains("IS NOT NULL", sql);
        }

        [Fact]
        public void WhereIsNull_DoesNotRequireValue()
        {
            // Should not throw even though no value is supplied
            var exception = Record.Exception(() =>
            {
                new QBuilder(parameterize: false)
                    .UseSelector().Select<Order>("Id").Then()
                    .UseFilter().WhereIsNull<Order>("DeletedAt")
                    .Then().Build();
            });
            Assert.Null(exception);
        }

        // ── BETWEEN / NOT BETWEEN ─────────────────────────────────────────────

        [Fact]
        public void WhereBetween_EmitsCorrectSql()
        {
            var sql = new QBuilder(parameterize: false)
                .UseSelector().Select<Order>("Amount").Then()
                .UseFilter().WhereBetween<Order>("Amount", 10, 100)
                .Then().Build();

            Assert.Contains("Between", sql);
            Assert.Contains("10", sql);
            Assert.Contains("100", sql);
        }

        [Fact]
        public void WhereNotBetween_EmitsCorrectSql()
        {
            var sql = new QBuilder(parameterize: false)
                .UseSelector().Select<Order>("Amount").Then()
                .UseFilter().WhereNotBetween<Order>("Amount", 200, 500)
                .Then().Build();

            Assert.Contains("Not Between", sql);
        }

        [Fact]
        public void WhereBetween_Parameterized_AddsParams()
        {
            var result = new QBuilder(parameterize: true)
                .UseSelector().Select<Order>("Amount").Then()
                .UseFilter().WhereBetween<Order>("Amount", 10, 100)
                .Then().BuildWithParameters();

            Assert.Equal(2, result.Parameters.Count);
            Assert.Contains("@Amount0", result.ParameterizedSql);
            Assert.Contains("@Amount1", result.ParameterizedSql);
        }

        // ── EXISTS / NOT EXISTS ───────────────────────────────────────────────

        [Fact]
        public void WhereExists_EmitsCorrectSql()
        {
            var subQuery = new QBuilder(parameterize: false)
                .UseSelector().Select<Order>("Id").Then()
                .UseFilter().Where<Order>("Status", FilterOperator.EqualTo, "active")
                .Then();

            var sql = new QBuilder(parameterize: false)
                .UseSelector().Select<User>("Id").Then()
                .UseFilter().WhereExists(subQuery)
                .Then().Build();

            Assert.Contains("Exists (", sql);
        }

        [Fact]
        public void WhereNotExists_EmitsCorrectSql()
        {
            var subQuery = new QBuilder(parameterize: false)
                .UseSelector().Select<Order>("Id").Then();

            var sql = new QBuilder(parameterize: false)
                .UseSelector().Select<User>("Id").Then()
                .UseFilter().WhereNotExists(subQuery)
                .Then().Build();

            Assert.Contains("Not Exists (", sql);
        }

        // ── Nested parentheses ────────────────────────────────────────────────

        [Fact]
        public void NestedParentheses_DoNotThrow()
        {
            var exception = Record.Exception(() =>
            {
                new QBuilder(parameterize: false)
                    .UseSelector().Select<Order>("Status").Then()
                    .UseFilter()
                        .OpenParentheses()
                            .Where<Order>("Status", FilterOperator.EqualTo, "new")
                        .CloseParentheses()
                    .Then().Build();
            });
            Assert.Null(exception);
        }

        [Fact]
        public void NestedParentheses_EmitsParensInSql()
        {
            var sql = new QBuilder(parameterize: false)
                .UseSelector().Select<Order>("Status").Then()
                .UseFilter()
                    .Where<Order>("Status", FilterOperator.EqualTo, "new")
                    .And()
                    .OpenParentheses()
                        .Where<Order>("Amount", FilterOperator.GreaterThan, 50)
                        .Or()
                        .Where<Order>("Amount", FilterOperator.LessThan, 5)
                    .CloseParentheses()
                .Then().Build();

            Assert.Contains("(", sql);
            Assert.Contains(")", sql);
            Assert.Contains("Or", sql);
        }

        // ── HAVING ────────────────────────────────────────────────────────────

        [Fact]
        public void Having_EmitsAfterGroupBy()
        {
            var sql = new QBuilder(parameterize: false)
                .UseSelector().SelectAggregated<Order>("Amount", "Total", "Sum").Then()
                .UseGrouper().GroupBy<Order>("UserId").Then()
                .UseHaving().Where<Order>("Amount", FilterOperator.GreaterThan, 100)
                .Then().Build();

            var normalized = Norm(sql);
            Assert.Contains("Group By", normalized);
            Assert.Contains("Having", normalized);
            var groupIdx = normalized.IndexOf("Group By", StringComparison.OrdinalIgnoreCase);
            var havingIdx = normalized.IndexOf("Having", StringComparison.OrdinalIgnoreCase);
            Assert.True(havingIdx > groupIdx, "HAVING must appear after GROUP BY");
        }

        [Fact]
        public void Having_Parameterized_AddsParam()
        {
            var result = new QBuilder(parameterize: true)
                .UseSelector().SelectAggregated<Order>("Amount", "Total", "Sum").Then()
                .UseGrouper().GroupBy<Order>("UserId").Then()
                .UseHaving().Where<Order>("Amount", FilterOperator.GreaterThan, 500)
                .Then().BuildWithParameters();

            Assert.Contains("@Amount", result.ParameterizedSql);
            Assert.Single(result.Parameters);
        }

        // ── UNION / UNION ALL / INTERSECT / EXCEPT ───────────────────────────

        [Fact]
        public void Union_EmitsUnionKeyword()
        {
            var q1 = new QBuilder(parameterize: false).UseSelector().Select<User>("Id").Then();
            var q2 = new QBuilder(parameterize: false).UseSelector().Select<User>("Id").Then();
            var sql = q1.Union(q2).Build();
            Assert.Contains("Union", sql);
        }

        [Fact]
        public void UnionAll_EmitsUnionAllKeyword()
        {
            var q1 = new QBuilder(parameterize: false).UseSelector().Select<User>("Id").Then();
            var q2 = new QBuilder(parameterize: false).UseSelector().Select<User>("Id").Then();
            var sql = q1.UnionAll(q2).Build();
            Assert.Contains("Union All", sql);
        }

        [Fact]
        public void Intersect_EmitsIntersectKeyword()
        {
            var q1 = new QBuilder(parameterize: false).UseSelector().Select<User>("Id").Then();
            var q2 = new QBuilder(parameterize: false).UseSelector().Select<User>("Id").Then();
            var sql = q1.Intersect(q2).Build();
            Assert.Contains("Intersect", sql);
        }

        [Fact]
        public void Except_EmitsExceptKeyword()
        {
            var q1 = new QBuilder(parameterize: false).UseSelector().Select<User>("Id").Then();
            var q2 = new QBuilder(parameterize: false).UseSelector().Select<User>("Id").Then();
            var sql = q1.Except(q2).Build();
            Assert.Contains("Except", sql);
        }

        [Fact]
        public void MultipleSetOps_EmitInOrder()
        {
            var q1 = new QBuilder(parameterize: false).UseSelector().Select<User>("Id").Then();
            var q2 = new QBuilder(parameterize: false).UseSelector().Select<User>("Id").Then();
            var q3 = new QBuilder(parameterize: false).UseSelector().Select<User>("Id").Then();

            var sql = Norm(q1.Union(q2).Except(q3).Build());
            var unionIdx = sql.IndexOf("Union", StringComparison.OrdinalIgnoreCase);
            var exceptIdx = sql.IndexOf("Except", StringComparison.OrdinalIgnoreCase);
            Assert.True(unionIdx < exceptIdx);
        }

        // ── CTE ───────────────────────────────────────────────────────────────

        [Fact]
        public void WithCte_EmitsCteBlock()
        {
            var cteQuery = new QBuilder(parameterize: false).UseSelector().Select<Order>("Id").Then();
            var mainQuery = new QBuilder(parameterize: false)
                .WithCte("ActiveOrders", cteQuery)
                .UseSelector().Select<User>("Id").Then();

            var sql = mainQuery.Build();
            Assert.StartsWith("With ActiveOrders", Norm(sql));
        }

        [Fact]
        public void MultipleCtes_EmitCommaSeparated()
        {
            var cte1 = new QBuilder(parameterize: false).UseSelector().Select<Order>("Id").Then();
            var cte2 = new QBuilder(parameterize: false).UseSelector().Select<User>("Id").Then();

            var mainQuery = new QBuilder(parameterize: false)
                .WithCte("CTE1", cte1)
                .WithCte("CTE2", cte2)
                .UseSelector().Select<User>("Name").Then();

            var sql = mainQuery.Build();
            Assert.Contains("CTE1", sql);
            Assert.Contains("CTE2", sql);
        }

        // ── CASE WHEN ─────────────────────────────────────────────────────────

        [Fact]
        public void CaseWhenBuilder_BuildsCorrectSql()
        {
            var caseExpr = CaseWhenBuilder.For<Order>()
                .When<Order, string>(o => o.Status, FilterOperator.EqualTo, "active").Then("Active")
                .When<Order, string>(o => o.Status, FilterOperator.EqualTo, "closed").Then("Closed")
                .Else("Unknown");

            var sql = caseExpr.Build();
            Assert.StartsWith("Case", sql);
            Assert.Contains("When", sql);
            Assert.Contains("Then 'Active'", sql);
            Assert.Contains("Else 'Unknown'", sql);
            Assert.Contains("End", sql);
        }

        [Fact]
        public void CaseWhenBuilder_NoElse_OmitsElse()
        {
            var caseExpr = CaseWhenBuilder.For<Order>()
                .When<Order, string>(o => o.Status, FilterOperator.EqualTo, "active").Then("Active");

            var sql = caseExpr.Build();
            Assert.DoesNotContain("Else", sql);
        }

        [Fact]
        public void CaseWhenBuilder_NoClauses_Throws()
        {
            var caseExpr = CaseWhenBuilder.For<Order>();
            Assert.Throws<InvalidOperationException>(() => caseExpr.Build());
        }

        [Fact]
        public void CaseWhenBuilder_Then_WithoutWhen_Throws()
        {
            var caseExpr = CaseWhenBuilder.For<Order>();
            Assert.Throws<InvalidOperationException>(() => caseExpr.Then("value"));
        }

        [Fact]
        public void SelectCaseWhen_InSelectBuilder()
        {
            var caseExpr = CaseWhenBuilder.For<Order>()
                .When<Order, string>(o => o.Status, FilterOperator.EqualTo, "active").Then("Active")
                .Else("Other");

            var sql = new QBuilder(parameterize: false)
                .UseSelector()
                    .Select<Order>("Id")
                    .SelectCaseWhen(caseExpr, alias: "StatusLabel")
                .Then().Build();

            Assert.Contains("Case When", sql);
            Assert.Contains("as StatusLabel", sql);
        }

        // ── CROSS JOIN ────────────────────────────────────────────────────────

        [Fact]
        public void CrossJoin_EmitsNoOnClause()
        {
            var sql = new QBuilder(parameterize: false)
                .UseSelector().Select<User>("Id").Then()
                .UseSelector().Select<Product>("Name").Then()
                .UseJoiner().CrossJoin<User, Product>().Then()
                .Build();

            Assert.Contains("Cross join", sql);
            Assert.DoesNotContain(" on ", sql.ToLower().Split("cross join").Last());
        }

        // ── OFFSET/FETCH paging ───────────────────────────────────────────────

        [Fact]
        public void OffsetFetchPaging_Page1()
        {
            var sql = new QBuilder(parameterize: false)
                .UseSelector().Select<User>("Name").Then()
                .UseOffsetFetchPagingBuilder<User>().PageBy(u => u.Name, page: 1, pageSize: 20)
                .Build();

            Assert.Contains("Offset 0 Rows", sql);
            Assert.Contains("Fetch Next 20 Rows Only", sql);
        }

        [Fact]
        public void OffsetFetchPaging_Page3()
        {
            var sql = new QBuilder(parameterize: false)
                .UseSelector().Select<User>("Name").Then()
                .UseOffsetFetchPagingBuilder<User>().PageBy(u => u.Name, page: 3, pageSize: 10)
                .Build();

            Assert.Contains("Offset 20 Rows", sql);
            Assert.Contains("Fetch Next 10 Rows Only", sql);
        }

        [Fact]
        public void OffsetFetchPaging_Descending()
        {
            var sql = new QBuilder(parameterize: false)
                .UseSelector().Select<User>("Name").Then()
                .UseOffsetFetchPagingBuilder<User>().PageBy(u => u.Name, page: 1, pageSize: 5, orderAscending: false)
                .Build();

            Assert.Contains("Desc", sql);
        }

        // ── Schema-qualified table names ──────────────────────────────────────

        [Fact]
        public void SchemaQualifiedTable_AliasStripsSchema()
        {
            var qb = Q.New(t => "dbo." + t.Name, parameterize: false);
            qb.UseSelector().Select<User>("Id");
            var sql = qb.Build();
            // alias must be "tUser" not "tdbo.User"
            Assert.Contains("tUser", sql);
            Assert.DoesNotContain("tdbo", sql);
        }

        // ── Multi-column ORDER BY ─────────────────────────────────────────────

        [Fact]
        public void MultiColumnOrderBy()
        {
            var sql = new QBuilder(parameterize: false)
                .UseSelector().Select<User>("Id").Then()
                .UseOrdering().OrderBy<User>("Id")
                .UseOrdering().OrderByDescending<User>("Name")
                .Build();

            Assert.Contains("tUser.Id Asc, tUser.Name Desc", Norm(sql));
        }

        // ── FilterOperator enum edge cases ───────────────────────────────────

        [Fact]
        public void FilterOperatorBetween_EnumValueDoesNotConflict()
        {
            Assert.Equal(12, (int)FilterOperator.Between);
            Assert.Equal(13, (int)FilterOperator.NotBetween);
            Assert.Equal(10, (int)FilterOperator.IsNull);
            Assert.Equal(11, (int)FilterOperator.IsNotNull);
        }
    }
}
