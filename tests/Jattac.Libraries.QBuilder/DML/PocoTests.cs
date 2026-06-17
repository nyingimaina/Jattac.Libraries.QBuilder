namespace Jattac.QBuilderTests.DML
{
    using System;
    using Jattac.Libraries.QBuilder;
    using Jattac.Libraries.QBuilder.Config;
    using Jattac.Libraries.QBuilder.Enums;
    using Jattac.QBuilderTests.Models;
    using Xunit;

    /// <summary>
    /// Unit tests for FromObject on INSERT, UPDATE, and DELETE builders.
    /// No database involved — asserts on generated SQL strings and parameter dictionaries.
    /// </summary>
    public class PocoTests
    {
        // ── Helpers ───────────────────────────────────────────────────────────────────

        private static QBuilder NewQ() => Q.New(true);

        private static QBuilder NewQWithDialect(Dialect dialect)
        {
            var opts = new QBuilderOptions { Dialect = dialect };
            return Q.New(opts, true);
        }

        // ── INSERT ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Insert_FromObject_AllProperties_AreIncludedInSql()
        {
            var user = new UserWithKey { Id = Guid.NewGuid(), Name = "Alice", IsActive = true };
            var q = NewQ().UseTableBoundInsert<UserWithKey>().FromObject(user).BuildWithParameters();

            Assert.Contains("Id", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Name", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("IsActive", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("DeletedAt", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(4, q.Parameters.Count);
        }

        [Fact]
        public void Insert_FromObject_QIgnore_PropertyIsExcluded()
        {
            var user = new UserWithIgnore { Id = Guid.NewGuid(), Name = "Alice", IsActive = true };
            var q = NewQ().UseTableBoundInsert<UserWithIgnore>().FromObject(user).BuildWithParameters();

            Assert.DoesNotContain("IsActive", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("IsActive", string.Join(",", q.Parameters.Keys));
            Assert.Equal(2, q.Parameters.Count);
        }

        [Fact]
        public void Insert_FromObject_QColumn_SqlUsesAliasName()
        {
            var user = new UserWithColumnAlias { Id = Guid.NewGuid(), Name = "Alice" };
            var q = NewQ().UseTableBoundInsert<UserWithColumnAlias>().FromObject(user).BuildWithParameters();

            Assert.Contains("user_name", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(" Name", q.ParameterizedSql);
            // Parameter is seeded from the C# property name "Name", not the alias "user_name".
            Assert.True(q.Parameters.ContainsKey("@Name0"), "Parameter should be @Name0, not @user_name0");
            Assert.False(q.Parameters.ContainsKey("@user_name0"));
        }

        [Fact]
        public void Insert_FromObject_QKey_IsIncludedAsNormalColumn()
        {
            var user = new UserWithKey { Id = Guid.NewGuid(), Name = "Alice" };
            var q = NewQ().UseTableBoundInsert<UserWithKey>().FromObject(user).BuildWithParameters();

            Assert.Contains("Id", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Insert_FromObject_AnonymousObject_AllPropertiesIncluded()
        {
            var anon = new { Name = "Alice", IsActive = true };
            var q = NewQ().UseTableBoundInsert<UserWithKey>().FromObject(anon).BuildWithParameters();

            Assert.Contains("Name", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("IsActive", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(2, q.Parameters.Count);
        }

        [Fact]
        public void Insert_FromObject_NullObject_ThrowsArgumentNullException()
        {
            var ex = Record.Exception(() =>
                NewQ().UseTableBoundInsert<UserWithKey>().FromObject<UserWithKey>(null));

            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void Insert_FromObject_DialectSqlServer_ColumnNamesAreBracketed()
        {
            var user = new UserWithKey { Id = Guid.NewGuid(), Name = "Alice" };
            var q = NewQWithDialect(Dialect.SqlServer)
                .UseTableBoundInsert<UserWithKey>()
                .FromObject(user)
                .BuildWithParameters();

            Assert.Contains("[Id]", q.ParameterizedSql);
            Assert.Contains("[Name]", q.ParameterizedSql);
        }

        [Fact]
        public void Insert_FromObject_DialectMySql_ColumnNamesAreBackticked()
        {
            var user = new UserWithKey { Id = Guid.NewGuid(), Name = "Alice" };
            var q = NewQWithDialect(Dialect.MySql)
                .UseTableBoundInsert<UserWithKey>()
                .FromObject(user)
                .BuildWithParameters();

            Assert.Contains("`Id`", q.ParameterizedSql);
            Assert.Contains("`Name`", q.ParameterizedSql);
        }

        [Fact]
        public void Insert_FromObject_ThenManualValue_BothAppear()
        {
            var user = new UserWithColumnAlias { Id = Guid.NewGuid(), Name = "Alice" };
            // UserWithColumnAlias has Id + Name (aliased). Value() adds an expression-based column.
            // Using UserWithKey as the table type so u => u.IsActive compiles.
            var q = NewQ()
                .UseTableBoundInsert<UserWithKey>()
                .FromObject(user)
                .Value(u => u.IsActive, true)
                .BuildWithParameters();

            Assert.Contains("user_name", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("IsActive", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Insert_FromObject_NonParameterized_ProducesLiterals()
        {
            var user = new UserWithColumnAlias { Id = Guid.NewGuid(), Name = "Hello World" };
            var sql = Q.New(false)
                .UseTableBoundInsert<UserWithColumnAlias>()
                .FromObject(user)
                .Build();

            Assert.Contains("'Hello World'", sql);
            Assert.DoesNotContain("@", sql);
        }

        [Fact]
        public void Insert_FromObject_ProducesIdenticalSqlToFluentValue()
        {
            var id = Guid.NewGuid();
            var fluent = NewQ().UseTableBoundInsert<UserWithKey>()
                .Value(u => u.Id, id)
                .Value(u => u.Name, "Alice")
                .Value(u => u.IsActive, true)
                .Value(u => u.DeletedAt, (DateTime?)null)
                .BuildWithParameters();

            var poco = NewQ().UseTableBoundInsert<UserWithKey>()
                .FromObject(new UserWithKey { Id = id, Name = "Alice", IsActive = true, DeletedAt = null })
                .BuildWithParameters();

            Assert.Equal(fluent.ParameterizedSql, poco.ParameterizedSql);
            Assert.Equal(fluent.Parameters.Count, poco.Parameters.Count);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Update_FromObject_KeyGoesToWhere_RestGoesToSet()
        {
            var user = new UserWithKey { Id = Guid.NewGuid(), Name = "Alice", IsActive = true };
            var q = NewQ().UseTableBoundUpdate<UserWithKey>().FromObject(user).BuildWithParameters();

            Assert.Contains("Set", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Where", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            // Id should be in WHERE, not in SET
            var setIndex = q.ParameterizedSql.IndexOf("Set", StringComparison.OrdinalIgnoreCase);
            var whereIndex = q.ParameterizedSql.IndexOf("Where", StringComparison.OrdinalIgnoreCase);
            Assert.True(whereIndex > setIndex);
            var setClause = q.ParameterizedSql.Substring(setIndex, whereIndex - setIndex);
            Assert.DoesNotContain("Id", setClause, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Update_FromObject_MultiKey_BothKeysInWhere()
        {
            var user = new UserWithMultiKey { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Alice" };
            var q = NewQ().UseTableBoundUpdate<UserWithMultiKey>().FromObject(user).BuildWithParameters();

            var whereIndex = q.ParameterizedSql.IndexOf("Where", StringComparison.OrdinalIgnoreCase);
            var whereClause = q.ParameterizedSql.Substring(whereIndex);
            Assert.Contains("TenantId", whereClause, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("UserId", whereClause, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Update_FromObject_QIgnore_AbsentFromSetAndWhere()
        {
            var user = new UserWithIgnore { Id = Guid.NewGuid(), Name = "Alice", IsActive = true };
            var q = NewQ().UseTableBoundUpdate<UserWithIgnore>().FromObject(user).BuildWithParameters();

            Assert.DoesNotContain("IsActive", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Update_FromObject_QColumn_SqlUsesAlias()
        {
            var user = new UserWithColumnAlias { Id = Guid.NewGuid(), Name = "Alice" };
            var q = NewQ().UseTableBoundUpdate<UserWithColumnAlias>().FromObject(user).BuildWithParameters();

            var setIndex = q.ParameterizedSql.IndexOf("Set", StringComparison.OrdinalIgnoreCase);
            var whereIndex = q.ParameterizedSql.IndexOf("Where", StringComparison.OrdinalIgnoreCase);
            var setClause = q.ParameterizedSql.Substring(setIndex, whereIndex - setIndex);
            Assert.Contains("user_name", setClause, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Update_FromObject_AnonymousObject_AllPropsGoToSet()
        {
            var anon = new { Name = "Alice" };
            // Anonymous object: no [QKey], so all props go to SET, HasWhere remains false.
            // We must use WhereEqualTo to avoid the no-WHERE guard; here we just check the SET part.
            var builder = NewQ()
                .UseTableBoundUpdate<UserWithKey>()
                .FromObject(anon)
                .WhereEqualTo(u => u.Id, Guid.NewGuid());

            var q = builder.BuildWithParameters();
            var setIndex = q.ParameterizedSql.IndexOf("Set", StringComparison.OrdinalIgnoreCase);
            var whereIndex = q.ParameterizedSql.IndexOf("Where", StringComparison.OrdinalIgnoreCase);
            var setClause = q.ParameterizedSql.Substring(setIndex, whereIndex - setIndex);
            Assert.Contains("Name", setClause, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Update_FromObject_AnonymousObject_NoWhereChained_ThrowsAtBuild()
        {
            var ex = Record.Exception(() =>
                NewQ()
                    .UseTableBoundUpdate<UserWithKey>()
                    .FromObject(new { Name = "Alice" })
                    .BuildWithParameters());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("ForEntireTable", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Update_FromObject_AnonymousObject_WithFluentWhere_Succeeds()
        {
            var id = Guid.NewGuid();
            var ex = Record.Exception(() =>
                NewQ()
                    .UseTableBoundUpdate<UserWithKey>()
                    .FromObject(new { Name = "Alice" })
                    .WhereEqualTo(u => u.Id, id)
                    .BuildWithParameters());

            Assert.Null(ex);
        }

        [Fact]
        public void Update_FromObject_NoKeyOnNamedType_ThrowsImmediately()
        {
            var ex = Record.Exception(() =>
                NewQ()
                    .UseTableBoundUpdate<UserNoKey>()
                    .FromObject(new UserNoKey { Id = Guid.NewGuid(), Name = "Alice" }));

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("QKey", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Update_FromObject_NullObject_ThrowsArgumentNullException()
        {
            var ex = Record.Exception(() =>
                NewQ().UseTableBoundUpdate<UserWithKey>().FromObject<UserWithKey>(null));

            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void Update_FromObject_QIgnoreWinsOverQKey_ThrowsWhenOnlyKeyIsIgnored()
        {
            // UserWithKeyAndIgnore has [QKey][QIgnore] on Id — Id is excluded entirely.
            // No key remains → no WHERE → should throw with the no-key message.
            var ex = Record.Exception(() =>
                NewQ()
                    .UseTableBoundUpdate<UserWithKeyAndIgnore>()
                    .FromObject(new UserWithKeyAndIgnore { Id = Guid.NewGuid(), Name = "Alice" }));

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("QKey", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        // ── DELETE ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Delete_FromObject_KeyGoesToWhere()
        {
            var user = new UserWithKey { Id = Guid.NewGuid(), Name = "Alice" };
            var q = NewQ().UseTableBoundDelete<UserWithKey>().FromObject(user).BuildWithParameters();

            Assert.Contains("Where", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Id", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Delete_FromObject_MultiKey_BothKeysInWhere()
        {
            var user = new UserWithMultiKey { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid(), Name = "Alice" };
            var q = NewQ().UseTableBoundDelete<UserWithMultiKey>().FromObject(user).BuildWithParameters();

            Assert.Contains("TenantId", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("UserId", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Delete_FromObject_NonKeyPropertiesAreDiscarded()
        {
            var user = new UserWithKey { Id = Guid.NewGuid(), Name = "Alice", IsActive = true };
            var q = NewQ().UseTableBoundDelete<UserWithKey>().FromObject(user).BuildWithParameters();

            // Only the WHERE clause is present — Name and IsActive should not appear.
            Assert.DoesNotContain("Name", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("IsActive", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            // Only Id param should be in parameters.
            Assert.Single(q.Parameters);
        }

        [Fact]
        public void Delete_FromObject_QIgnore_KeyIgnored_ThrowsIfNoOtherKey()
        {
            var ex = Record.Exception(() =>
                NewQ()
                    .UseTableBoundDelete<UserWithKeyAndIgnore>()
                    .FromObject(new UserWithKeyAndIgnore { Id = Guid.NewGuid(), Name = "Alice" }));

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("QKey", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Delete_FromObject_NoKeyOnNamedType_ThrowsImmediately()
        {
            var ex = Record.Exception(() =>
                NewQ()
                    .UseTableBoundDelete<UserNoKey>()
                    .FromObject(new UserNoKey { Id = Guid.NewGuid(), Name = "Alice" }));

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("QKey", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Delete_FromObject_AnonymousObject_NoWhere_ThrowsAtBuild()
        {
            var ex = Record.Exception(() =>
                NewQ()
                    .UseTableBoundDelete<UserWithKey>()
                    .FromObject(new { Name = "Alice" })
                    .BuildWithParameters());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("ForEntireTable", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Delete_FromObject_NullObject_ThrowsArgumentNullException()
        {
            var ex = Record.Exception(() =>
                NewQ().UseTableBoundDelete<UserWithKey>().FromObject<UserWithKey>(null));

            Assert.IsType<ArgumentNullException>(ex);
        }

        // ── Shorthand methods ──────────────────────────────────────────────────────────

        [Fact]
        public void InsertFrom_Shorthand_ProducesSameResultAsBuilderChain()
        {
            var user = new UserWithKey { Id = Guid.NewGuid(), Name = "Bob", IsActive = false };

            var direct = NewQ().UseTableBoundInsert<UserWithKey>().FromObject(user).BuildWithParameters();
            var shorthand = NewQ().InsertFrom<UserWithKey, UserWithKey>(user).BuildWithParameters();

            Assert.Equal(direct.ParameterizedSql, shorthand.ParameterizedSql);
        }

        [Fact]
        public void UpdateFrom_Shorthand_ProducesSameResultAsBuilderChain()
        {
            var user = new UserWithKey { Id = Guid.NewGuid(), Name = "Bob", IsActive = false };

            var direct = NewQ().UseTableBoundUpdate<UserWithKey>().FromObject(user).BuildWithParameters();
            var shorthand = NewQ().UpdateFrom<UserWithKey, UserWithKey>(user).BuildWithParameters();

            Assert.Equal(direct.ParameterizedSql, shorthand.ParameterizedSql);
        }

        [Fact]
        public void DeleteFrom_Shorthand_ProducesSameResultAsBuilderChain()
        {
            var user = new UserWithKey { Id = Guid.NewGuid(), Name = "Bob" };

            var direct = NewQ().UseTableBoundDelete<UserWithKey>().FromObject(user).BuildWithParameters();
            var shorthand = NewQ().DeleteFrom<UserWithKey, UserWithKey>(user).BuildWithParameters();

            Assert.Equal(direct.ParameterizedSql, shorthand.ParameterizedSql);
        }
    }
}
