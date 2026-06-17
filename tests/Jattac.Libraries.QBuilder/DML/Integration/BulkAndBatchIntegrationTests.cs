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
    /// End-to-end tests for bulk INSERT (FromObjects) and batched DML (QBatch).
    /// Each test runs against a fresh in-memory SQLite database.
    /// </summary>
    public class BulkAndBatchIntegrationTests : IntegrationTestBase
    {
        private static readonly QBuilderOptions DbOptions = new QBuilderOptions
        {
            TableNameResolver = t =>
            {
                var map = new Dictionary<Type, string>
                {
                    [typeof(UserWithKeyDb)]      = "User",
                    [typeof(UserWithMultiKeyDb)] = "UserState",
                    [typeof(SimpleProductDb)]    = "Product",
                };
                return map.TryGetValue(t, out var name) ? name : t.Name;
            }
        };

        private static QBuilder NewQ() => Q.New(DbOptions, true);

        public BulkAndBatchIntegrationTests()
        {
            Db.Execute(@"
                CREATE TABLE IF NOT EXISTS UserState (
                    TenantId TEXT NOT NULL,
                    UserId   TEXT NOT NULL,
                    Name     TEXT NOT NULL,
                    PRIMARY KEY (TenantId, UserId)
                );
            ");
        }

        // ── Bulk INSERT ───────────────────────────────────────────────────────────────

        [Fact]
        public void FromObjects_InsertThreeRows_AllRetrievable()
        {
            var users = new List<UserWithKeyDb>
            {
                new UserWithKeyDb { Id = "bulk-1", Name = "Alice", IsActive = 1 },
                new UserWithKeyDb { Id = "bulk-2", Name = "Bob",   IsActive = 0 },
                new UserWithKeyDb { Id = "bulk-3", Name = "Carol", IsActive = 1 },
            };

            var q = NewQ().UseTableBoundInsert<UserWithKeyDb>().FromObjects(users).BuildWithParameters();
            Db.Execute(q.ParameterizedSql, q.Parameters);

            var rows = Db.Query<dynamic>("SELECT Id, Name, IsActive FROM User ORDER BY Id").ToList();
            Assert.Equal(3, rows.Count);
            Assert.Equal("Alice", (string)rows[0].Name);
            Assert.Equal("Bob",   (string)rows[1].Name);
            Assert.Equal("Carol", (string)rows[2].Name);
        }

        [Fact]
        public void FromObjects_InsertEmpty_ThrowsBeforeHittingDb()
        {
            var ex = Record.Exception(() =>
                NewQ()
                    .UseTableBoundInsert<UserWithKeyDb>()
                    .FromObjects(new List<UserWithKeyDb>())
                    .BuildWithParameters());

            Assert.NotNull(ex);
            var count = Db.ExecuteScalar<int>("SELECT COUNT(*) FROM User");
            Assert.Equal(0, count);
        }

        // ── Batched DML ───────────────────────────────────────────────────────────────

        [Fact]
        public void QBatch_UpdateTwoRows_BothUpdated()
        {
            SeedUsers(
                ("batch-upd-1", "Alice", true, null),
                ("batch-upd-2", "Bob",   true, null));

            var u1 = new UserWithKeyDb { Id = "batch-upd-1", Name = "Alice Updated", IsActive = 1 };
            var u2 = new UserWithKeyDb { Id = "batch-upd-2", Name = "Bob Updated",   IsActive = 0 };

            var queries = new[]
            {
                NewQ().UseTableBoundUpdate<UserWithKeyDb>().FromObject(u1).BuildWithParameters(),
                NewQ().UseTableBoundUpdate<UserWithKeyDb>().FromObject(u2).BuildWithParameters(),
            };

            var batch = QBatch.New().AddRange(queries).Build();
            Db.Execute(batch.ParameterizedSql, batch.Parameters);

            var names = Db.Query<string>("SELECT Name FROM User ORDER BY Name").ToList();
            Assert.Contains("Alice Updated", names);
            Assert.Contains("Bob Updated",   names);
            Assert.DoesNotContain("Alice", names);
            Assert.DoesNotContain("Bob",   names);
        }

        [Fact]
        public void QBatch_DeleteByCompositeKey_CorrectRowsRemoved()
        {
            Db.Execute(@"
                INSERT INTO UserState VALUES ('t1', 'u1', 'Alice');
                INSERT INTO UserState VALUES ('t1', 'u2', 'Bob');
                INSERT INTO UserState VALUES ('t2', 'u1', 'Carol');
            ");

            var toDelete = new[]
            {
                new UserWithMultiKeyDb { TenantId = "t1", UserId = "u1", Name = "Alice" },
                new UserWithMultiKeyDb { TenantId = "t1", UserId = "u2", Name = "Bob" },
            };

            var queries = toDelete.Select(k =>
                NewQ().UseTableBoundDelete<UserWithMultiKeyDb>().FromObject(k).BuildWithParameters());

            var batch = QBatch.New().AddRange(queries).Build();
            Db.Execute(batch.ParameterizedSql, batch.Parameters);

            var remaining = Db.ExecuteScalar<int>("SELECT COUNT(*) FROM UserState");
            Assert.Equal(1, remaining);
            var name = Db.ExecuteScalar<string>("SELECT Name FROM UserState");
            Assert.Equal("Carol", name);
        }

        [Fact]
        public void QBatch_InsertThenUpdate_BothApplied()
        {
            SeedUsers(("batch-mix-exist", "Existing", true, null));

            var insertQ = NewQ().UseTableBoundInsert<UserWithKeyDb>()
                .FromObject(new UserWithKeyDb { Id = "batch-mix-new", Name = "New", IsActive = 1 })
                .BuildWithParameters();

            var updateQ = NewQ().UseTableBoundUpdate<UserWithKeyDb>()
                .FromObject(new UserWithKeyDb { Id = "batch-mix-exist", Name = "Updated", IsActive = 0 })
                .BuildWithParameters();

            var batch = QBatch.New().Add(insertQ).Add(updateQ).Build();
            Db.Execute(batch.ParameterizedSql, batch.Parameters);

            var count = Db.ExecuteScalar<int>("SELECT COUNT(*) FROM User");
            Assert.Equal(2, count);

            var existingName = Db.ExecuteScalar<string>(
                "SELECT Name FROM User WHERE Id = 'batch-mix-exist'");
            Assert.Equal("Updated", existingName);

            var newExists = Db.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM User WHERE Id = 'batch-mix-new'");
            Assert.Equal(1, newExists);
        }

        [Fact]
        public void QBatch_ThreeUpdates_ParamCollisionHandledTransparently()
        {
            SeedUsers(
                ("coll-1", "Alpha", true, null),
                ("coll-2", "Beta",  true, null),
                ("coll-3", "Gamma", true, null));

            var updates = new[]
            {
                new UserWithKeyDb { Id = "coll-1", Name = "Alpha-v2", IsActive = 1 },
                new UserWithKeyDb { Id = "coll-2", Name = "Beta-v2",  IsActive = 0 },
                new UserWithKeyDb { Id = "coll-3", Name = "Gamma-v2", IsActive = 1 },
            };

            var queries = updates.Select(u =>
                NewQ().UseTableBoundUpdate<UserWithKeyDb>().FromObject(u).BuildWithParameters());

            var batch = QBatch.New().AddRange(queries).Build();
            Db.Execute(batch.ParameterizedSql, batch.Parameters);

            var names = Db.Query<string>("SELECT Name FROM User ORDER BY Name").ToList();
            Assert.Contains("Alpha-v2", names);
            Assert.Contains("Beta-v2",  names);
            Assert.Contains("Gamma-v2", names);
        }
    }
}
