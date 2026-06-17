namespace Jattac.QBuilderTests.DML.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dapper;
    using Jattac.Libraries.QBuilder;
    using Jattac.Libraries.QBuilder.Config;
    using Jattac.QBuilderTests.Integration;
    using Jattac.QBuilderTests.Models;
    using Xunit;

    /// <summary>
    /// End-to-end tests for FromObject on INSERT, UPDATE, and DELETE.
    /// Each test runs against a fresh in-memory SQLite database.
    /// </summary>
    public class DmlPocoIntegrationTests : IntegrationTestBase
    {
        // Maps test POCO types to their corresponding SQLite table names.
        private static readonly QBuilderOptions DbOptions = new QBuilderOptions
        {
            TableNameResolver = t =>
            {
                var map = new Dictionary<Type, string>
                {
                    [typeof(UserWithKeyDb)]     = "User",
                    [typeof(UserWithIgnoreDb)]  = "User",
                    [typeof(UserWithAliasDb)]   = "UserAlias",
                    [typeof(UserWithMultiKeyDb)] = "UserState",
                };
                return map.TryGetValue(t, out var name) ? name : t.Name;
            }
        };

        private static QBuilder NewQ() => Q.New(DbOptions, true);

        public DmlPocoIntegrationTests()
        {
            Db.Execute(@"
                CREATE TABLE UserAlias (
                    Id        TEXT NOT NULL PRIMARY KEY,
                    user_name TEXT NOT NULL
                );
            ");
        }

        // ── INSERT ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Insert_FromObject_RowIsInsertedCorrectly()
        {
            var user = new UserWithKeyDb { Id = "poco-ins-1", Name = "Alice", IsActive = 1, DeletedAt = null };
            var q = NewQ().UseTableBoundInsert<UserWithKeyDb>().FromObject(user).BuildWithParameters();
            Db.Execute(q.ParameterizedSql, q.Parameters);

            var row = Db.QuerySingle<dynamic>("SELECT Id, Name, IsActive FROM User WHERE Id = 'poco-ins-1'");
            Assert.Equal("Alice", (string)row.Name);
            Assert.Equal(1, (int)row.IsActive);
        }

        [Fact]
        public void Insert_FromObject_QColumn_ColumnMappedCorrectly()
        {
            var user = new UserWithAliasDb { Id = "alias-ins-1", Name = "Bob" };
            var q = NewQ().UseTableBoundInsert<UserWithAliasDb>().FromObject(user).BuildWithParameters();
            Db.Execute(q.ParameterizedSql, q.Parameters);

            var name = Db.ExecuteScalar<string>("SELECT user_name FROM UserAlias WHERE Id = 'alias-ins-1'");
            Assert.Equal("Bob", name);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Update_FromObject_OnlyTargetRowChanged()
        {
            SeedUsers(
                ("poco-upd-1", "Alice", true, null),
                ("poco-upd-2", "Bob",   true, null));

            var updated = new UserWithKeyDb { Id = "poco-upd-1", Name = "Alice Updated", IsActive = 1 };
            var q = NewQ().UseTableBoundUpdate<UserWithKeyDb>().FromObject(updated).BuildWithParameters();
            Db.Execute(q.ParameterizedSql, q.Parameters);

            var names = Db.Query<string>("SELECT Name FROM User ORDER BY Name").ToList();
            Assert.Contains("Alice Updated", names);
            Assert.Contains("Bob", names);
            Assert.DoesNotContain("Alice", names);
        }

        [Fact]
        public void Update_FromObject_QIgnore_FieldRetainsOriginalValue()
        {
            // Seed with IsActive = 1 (true).
            SeedUsers(("poco-ignore-1", "Carol", true, null));

            // UserWithIgnoreDb has [QIgnore] on IsActive — the value 0 in the POCO must be ignored.
            var user = new UserWithIgnoreDb { Id = "poco-ignore-1", Name = "Carol Updated", IsActive = 0 };
            var q = NewQ().UseTableBoundUpdate<UserWithIgnoreDb>().FromObject(user).BuildWithParameters();
            Db.Execute(q.ParameterizedSql, q.Parameters);

            var row = Db.QuerySingle<dynamic>("SELECT Name, IsActive FROM User WHERE Id = 'poco-ignore-1'");
            Assert.Equal("Carol Updated", (string)row.Name);
            Assert.Equal(1, (int)row.IsActive); // Must still be 1 — IsActive was ignored.
        }

        // ── DELETE ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Delete_FromObject_OnlyTargetRowRemoved()
        {
            SeedUsers(
                ("poco-del-1", "Dave", true, null),
                ("poco-del-2", "Eve",  true, null));

            var user = new UserWithKeyDb { Id = "poco-del-1", Name = "Dave", IsActive = 1 };
            var q = NewQ().UseTableBoundDelete<UserWithKeyDb>().FromObject(user).BuildWithParameters();
            Db.Execute(q.ParameterizedSql, q.Parameters);

            var remaining = Db.Query<string>("SELECT Id FROM User").ToList();
            Assert.DoesNotContain("poco-del-1", remaining);
            Assert.Contains("poco-del-2", remaining);
        }

        [Fact]
        public void Delete_FromObject_MultiKey_OnlyTargetRowRemoved()
        {
            Db.Execute(@"
                CREATE TABLE IF NOT EXISTS UserState (
                    TenantId TEXT NOT NULL,
                    UserId   TEXT NOT NULL,
                    Name     TEXT NOT NULL,
                    PRIMARY KEY (TenantId, UserId)
                );
                INSERT INTO UserState VALUES ('t1', 'u1', 'Alice');
                INSERT INTO UserState VALUES ('t1', 'u2', 'Bob');
                INSERT INTO UserState VALUES ('t2', 'u1', 'Carol');
            ");

            var target = new UserWithMultiKeyDb { TenantId = "t1", UserId = "u1", Name = "Alice" };
            var q = NewQ().UseTableBoundDelete<UserWithMultiKeyDb>().FromObject(target).BuildWithParameters();
            Db.Execute(q.ParameterizedSql, q.Parameters);

            var count = Db.ExecuteScalar<int>("SELECT COUNT(*) FROM UserState");
            Assert.Equal(2, count);
            var remaining = Db.Query<string>("SELECT Name FROM UserState").ToList();
            Assert.DoesNotContain("Alice", remaining);
            Assert.Contains("Bob", remaining);
            Assert.Contains("Carol", remaining);
        }
    }
}
