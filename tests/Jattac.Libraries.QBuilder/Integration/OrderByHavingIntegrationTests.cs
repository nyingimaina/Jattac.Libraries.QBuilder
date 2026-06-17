namespace Jattac.QBuilderTests.Integration
{
    using System.Linq;
    using Dapper;
    using Jattac.Libraries.QBuilder;
    using Jattac.QBuilderTests.Models;
    using Xunit;

    /// <summary>
    /// Integration tests for TableBoundOrderByBuilder and TableBoundHavingBuilder against in-memory SQLite.
    /// </summary>
    public class OrderByHavingIntegrationTests : IntegrationTestBase
    {
        public OrderByHavingIntegrationTests()
        {
            SeedUsers(
                ("1", "Charlie", true, null),
                ("2", "Alice", true, null),
                ("3", "Bob", false, null),
                ("4", "Dave", false, null)
            );
        }

        // ── ORDER BY ─────────────────────────────────────────────────────────

        [Fact]
        public void Ascending_ReturnsCorrectCountAndFirstElement()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundOrderBy<User>().Ascending(u => u.Name)
                .Then().Build();

            var names = Db.Query<string>(sql).ToList();
            Assert.Equal(4, names.Count);
            Assert.Equal("Alice", names[0]); // SQLite preserves subquery ordering
        }

        [Fact]
        public void Descending_PutsLastAlphabeticalNameFirst()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundOrderBy<User>().Descending(u => u.Name)
                .Then().Build();

            var names = Db.Query<string>(sql).ToList();
            Assert.Equal(4, names.Count);
            Assert.Equal("Dave", names[0]);
        }

        [Fact]
        public void ThenAscending_MultiColumnOrderExecutesWithoutError()
        {
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundOrderBy<User>()
                    .Ascending(u => u.IsActive)
                    .ThenAscending(u => u.Name)
                .Then().Build();

            var names = Db.Query<string>(sql).ToList();
            Assert.Equal(4, names.Count);
            // active=0 rows first (Bob, Dave), then active=1 rows (Alice, Charlie)
            Assert.Equal("Bob", names[0]);
        }

        // ── HAVING ───────────────────────────────────────────────────────────

        [Fact]
        public void HavingEqualTo_FiltersGroupByResult()
        {
            // Group by IsActive, keep only the active group (IsActive=1)
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.IsActive).Then()
                .UseTableBoundGrouper<User>().GroupBy(u => u.IsActive)
                .UseTableBoundHaving<User>().HavingEqualTo(u => u.IsActive, 1)
                .Then().Build();

            var activeValues = Db.Query<int>(sql).ToList();
            Assert.Single(activeValues);
            Assert.Equal(1, activeValues[0]);
        }

        [Fact]
        public void HavingGreaterThan_FiltersGroupsAboveThreshold()
        {
            // Group by IsActive, keep only groups where IsActive > 0
            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.IsActive).Then()
                .UseTableBoundGrouper<User>().GroupBy(u => u.IsActive)
                .UseTableBoundHaving<User>().HavingGreaterThan(u => u.IsActive, 0)
                .Then().Build();

            var activeValues = Db.Query<int>(sql).ToList();
            Assert.Single(activeValues); // only the IsActive=1 group remains
            Assert.Equal(1, activeValues[0]);
        }
    }
}
