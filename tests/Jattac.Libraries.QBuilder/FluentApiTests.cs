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
    /// Tests for the zero-boilerplate fluent extension API introduced in v7.0.
    /// Every test asserts on the generated SQL string to document exact output.
    /// </summary>
    public class FluentApiTests
    {
        // ── helpers ────────────────────────────────────────────────────────────
        private static string Normalize(string sql) =>
            sql.Replace("\r\n", "\n").Replace("\r", "\n").Trim();

        // ── Q.Build factory ───────────────────────────────────────────────────

        [Fact]
        public void QBuildDefaultParameterizes()
        {
            var qb = Q.Build();
            qb.UseSelector().Select<User>("Name");
            var result = qb.BuildWithParameters();
            Assert.NotNull(result);
        }

        [Fact]
        public void QBuildFalseDoesNotParameterize()
        {
            var qb = Q.Build(parameterize: false);
            qb.UseSelector().Select<User>("Name");
            var sql = qb.Build();
            Assert.NotNull(sql);
        }

        [Fact]
        public void QBuildWithCustomResolver()
        {
            var qb = Q.Build(t => "dbo." + t.Name, parameterize: false);
            qb.UseSelector().Select<User>("Id");
            var sql = qb.Build();
            Assert.Contains("dbo.User", sql);
        }

        // ── SELECT extensions ─────────────────────────────────────────────────

        [Fact]
        public void SelectLambdaNoAlias()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, Guid>(u => u.Id)
                .Build();
            Assert.Contains("tUser.Id", Normalize(sql));
        }

        [Fact]
        public void SelectLambdaWithAlias()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, string>(u => u.Name, alias: "UserName")
                .Build();
            Assert.Contains("as UserName", Normalize(sql));
        }

        [Fact]
        public void SelectDistinct()
        {
            var sql = Q.Build(parameterize: false)
                .Distinct()
                .Select<User, string>(u => u.Name)
                .Build();
            Assert.Contains("Distinct", sql);
        }

        [Fact]
        public void SelectTop()
        {
            var sql = Q.Build(parameterize: false)
                .Top(10)
                .Select<User, string>(u => u.Name)
                .Build();
            Assert.Contains("Top 10", sql);
        }

        [Fact]
        public void AggregateSum()
        {
            var sql = Q.Build(parameterize: false)
                .Aggregate<Order, decimal>(o => o.Amount, "Total", AggregateFunction.Sum)
                .Build();
            Assert.Contains("Sum(", sql);
            Assert.Contains("as Total", sql);
        }

        [Fact]
        public void AggregateCount()
        {
            var sql = Q.Build(parameterize: false)
                .Aggregate<Order, Guid>(o => o.Id, "Cnt", AggregateFunction.Count)
                .Build();
            Assert.Contains("Count(", sql);
        }

        [Fact]
        public void SelectCaseWhenExtension()
        {
            var caseExpr = CaseWhenBuilder.For<Order>()
                .When<Order, string>(o => o.Status, FilterOperator.EqualTo, "active").Then("Active")
                .Else("Other");

            var sql = Q.Build(parameterize: false)
                .Select<Order, Guid>(o => o.Id)
                .SelectCaseWhen(caseExpr, alias: "StatusLabel")
                .Build();

            Assert.Contains("Case When", sql);
            Assert.Contains("as StatusLabel", sql);
        }

        // ── JOIN extensions ───────────────────────────────────────────────────

        [Fact]
        public void InnerJoinExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, Guid>(u => u.Id)
                .Select<Order, decimal>(o => o.Amount)
                .InnerJoin<User, Order, Guid, Guid>(u => u.Id, o => o.UserId)
                .Build();

            Assert.Contains("join Order", Normalize(sql));
            Assert.Contains("tUser.Id", sql);
        }

        [Fact]
        public void LeftJoinExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, Guid>(u => u.Id)
                .Select<Order, decimal>(o => o.Amount)
                .LeftJoin<User, Order, Guid, Guid>(u => u.Id, o => o.UserId)
                .Build();

            Assert.Contains("Left join", sql);
        }

        [Fact]
        public void CrossJoinExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, Guid>(u => u.Id)
                .Select<Product, string>(p => p.Name)
                .CrossJoin<User, Product>()
                .Build();

            Assert.Contains("Cross join", sql);
        }

        // ── WHERE extensions ──────────────────────────────────────────────────

        [Fact]
        public void WhereExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, string>(u => u.Name)
                .Where<User, string>(u => u.Name, FilterOperator.EqualTo, "Alice")
                .Build();

            Assert.Contains("tUser.Name", sql);
            Assert.Contains("= 'Alice'", sql);
        }

        [Fact]
        public void AndWhereExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, string>(u => u.Name)
                .Where<User, string>(u => u.Name, FilterOperator.EqualTo, "Alice")
                .AndWhere<User, string>(u => u.Name, FilterOperator.NotEqualTo, "Bob")
                .Build();

            Assert.Contains("And", sql);
            Assert.Contains("<>", sql);
        }

        [Fact]
        public void OrWhereExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, string>(u => u.Name)
                .Where<User, string>(u => u.Name, FilterOperator.EqualTo, "Alice")
                .OrWhere<User, string>(u => u.Name, FilterOperator.EqualTo, "Bob")
                .Build();

            Assert.Contains("Or", sql);
        }

        [Fact]
        public void WhereIsNullExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<Order, Guid>(o => o.Id)
                .WhereIsNull<Order, DateTime?>(o => o.DeletedAt)
                .Build();

            Assert.Contains("IS NULL", sql);
        }

        [Fact]
        public void AndWhereIsNullExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<Order, Guid>(o => o.Id)
                .Where<Order, string>(o => o.Status, FilterOperator.EqualTo, "active")
                .AndWhereIsNull<Order, DateTime?>(o => o.DeletedAt)
                .Build();

            Assert.Contains("IS NULL", sql);
            Assert.Contains("And", sql);
        }

        [Fact]
        public void WhereIsNotNullExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<Order, Guid>(o => o.Id)
                .WhereIsNotNull<Order, DateTime?>(o => o.DeletedAt)
                .Build();

            Assert.Contains("IS NOT NULL", sql);
        }

        [Fact]
        public void WhereBetweenExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<Order, decimal>(o => o.Amount)
                .WhereBetween<Order, decimal>(o => o.Amount, 10, 100)
                .Build();

            Assert.Contains("Between", sql);
            Assert.Contains("10", sql);
            Assert.Contains("100", sql);
        }

        [Fact]
        public void WhereInExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<Order, string>(o => o.Status)
                .WhereIn<Order, string, string>(o => o.Status, new[] { "new", "processing" })
                .Build();

            Assert.Contains("in", sql);
            Assert.Contains("new", sql);
        }

        [Fact]
        public void WhereNotInExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<Order, string>(o => o.Status)
                .WhereNotIn<Order, string, string>(o => o.Status, new[] { "cancelled" })
                .Build();

            Assert.Contains("not in", sql);
        }

        [Fact]
        public void OpenGroupCloseGroupExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<Order, string>(o => o.Status)
                .Where<Order, string>(o => o.Status, FilterOperator.EqualTo, "new")
                .OpenGroup()
                .OrWhere<Order, string>(o => o.Status, FilterOperator.EqualTo, "processing")
                .CloseGroup()
                .Build();

            Assert.Contains("(", sql);
            Assert.Contains(")", sql);
        }

        // ── HAVING & GROUP BY ──────────────────────────────────────────────────

        [Fact]
        public void GroupByAndHavingExtensions()
        {
            var sql = Q.Build(parameterize: false)
                .Aggregate<Order, decimal>(o => o.Amount, "Total", AggregateFunction.Sum)
                .GroupBy<Order, Guid>(o => o.UserId)
                .Having<Order, decimal>(o => o.Amount, FilterOperator.GreaterThan, 100)
                .Build();

            Assert.Contains("Group By", sql);
            Assert.Contains("Having", sql);
            Assert.Contains(">", sql);
        }

        // ── ORDER BY ──────────────────────────────────────────────────────────

        [Fact]
        public void OrderByExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, string>(u => u.Name)
                .OrderBy<User, string>(u => u.Name)
                .Build();

            Assert.Contains("Order By tUser.Name Asc", sql);
        }

        [Fact]
        public void OrderByDescendingExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, string>(u => u.Name)
                .OrderByDescending<User, string>(u => u.Name)
                .Build();

            Assert.Contains("tUser.Name Desc", sql);
        }

        [Fact]
        public void ThenByExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, string>(u => u.Name)
                .OrderBy<User, Guid>(u => u.Id)
                .ThenBy<User, string>(u => u.Name)
                .Build();

            Assert.Contains("tUser.Id Asc", sql);
            Assert.Contains("tUser.Name Asc", sql);
        }

        // ── PAGING ────────────────────────────────────────────────────────────

        [Fact]
        public void PageSqlServerExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, Guid>(u => u.Id)
                .PageSqlServer<User, string>(u => u.Name, page: 1, pageSize: 10)
                .Build();

            Assert.Contains("ROW_NUMBER()", sql);
            Assert.Contains("__qb_rn__", sql);
        }

        [Fact]
        public void PageOffsetFetchExtension()
        {
            var sql = Q.Build(parameterize: false)
                .Select<User, Guid>(u => u.Id)
                .PageOffsetFetch<User, string>(u => u.Name, page: 2, pageSize: 10)
                .Build();

            Assert.Contains("Offset 10 Rows Fetch Next 10 Rows Only", sql);
        }

        // ── parameterized WHERE via extensions ────────────────────────────────

        [Fact]
        public void WhereExtensionParameterized()
        {
            var result = Q.Build(parameterize: true)
                .Select<User, string>(u => u.Name)
                .Where<User, string>(u => u.Name, FilterOperator.EqualTo, "Alice")
                .BuildWithParameters();

            Assert.Single(result.Parameters);
            Assert.Equal("Alice", result.Parameters.Values.First().ToString());
            Assert.Contains("@Name", result.ParameterizedSql);
        }
    }
}
