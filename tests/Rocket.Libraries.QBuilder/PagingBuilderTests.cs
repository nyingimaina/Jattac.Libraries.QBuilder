namespace Rocket.Libraries.QuriousTests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Rocket.Libraries.Qurious;
    using Rocket.Libraries.Qurious.Builders.Paging;
    using Rocket.Libraries.Qurious.Models;
    using Rocket.Libraries.QuriousTests.Models;
    using Xunit;

    public class PagingBuilderTests
    {
        [Fact]
        public void SqlServerPagingIsDoneCorrectly()
        {
            var query = new QBuilder(parameterize: false)
                .UseTableBoundSelector<User>()
                .Select(a => a.Id)
                .Then()
                .UseTableBoundSelector<WorkflowInstance>()
                .Select(a => a.UserId)
                .Then()
                .UseTableBoundJoinBuilder<User, WorkflowInstance>()
                .InnerJoin(u => u.Id, w => w.UserId)
                .UseSqlServerPagingBuilder<User>()
                .PageBy(a => a.Name, 1, 4)
                .Build();

            _ = 9;
        }

        [Theory]
        [InlineData(1, 1, 14)]
        [InlineData(2, 15, 28)]
        [InlineData(3, 29, 42)]
        [InlineData(4, 43, 56)]
        [InlineData(5, 57, 70)]
        [InlineData(6, 71, 84)]
        public void PageRangeCalculatedCorrectlyForSqlServer(uint page, int start, int end)
        {
            const ushort pageSize = 14;
            var actual = PageRangeCalculator.GetPageRange(1, page, pageSize);
            var expected = new PageRange
            {
                Start = start,
                PageSize = pageSize,
                End = end
            };

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(2, 14)]
        [InlineData(3, 28)]
        [InlineData(4, 42)]
        [InlineData(5, 56)]
        [InlineData(6, 70)]
        public void PageRangeCalculatedCorrectlyForMariaDb(uint page, int start)
        {
            const ushort pageSize = 14;
            var actual = PageRangeCalculator.GetPageRange(0, page, pageSize);
            var expected = new PageRange
            {
                Start = start,
                PageSize = pageSize
            };

            Assert.Equal(expected.Start, actual.Start);
            Assert.Equal(expected.PageSize, pageSize);
        }
    }
}