namespace Jattac.QBuilderTests
{
    using System;
    using Jattac.Libraries.QBuilder;
    using Jattac.QBuilderTests.Models;
    using Xunit;

    public class GuardValidationTests
    {
        // ── QBuilder ─────────────────────────────────────────────────────────────────

        [Fact]
        public void Build_WithNoSelectsAndNoJoins_ThrowsNoTableQueued()
        {
            var ex = Record.Exception(() => Q.New(false).Build());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("no tables queued", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Build_CalledTwice_ThrowsAlreadyBuilt()
        {
            var qb = Q.New(false).UseTableBoundSelector<User>().Column(u => u.Id).Then();
            qb.Build();

            var ex = Record.Exception(() => qb.Build());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("already been called", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ── SelectBuilder ────────────────────────────────────────────────────────────

        [Fact]
        public void Build_WithJoinButNoSelectFields_ThrowsNoFieldsQueued()
        {
            var ex = Record.Exception(() =>
                Q.New(false)
                    .UseTableBoundJoinBuilder<User, Order>().InnerJoin(u => u.Id, o => o.UserId)
                    .Build());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("no fields queued", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Build_SelectingFromTableNotInJoins_ThrowsTableNotQueued()
        {
            // Select from Product, but only User↔Order is joined
            var ex = Record.Exception(() =>
                Q.New(false)
                    .UseTableBoundSelector<Product>().Column(p => p.Name).Then()
                    .UseTableBoundJoinBuilder<User, Order>().InnerJoin(u => u.Id, o => o.UserId)
                    .Build());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("not been queued as a datasource", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ── ConditionMaker — null value ───────────────────────────────────────────────

        [Fact]
        public void WhereEqualTo_NullValue_ThrowsNoValueProvided()
        {
            var ex = Record.Exception(() =>
                Q.New(false)
                    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                    .UseTableBoundFilter<User>().WhereEqualTo(u => u.Name, null).Then()
                    .Build());

            Assert.IsType<ArgumentNullException>(ex);
            Assert.Contains("no value was provided for filter", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ── ConditionMaker — BETWEEN nulls ────────────────────────────────────────────

        [Fact]
        public void WhereBetween_NullFrom_ThrowsNoFromValue()
        {
            var ex = Record.Exception(() =>
                Q.New(false)
                    .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                    .UseTableBoundFilter<User>().WhereBetween(u => u.Name, null, "Z").Then()
                    .Build());

            Assert.IsType<ArgumentNullException>(ex);
            Assert.Contains("'from' value", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void WhereBetween_NullTo_ThrowsNoToValue()
        {
            var ex = Record.Exception(() =>
                Q.New(false)
                    .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                    .UseTableBoundFilter<User>().WhereBetween(u => u.Name, "A", null).Then()
                    .Build());

            Assert.IsType<ArgumentNullException>(ex);
            Assert.Contains("'to' value", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ── WhereBuilder — parentheses ────────────────────────────────────────────────

        [Fact]
        public void CloseGroup_WithNoOpenGroup_ThrowsNothingToClose()
        {
            var ex = Record.Exception(() =>
                Q.New(false)
                    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                    .UseTableBoundFilter<User>().CloseGroup().Then()
                    .Build());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("no open parentheses", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Build_WithUnclosedOpenGroup_ThrowsUnclosedParentheses()
        {
            var ex = Record.Exception(() =>
                Q.New(false)
                    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                    .UseTableBoundFilter<User>()
                        .WhereEqualTo(u => u.Name, "Alice")
                        .OpenGroup()
                        .OrWhereEqualTo(u => u.Name, "Bob")
                        // intentionally no CloseGroup()
                    .Then()
                    .Build());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("unclosed parentheses", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ── SqlServerPagingBuilder ────────────────────────────────────────────────────

        [Fact]
        public void SqlServerPaging_PageZero_ThrowsPageInvalid()
        {
            var ex = Record.Exception(() =>
                Q.New(false)
                    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                    .UseSqlServerPagingBuilder<User>().PageBy(u => u.Name, page: 0, pageSize: 10));

            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Contains("page", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("greater than or equal to 1", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void SqlServerPaging_PageSizeZero_ThrowsPageSizeInvalid()
        {
            var ex = Record.Exception(() =>
                Q.New(false)
                    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                    .UseSqlServerPagingBuilder<User>().PageBy(u => u.Name, page: 1, pageSize: 0));

            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Contains("page size", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ── OffsetFetchPagingBuilder ──────────────────────────────────────────────────

        [Fact]
        public void OffsetFetchPaging_PageZero_ThrowsPageInvalid()
        {
            var ex = Record.Exception(() =>
                Q.New(false)
                    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                    .UseOffsetFetchPagingBuilder<User>().PageBy(u => u.Name, page: 0, pageSize: 10));

            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Contains("page", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(">= 1", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void OffsetFetchPaging_PageSizeZero_ThrowsPageSizeInvalid()
        {
            var ex = Record.Exception(() =>
                Q.New(false)
                    .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                    .UseOffsetFetchPagingBuilder<User>().PageBy(u => u.Name, page: 1, pageSize: 0));

            Assert.IsType<ArgumentOutOfRangeException>(ex);
            Assert.Contains("page size", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
    }
}
