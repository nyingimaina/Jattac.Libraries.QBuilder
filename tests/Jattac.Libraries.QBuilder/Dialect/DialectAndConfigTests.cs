namespace Jattac.QBuilderTests.Dialect
{
    using System;
    using Jattac.Libraries.QBuilder;
    using Jattac.Libraries.QBuilder.Config;
    using Jattac.Libraries.QBuilder.Enums;
    using Jattac.QBuilderTests.Models;
    using Xunit;

    /// <summary>
    /// Contract tests for the QBuilderConfig registry, IdentifierQuoter, and UsePageBy dispatch.
    /// Each test that mutates global config resets it in a finally block.
    /// </summary>
    public class DialectAndConfigTests
    {
        // ── QBuilderConfig registry ───────────────────────────────────────────────────

        [Fact]
        public void ConfigureDefault_Dialect_IsHonouredByNewInstances()
        {
            try
            {
                QBuilderConfig.ConfigureDefault(opt => opt.Dialect = Dialect.SqlServer);
                var q = Q.New(false)
                    .UseTableBoundSelector<User>()
                    .Column(u => u.Id)
                    .Then()
                    .Build();

                Assert.Contains("[tUser]", q);
                Assert.Contains("[Id]", q);
            }
            finally
            {
                QBuilderConfig.Reset();
            }
        }

        [Fact]
        public void NamedConfig_IsIsolatedFromDefault()
        {
            try
            {
                QBuilderConfig.ConfigureDefault(opt => opt.Dialect = Dialect.None);
                QBuilderConfig.Configure("mysql", opt => opt.Dialect = Dialect.MySql);

                var defaultSql = Q.New(false)
                    .UseTableBoundSelector<User>()
                    .Column(u => u.Id)
                    .Then()
                    .Build();

                var mysqlSql = Q.New("mysql", false)
                    .UseTableBoundSelector<User>()
                    .Column(u => u.Id)
                    .Then()
                    .Build();

                Assert.DoesNotContain("`", defaultSql);
                Assert.Contains("`tUser`", mysqlSql);
                Assert.Contains("`Id`", mysqlSql);
            }
            finally
            {
                QBuilderConfig.Reset();
            }
        }

        [Fact]
        public void Reset_RestoresNoDialect()
        {
            QBuilderConfig.ConfigureDefault(opt => opt.Dialect = Dialect.SqlServer);
            QBuilderConfig.Reset();

            var sql = Q.New(false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Id)
                .Then()
                .Build();

            Assert.DoesNotContain("[", sql);
        }

        [Fact]
        public void New_UnknownNamedConfig_ThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() => Q.New("nonexistent"));
        }

        // ── Identifier quoting per dialect ────────────────────────────────────────────

        [Fact]
        public void Dialect_SqlServer_QuotesBracketsAroundIdentifiers()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.SqlServer }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Name)
                .Then()
                .Build();

            Assert.Contains("[tUser].[Name]", sql);
            Assert.Contains("From [User] [tUser]", sql);
        }

        [Fact]
        public void Dialect_MsSql_IsAliasForSqlServer()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.MsSql }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Name)
                .Then()
                .Build();

            Assert.Contains("[tUser].[Name]", sql);
        }

        [Fact]
        public void Dialect_MySql_QuotesBackticks()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.MySql }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Name)
                .Then()
                .Build();

            Assert.Contains("`tUser`.`Name`", sql);
            Assert.Contains("From `User` `tUser`", sql);
        }

        [Fact]
        public void Dialect_MariaDb_QuotesBackticksLikeMySQL()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.MariaDb }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Name)
                .Then()
                .Build();

            Assert.Contains("`tUser`.`Name`", sql);
        }

        [Fact]
        public void Dialect_Sqlite_QuotesDoubleQuotes()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.Sqlite }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Name)
                .Then()
                .Build();

            Assert.Contains("\"tUser\".\"Name\"", sql);
            Assert.Contains("From \"User\" \"tUser\"", sql);
        }

        [Fact]
        public void Dialect_Postgres_QuotesDoubleQuotes()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.Postgres }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Name)
                .Then()
                .Build();

            Assert.Contains("\"tUser\".\"Name\"", sql);
        }

        [Fact]
        public void Dialect_Generic_QuotesDoubleQuotes()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.Generic }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Name)
                .Then()
                .Build();

            Assert.Contains("\"tUser\".\"Name\"", sql);
        }

        [Fact]
        public void Dialect_None_NoQuoting_BackwardCompat()
        {
            var sql = Q.New(false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Name)
                .Then()
                .Build();

            Assert.DoesNotContain("[", sql);
            Assert.DoesNotContain("`", sql);
            Assert.Contains("tUser.Name", sql);
        }

        [Fact]
        public void Dialect_SqlServer_ReservedWordTable_QuotedCorrectly()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.SqlServer, TableNameResolver = t => t.Name }, false)
                .UseTableBoundSelector<Order>()
                .Column(o => o.Id)
                .Then()
                .Build();

            // "Order" is a SQL reserved word — must be quoted
            Assert.Contains("[Order]", sql);
            Assert.Contains("[tOrder]", sql);
        }

        // ── Where clause quoting ──────────────────────────────────────────────────────

        [Fact]
        public void Dialect_SqlServer_WhereClause_QuotesIdentifiers()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.SqlServer }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Id)
                .Then()
                .UseTableBoundFilter<User>()
                .WhereEqualTo(u => u.Id, "abc")
                .Then()
                .Build();

            Assert.Contains("[tUser].[Id]", sql);
        }

        [Fact]
        public void Dialect_MySql_WhereClause_QuotesIdentifiers()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.MySql }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Id)
                .Then()
                .UseTableBoundFilter<User>()
                .WhereEqualTo(u => u.Id, "abc")
                .Then()
                .Build();

            Assert.Contains("`tUser`.`Id`", sql);
        }

        // ── DML quoting ───────────────────────────────────────────────────────────────

        [Fact]
        public void Dialect_SqlServer_Insert_QuotesTableAndColumns()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.SqlServer }, false)
                .UseTableBoundInsert<User>()
                .Value(u => u.Name, "Alice")
                .Build();

            Assert.Contains("[User]", sql);
            Assert.Contains("[Name]", sql);
        }

        [Fact]
        public void Dialect_SqlServer_Update_QuotesTableAndSetColumns()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.SqlServer }, false)
                .UseTableBoundUpdate<User>()
                .Set(u => u.Name, "Alice")
                .ForEntireTable()
                .Build();

            Assert.Contains("[User]", sql);
            Assert.Contains("[Name]", sql);
        }

        [Fact]
        public void Dialect_SqlServer_Delete_QuotesTableName()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.SqlServer }, false)
                .UseTableBoundDelete<User>()
                .ForEntireTable()
                .Build();

            Assert.Contains("[User]", sql);
        }

        // ── UsePageBy dispatch ────────────────────────────────────────────────────────

        [Fact]
        public void UsePageBy_SqlServer_EmitsRowNumber()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.SqlServer }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Id)
                .Then()
                .UsePageBy<User, string>(u => u.Name, page: 1, pageSize: 10)
                .Build();

            Assert.Contains("ROW_NUMBER()", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UsePageBy_MySql_EmitsLimit()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.MySql }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Id)
                .Then()
                .UsePageBy<User, string>(u => u.Name, page: 1, pageSize: 10)
                .Build();

            Assert.Contains("Limit", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UsePageBy_MariaDb_EmitsLimit()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.MariaDb }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Id)
                .Then()
                .UsePageBy<User, string>(u => u.Name, page: 1, pageSize: 10)
                .Build();

            Assert.Contains("Limit", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UsePageBy_None_EmitsOffsetFetch()
        {
            var sql = Q.New(false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Id)
                .Then()
                .UsePageBy<User, string>(u => u.Name, page: 1, pageSize: 10)
                .Build();

            Assert.Contains("Offset", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Fetch Next", sql, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void UsePageBy_Sqlite_EmitsOffsetFetch()
        {
            var sql = Q.New(new QBuilderOptions { Dialect = Dialect.Sqlite }, false)
                .UseTableBoundSelector<User>()
                .Column(u => u.Id)
                .Then()
                .UsePageBy<User, string>(u => u.Name, page: 1, pageSize: 10)
                .Build();

            Assert.Contains("Offset", sql, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Fetch Next", sql, StringComparison.OrdinalIgnoreCase);
        }
    }
}
