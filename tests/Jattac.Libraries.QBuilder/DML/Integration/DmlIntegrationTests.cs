namespace Jattac.QBuilderTests.DML.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Dapper;
    using Jattac.Libraries.QBuilder;
    using Jattac.QBuilderTests.Integration;
    using Jattac.QBuilderTests.Models;
    using Xunit;

    /// <summary>
    /// End-to-end DML tests that execute generated SQL against an in-memory SQLite database.
    /// Each test seeds fresh data, runs the DML, and verifies the result via a SELECT.
    /// </summary>
    public class DmlIntegrationTests : IntegrationTestBase
    {
        // ── INSERT ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Insert_SingleRow_RowIsPersistedAndRetrievable()
        {
            var id = "u-insert-1";
            var q = Q.New(true)
                .UseTableBoundInsert<User>()
                .Value(u => u.Id, id)
                .Value(u => u.Name, "Alice")
                .Value(u => u.IsActive, 1)
                .Value(u => u.DeletedAt, null)
                .BuildWithParameters();

            Db.Execute(q.ParameterizedSql, q.Parameters);

            var user = Db.QuerySingle<dynamic>("SELECT Id, Name FROM User WHERE Id = @id", new { id });
            Assert.Equal("Alice", (string)user.Name);
        }

        [Fact]
        public void Insert_LoggingDelegate_ReceivesSql()
        {
            string logged = null;
            var id = "u-insert-log";
            var q = Q.New(true)
                .UseTableBoundInsert<User>()
                .Value(u => u.Id, id)
                .Value(u => u.Name, "Bob")
                .Value(u => u.IsActive, 1)
                .Value(u => u.DeletedAt, null)
                .BuildWithParameters(sql => logged = sql);

            Assert.NotNull(logged);
            Assert.Contains("Insert Into", logged, StringComparison.OrdinalIgnoreCase);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Update_ByPrimaryKey_ChangesOnlyTargetRow()
        {
            SeedUsers(
                ("u-upd-1", "Alice", true, null),
                ("u-upd-2", "Bob", true, null));

            var q = Q.New(true)
                .UseTableBoundUpdate<User>()
                .Set(u => u.Name, "Alice Updated")
                .WhereEqualTo(u => u.Id, "u-upd-1")
                .BuildWithParameters();

            Db.Execute(q.ParameterizedSql, q.Parameters);

            var names = Db.Query<string>("SELECT Name FROM User ORDER BY Name").ToList();
            Assert.Contains("Alice Updated", names);
            Assert.Contains("Bob", names);
            Assert.DoesNotContain("Alice", names);
        }

        [Fact]
        public void Update_MultipleSetColumns_AllColumnsChanged()
        {
            SeedUsers(("u-upd-multi", "Old Name", true, null));

            var q = Q.New(true)
                .UseTableBoundUpdate<User>()
                .Set(u => u.Name, "New Name")
                .Set(u => u.IsActive, 0)
                .WhereEqualTo(u => u.Id, "u-upd-multi")
                .BuildWithParameters();

            Db.Execute(q.ParameterizedSql, q.Parameters);

            var user = Db.QuerySingle<dynamic>("SELECT Name, IsActive FROM User WHERE Id = 'u-upd-multi'");
            Assert.Equal("New Name", (string)user.Name);
            Assert.Equal(0, (int)user.IsActive);
        }

        [Fact]
        public void Update_ForEntireTable_UpdatesAllRows()
        {
            SeedUsers(
                ("u-all-1", "Alpha", true, null),
                ("u-all-2", "Beta", true, null));

            var q = Q.New(true)
                .UseTableBoundUpdate<User>()
                .Set(u => u.IsActive, 0)
                .ForEntireTable()
                .BuildWithParameters();

            Db.Execute(q.ParameterizedSql, q.Parameters);

            var activeCount = Db.ExecuteScalar<int>("SELECT COUNT(*) FROM User WHERE IsActive = 1");
            Assert.Equal(0, activeCount);
        }

        [Fact]
        public void Update_LoggingDelegate_ReceivesSql()
        {
            SeedUsers(("u-upd-log", "Log Test", true, null));
            string logged = null;

            var q = Q.New(true)
                .UseTableBoundUpdate<User>()
                .Set(u => u.Name, "Updated")
                .WhereEqualTo(u => u.Id, "u-upd-log")
                .BuildWithParameters(sql => logged = sql);

            Assert.NotNull(logged);
            Assert.Contains("Update", logged, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Set", logged, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Where", logged, StringComparison.OrdinalIgnoreCase);
        }

        // ── DELETE ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Delete_ByPrimaryKey_RemovesOnlyTargetRow()
        {
            SeedUsers(
                ("u-del-1", "Alice", true, null),
                ("u-del-2", "Bob", true, null));

            var q = Q.New(true)
                .UseTableBoundDelete<User>()
                .WhereEqualTo(u => u.Id, "u-del-1")
                .BuildWithParameters();

            Db.Execute(q.ParameterizedSql, q.Parameters);

            var remaining = Db.Query<string>("SELECT Id FROM User").ToList();
            Assert.DoesNotContain("u-del-1", remaining);
            Assert.Contains("u-del-2", remaining);
        }

        [Fact]
        public void Delete_WithAndWhereConditions_FiltersCorrectly()
        {
            SeedUsers(
                ("u-del-a", "Inactive 1", false, null),
                ("u-del-b", "Inactive 2", false, null),
                ("u-del-c", "Active", true, null));

            var q = Q.New(true)
                .UseTableBoundDelete<User>()
                .WhereEqualTo(u => u.IsActive, 0)
                .BuildWithParameters();

            Db.Execute(q.ParameterizedSql, q.Parameters);

            var count = Db.ExecuteScalar<int>("SELECT COUNT(*) FROM User");
            Assert.Equal(1, count);
        }

        [Fact]
        public void Delete_ForEntireTable_RemovesAllRows()
        {
            SeedUsers(
                ("u-all-del-1", "One", true, null),
                ("u-all-del-2", "Two", true, null));

            var q = Q.New(true)
                .UseTableBoundDelete<User>()
                .ForEntireTable()
                .BuildWithParameters();

            Db.Execute(q.ParameterizedSql, q.Parameters);

            var count = Db.ExecuteScalar<int>("SELECT COUNT(*) FROM User");
            Assert.Equal(0, count);
        }

        [Fact]
        public void Delete_LoggingDelegate_ReceivesSql()
        {
            SeedUsers(("u-del-log", "Log", true, null));
            string logged = null;

            var q = Q.New(true)
                .UseTableBoundDelete<User>()
                .WhereEqualTo(u => u.Id, "u-del-log")
                .BuildWithParameters(sql => logged = sql);

            Assert.NotNull(logged);
            Assert.Contains("Delete From", logged, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Where", logged, StringComparison.OrdinalIgnoreCase);
        }
    }
}
