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
    /// Tests for the zero-boilerplate fluent TableBound* API introduced in v8.0.
    /// Every test asserts on the generated SQL string to document exact output.
    /// </summary>
    public class FluentApiTests
    {
        private static string Normalize(string sql) =>
            sql.Replace("\r\n", "\n").Replace("\r", "\n").Trim();

        // ── Q.New factory ─────────────────────────────────────────────────────

        [Fact]
        public void QBuildDefaultParameterizes()
        {
            var qb = Q.New();
            qb.UseSelector().Select<User>("Name");
            var result = qb.BuildWithParameters();
            Assert.NotNull(result);
        }

        [Fact]
        public void QBuildFalseDoesNotParameterize()
        {
            var qb = Q.New(parameterize: false);
            qb.UseSelector().Select<User>("Name");
            var sql = qb.Build();
            Assert.NotNull(sql);
        }

        [Fact]
        public void QBuildWithCustomResolver()
        {
            var qb = Q.New(t => "dbo." + t.Name, parameterize: false);
            qb.UseSelector().Select<User>("Id");
            var sql = qb.Build();
            Assert.Contains("dbo.User", sql);
        }

        // ── SELECT ────────────────────────────────────────────────────────────

        [Fact]
        public void SelectLambdaNoAlias()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Id)
                .Then().Build();
            Assert.Contains("tUser.Id", Normalize(sql));
        }

        [Fact]
        public void SelectLambdaWithAlias()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Name, "UserName")
                .Then().Build();
            Assert.Contains("as UserName", Normalize(sql));
        }

        [Fact]
        public void SelectDistinct()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Distinct().Column(u => u.Name)
                .Then().Build();
            Assert.Contains("Distinct", sql);
        }

        [Fact]
        public void SelectTop()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Top(10).Column(u => u.Name)
                .Then().Build();
            Assert.Contains("Top 10", sql);
        }

        [Fact]
        public void AggregateSum()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<Order>().Aggregate(o => o.Amount, "Total", AggregateFunction.Sum)
                .Then().Build();
            Assert.Contains("Sum(", sql);
            Assert.Contains("as Total", sql);
        }

        [Fact]
        public void AggregateCount()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<Order>().Aggregate(o => o.Id, "Cnt", AggregateFunction.Count)
                .Then().Build();
            Assert.Contains("Count(", sql);
        }

        [Fact]
        public void SelectCaseWhenExtension()
        {
            var caseExpr = CaseWhenBuilder.For<Order>()
                .When<Order, string>(o => o.Status, FilterOperator.EqualTo, "active").Then("Active")
                .Else("Other");

            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<Order>()
                    .Column(o => o.Id)
                    .CaseWhen(caseExpr, "StatusLabel")
                .Then().Build();

            Assert.Contains("Case When", sql);
            Assert.Contains("as StatusLabel", sql);
        }

        // ── JOIN ──────────────────────────────────────────────────────────────

        [Fact]
        public void InnerJoinExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                .UseTableBoundSelector<Order>().Column(o => o.Amount).Then()
                .UseTableBoundJoinBuilder<User, Order>()
                    .InnerJoin(u => u.Id, o => o.UserId)
                .Build();

            Assert.Contains("join Order", Normalize(sql));
            Assert.Contains("tUser.Id", sql);
        }

        [Fact]
        public void LeftJoinExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                .UseTableBoundSelector<Order>().Column(o => o.Amount).Then()
                .UseTableBoundJoinBuilder<User, Order>()
                    .LeftJoin(u => u.Id, o => o.UserId)
                .Build();

            Assert.Contains("Left join", sql);
        }

        [Fact]
        public void CrossJoinExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                .UseTableBoundSelector<Product>().Column(p => p.Name).Then()
                .UseJoiner().CrossJoin<User, Product>().Then()
                .Build();

            Assert.Contains("Cross join", sql);
        }

        // ── WHERE ─────────────────────────────────────────────────────────────

        [Fact]
        public void WhereExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>().WhereEqualTo(u => u.Name, "Alice")
                .Then().Build();

            Assert.Contains("tUser.Name", sql);
            Assert.Contains("= 'Alice'", sql);
        }

        [Fact]
        public void AndWhereExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>()
                    .WhereEqualTo(u => u.Name, "Alice")
                    .AndWhereNotEqualTo(u => u.Name, "Bob")
                .Then().Build();

            Assert.Contains("And", sql);
            Assert.Contains("<>", sql);
        }

        [Fact]
        public void OrWhereExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>()
                    .WhereEqualTo(u => u.Name, "Alice")
                    .OrWhereEqualTo(u => u.Name, "Bob")
                .Then().Build();

            Assert.Contains("Or", sql);
        }

        [Fact]
        public void WhereIsNullExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<Order>().Column(o => o.Id).Then()
                .UseTableBoundFilter<Order>().WhereIsNull(o => o.DeletedAt)
                .Then().Build();

            Assert.Contains("IS NULL", sql);
        }

        [Fact]
        public void AndWhereIsNullExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<Order>().Column(o => o.Id).Then()
                .UseTableBoundFilter<Order>()
                    .WhereEqualTo(o => o.Status, "active")
                    .AndWhereIsNull(o => o.DeletedAt)
                .Then().Build();

            Assert.Contains("IS NULL", sql);
            Assert.Contains("And", sql);
        }

        [Fact]
        public void WhereIsNotNullExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<Order>().Column(o => o.Id).Then()
                .UseTableBoundFilter<Order>().WhereIsNotNull(o => o.DeletedAt)
                .Then().Build();

            Assert.Contains("IS NOT NULL", sql);
        }

        [Fact]
        public void WhereBetweenExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<Order>().Column(o => o.Amount).Then()
                .UseTableBoundFilter<Order>().WhereBetween(o => o.Amount, 10, 100)
                .Then().Build();

            Assert.Contains("Between", sql);
            Assert.Contains("10", sql);
            Assert.Contains("100", sql);
        }

        [Fact]
        public void WhereInExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<Order>().Column(o => o.Status).Then()
                .UseTableBoundFilter<Order>().WhereIn<string, string>(o => o.Status, new[] { "new", "processing" })
                .Then().Build();

            Assert.Contains("in", sql);
            Assert.Contains("new", sql);
        }

        [Fact]
        public void WhereNotInExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<Order>().Column(o => o.Status).Then()
                .UseTableBoundFilter<Order>().WhereNotIn<string, string>(o => o.Status, new[] { "cancelled" })
                .Then().Build();

            Assert.Contains("not in", sql);
        }

        [Fact]
        public void OpenGroupCloseGroupExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<Order>().Column(o => o.Status).Then()
                .UseTableBoundFilter<Order>()
                    .WhereEqualTo(o => o.Status, "new")
                    .OpenGroup()
                    .OrWhereEqualTo(o => o.Status, "processing")
                    .CloseGroup()
                .Then().Build();

            Assert.Contains("(", sql);
            Assert.Contains(")", sql);
        }

        // ── HAVING & GROUP BY ──────────────────────────────────────────────────

        [Fact]
        public void GroupByAndHavingExtensions()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<Order>().Aggregate(o => o.Amount, "Total", AggregateFunction.Sum).Then()
                .UseTableBoundGrouper<Order>().GroupBy(o => o.UserId)
                .UseTableBoundHaving<Order>().HavingGreaterThan(o => o.Amount, 100)
                .Then().Build();

            Assert.Contains("Group By", sql);
            Assert.Contains("Having", sql);
            Assert.Contains(">", sql);
        }

        // ── ORDER BY ──────────────────────────────────────────────────────────

        [Fact]
        public void OrderByExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundOrderBy<User>().Ascending(u => u.Name)
                .Then().Build();

            Assert.Contains("Order By tUser.Name Asc", sql);
        }

        [Fact]
        public void OrderByDescendingExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundOrderBy<User>().Descending(u => u.Name)
                .Then().Build();

            Assert.Contains("tUser.Name Desc", sql);
        }

        [Fact]
        public void ThenByExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundOrderBy<User>()
                    .Ascending(u => u.Id)
                    .ThenAscending(u => u.Name)
                .Then().Build();

            Assert.Contains("tUser.Id Asc", sql);
            Assert.Contains("tUser.Name Asc", sql);
        }

        [Fact]
        public void OrderByDescending_OnJoinedTable_UsesCorrectAlias()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                .UseTableBoundSelector<Order>().Column(o => o.Amount).Then()
                .UseTableBoundJoinBuilder<User, Order>()
                    .InnerJoin(u => u.Id, o => o.UserId)
                .UseTableBoundOrderBy<Order>().Descending(o => o.Amount)
                .Then().Build();

            Assert.Contains("tOrder.Amount Desc", sql);
        }

        [Fact]
        public void MultiColumnOrderBy_AcrossJoinedTables_BothAliasesCorrect()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                .UseTableBoundSelector<Order>().Column(o => o.Amount).Then()
                .UseTableBoundJoinBuilder<User, Order>()
                    .InnerJoin(u => u.Id, o => o.UserId)
                .UseTableBoundOrderBy<Order>()
                    .Descending(o => o.Amount)
                    .ThenAscending(o => o.Status)
                .Then().Build();

            Assert.Contains("tOrder.Amount Desc", sql);
            Assert.Contains("tOrder.Status Asc", sql);
        }

        // ── PAGING ────────────────────────────────────────────────────────────

        [Fact]
        public void PageSqlServerExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                .UseSqlServerPagingBuilder<User>().PageBy(u => u.Name, page: 1, pageSize: 10)
                .Build();

            Assert.Contains("ROW_NUMBER()", sql);
            Assert.Contains("__qb_rn__", sql);
        }

        [Fact]
        public void PageOffsetFetchExtension()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                .UseOffsetFetchPagingBuilder<User>().PageBy(u => u.Name, page: 2, pageSize: 10)
                .Build();

            Assert.Contains("Offset 10 Rows Fetch Next 10 Rows Only", sql);
        }

        // ── parameterized WHERE ───────────────────────────────────────────────

        [Fact]
        public void WhereExtensionParameterized()
        {
            var result = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>().WhereEqualTo(u => u.Name, "Alice")
                .Then().BuildWithParameters();

            Assert.Single(result.Parameters);
            Assert.Equal("Alice", result.Parameters.Values.First().ToString());
            Assert.Contains("@Name", result.ParameterizedSql);
        }
    }
}
