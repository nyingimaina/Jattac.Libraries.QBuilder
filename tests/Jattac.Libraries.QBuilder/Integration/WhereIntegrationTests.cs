namespace Jattac.QBuilderTests.Integration
{
    using System.Collections.Generic;
    using System.Linq;
    using Dapper;
    using Jattac.Libraries.QBuilder;
    using Jattac.QBuilderTests.Models;
    using Xunit;

    /// <summary>
    /// Integration tests for And*/Or* predicate families executed against in-memory SQLite.
    /// Each test class instance gets a fresh database — schema is re-created per instance by IntegrationTestBase.
    /// </summary>
    public class WhereIntegrationTests : IntegrationTestBase
    {
        public WhereIntegrationTests()
        {
            SeedUsers(
                ("1", "Alice", true, null),
                ("2", "Bob", true, null),
                ("3", "Charlie", false, "2024-01-01"),
                ("4", "Alice", false, null)
            );
        }

        [Fact]
        public void WhereEqualTo_ReturnsMatchingRows()
        {
            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>().WhereEqualTo(u => u.Name, "Alice")
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Equal(2, names.Count);
            Assert.All(names, n => Assert.Equal("Alice", n));
        }

        [Fact]
        public void AndWhereEqualTo_CombinesTwoConditions()
        {
            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>()
                    .WhereEqualTo(u => u.Name, "Alice")
                    .AndWhereEqualTo(u => u.IsActive, true)
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Single(names);
            Assert.Equal("Alice", names[0]);
        }

        [Fact]
        public void OrWhereEqualTo_ReturnsEitherMatch()
        {
            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>()
                    .WhereEqualTo(u => u.Name, "Alice")
                    .OrWhereEqualTo(u => u.Name, "Bob")
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Equal(3, names.Count); // 2 Alices + 1 Bob
        }

        [Fact]
        public void WhereIsNull_ReturnsOnlyNullDeletedAt()
        {
            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>().WhereIsNull(u => u.DeletedAt)
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Equal(3, names.Count); // Alice, Bob, Alice (Charlie has DeletedAt set)
            Assert.DoesNotContain("Charlie", names);
        }

        [Fact]
        public void WhereIsNotNull_ReturnsOnlySoftDeletedRows()
        {
            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>().WhereIsNotNull(u => u.DeletedAt)
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Single(names);
            Assert.Equal("Charlie", names[0]);
        }

        [Fact]
        public void WhereIn_ReturnsOnlyNamedRows()
        {
            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>()
                    .WhereIn<string, string>(u => u.Name, new List<string> { "Alice", "Bob" })
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Equal(3, names.Count); // 2 Alices + 1 Bob
            Assert.DoesNotContain("Charlie", names);
        }

        [Fact]
        public void WhereNotIn_ExcludesListedNames()
        {
            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>()
                    .WhereNotIn<string, string>(u => u.Name, new List<string> { "Alice" })
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Equal(2, names.Count); // Bob + Charlie
            Assert.DoesNotContain("Alice", names);
        }

        [Fact]
        public void AndWhereIsNull_FiltersActiveAndNotDeleted()
        {
            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>()
                    .WhereEqualTo(u => u.IsActive, true)
                    .AndWhereIsNull(u => u.DeletedAt)
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Equal(2, names.Count); // active Alice + Bob
        }

        [Fact]
        public void OrWhereIsNull_ReturnsInactiveOrNotDeleted()
        {
            // IsActive = false OR DeletedAt IS NULL
            var built = Q.New(parameterize: true)
                .UseTableBoundSelector<User>().Column(u => u.Name).Then()
                .UseTableBoundFilter<User>()
                    .WhereEqualTo(u => u.IsActive, false)
                    .OrWhereIsNull(u => u.DeletedAt)
                .Then().BuildWithParameters();

            var names = Db.Query<string>(built.ParameterizedSql, built.Parameters).ToList();
            Assert.Equal(4, names.Count); // all rows match at least one condition
        }
    }
}
