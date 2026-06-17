namespace Jattac.QBuilderTests.Integration
{
    using System.Linq;
    using Dapper;
    using Jattac.Libraries.QBuilder;
    using Jattac.QBuilderTests.Models;
    using Xunit;

    /// <summary>
    /// Integration tests for .If(), WhereBetween, WhereExists/WhereNotExists executed against in-memory SQLite.
    /// </summary>
    public class AdvancedWhereIntegrationTests : IntegrationTestBase
    {
        public AdvancedWhereIntegrationTests()
        {
            SeedUsers(
                ("1", "Alice", true, null),
                ("2", "Bob", true, null),
                ("3", "Charlie", false, "2024-01-01"),
                ("4", "Dave", false, null)
            );
        }

        // ── .If() ─────────────────────────────────────────────────────────────

        [Fact]
        public void If_ConditionTrue_AppliesFilter()
        {
            var nameFilter = "Alice";

            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>()
                    .If(!string.IsNullOrEmpty(nameFilter),
                        fb => fb.WhereEqualTo(u => u.Name, nameFilter))
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Single(names);
            Assert.Equal("Alice", names[0]);
        }

        [Fact]
        public void If_ConditionFalse_ReturnsAllRows()
        {
            var nameFilter = string.Empty;

            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>()
                    .If(!string.IsNullOrEmpty(nameFilter),
                        fb => fb.WhereEqualTo(u => u.Name, nameFilter))
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Equal(4, names.Count);
        }

        // ── WhereBetween ──────────────────────────────────────────────────────

        [Fact]
        public void WhereBetween_ReturnsRowsInAlphabeticRange()
        {
            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>().WhereBetween(u => u.Name, "Alice", "Charlie")
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Equal(3, names.Count); // Alice, Bob, Charlie — Dave > Charlie
            Assert.DoesNotContain("Dave", names);
        }

        [Fact]
        public void WhereNotBetween_ExcludesRowsInRange()
        {
            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>().WhereNotBetween(u => u.Name, "Alice", "Charlie")
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Single(names); // only Dave is outside Alice..Charlie range
            Assert.Equal("Dave", names[0]);
        }

        // ── WhereExists / WhereNotExists ──────────────────────────────────────

        [Fact]
        public void WhereExists_ReturnsAllRowsWhenSubqueryHasMatch()
        {
            // Return all users IF there exists any user named Alice
            var subQuery = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                .UseTableBoundFilter<User>().WhereEqualTo(u => u.Name, "Alice").Then();

            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>().WhereExists(subQuery)
                .Then().Build();

            var names = Db.Query<string>(sql).ToList();
            Assert.Equal(4, names.Count); // EXISTS (Alice) is true → all rows returned
        }

        [Fact]
        public void WhereNotExists_ReturnsEmptyWhenSubqueryHasMatch()
        {
            // Return all users IF there is no user named Alice — but Alice exists, so empty
            var subQuery = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Id).Then()
                .UseTableBoundFilter<User>().WhereEqualTo(u => u.Name, "Alice").Then();

            var sql = Q.New(parameterize: false)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>().WhereNotExists(subQuery)
                .Then().Build();

            var names = Db.Query<string>(sql).ToList();
            Assert.Empty(names); // NOT EXISTS (Alice) is false → no rows
        }
    }
}
