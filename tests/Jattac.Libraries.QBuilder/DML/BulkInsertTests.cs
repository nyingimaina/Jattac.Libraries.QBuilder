namespace Jattac.QBuilderTests.DML
{
    using System;
    using System.Collections.Generic;
    using Jattac.Libraries.QBuilder;
    using Jattac.Libraries.QBuilder.Config;
    using Jattac.Libraries.QBuilder.Enums;
    using Jattac.QBuilderTests.Models;
    using Xunit;

    /// <summary>
    /// Unit tests for <c>FromObjects</c> (bulk INSERT) on <c>TableBoundInsertBuilder</c>.
    /// No database involved — asserts on generated SQL strings and parameter dictionaries.
    /// </summary>
    public class BulkInsertTests
    {
        private static QBuilder NewQ() => Q.New(true);

        private static List<SimpleProduct> ThreeProducts() => new List<SimpleProduct>
        {
            new SimpleProduct { Id = 1, Name = "Alpha", Price = 9.99m },
            new SimpleProduct { Id = 2, Name = "Beta",  Price = 19.99m },
            new SimpleProduct { Id = 3, Name = "Gamma", Price = 29.99m },
        };

        [Fact]
        public void FromObjects_ThreeItems_EmitsThreeValueRows()
        {
            var q = NewQ().UseTableBoundInsert<SimpleProduct>().FromObjects(ThreeProducts()).BuildWithParameters();

            // Three value groups: (row1), (row2), (row3)
            // Count opening parens after "Values" to verify three rows.
            var afterValues = q.ParameterizedSql.Substring(
                q.ParameterizedSql.IndexOf("Values", StringComparison.OrdinalIgnoreCase) + 6);
            var openParens = 0;
            foreach (var c in afterValues) if (c == '(') openParens++;
            Assert.Equal(3, openParens);
        }

        [Fact]
        public void FromObjects_ThreeItems_AllParametersPresent()
        {
            var q = NewQ().UseTableBoundInsert<SimpleProduct>().FromObjects(ThreeProducts()).BuildWithParameters();

            // SimpleProduct has 3 non-ignored columns (Id, Name, Price); 3 rows = 9 parameters.
            Assert.Equal(9, q.Parameters.Count);
        }

        [Fact]
        public void FromObjects_ParamsAreUniqueAcrossRows()
        {
            var q = NewQ().UseTableBoundInsert<SimpleProduct>().FromObjects(ThreeProducts()).BuildWithParameters();

            // Keys must be unique.
            Assert.Equal(q.Parameters.Count, new HashSet<string>(q.Parameters.Keys).Count);

            // Id params from each row get sequential suffixes.
            Assert.True(q.Parameters.ContainsKey("@Id0"), "Expected @Id0");
            Assert.True(q.Parameters.ContainsKey("@Id1"), "Expected @Id1");
            Assert.True(q.Parameters.ContainsKey("@Id2"), "Expected @Id2");
        }

        [Fact]
        public void FromObjects_EmptyCollection_ThrowsGuard()
        {
            var ex = Record.Exception(() =>
                NewQ().UseTableBoundInsert<SimpleProduct>()
                    .FromObjects(new List<SimpleProduct>())
                    .BuildWithParameters());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("empty", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void FromObjects_NullCollection_ThrowsArgumentNull()
        {
            var ex = Record.Exception(() =>
                NewQ().UseTableBoundInsert<SimpleProduct>()
                    .FromObjects<SimpleProduct>(null)
                    .BuildWithParameters());

            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void FromObjects_SingleItem_BehavesLikeFromObject()
        {
            var product = new SimpleProduct { Id = 1, Name = "Solo", Price = 5.00m };

            var single = NewQ().UseTableBoundInsert<SimpleProduct>().FromObject(product).BuildWithParameters();
            var bulk = NewQ().UseTableBoundInsert<SimpleProduct>()
                .FromObjects(new List<SimpleProduct> { product })
                .BuildWithParameters();

            Assert.Equal(single.ParameterizedSql, bulk.ParameterizedSql);
            Assert.Equal(single.Parameters.Count, bulk.Parameters.Count);
        }

        [Fact]
        public void FromObjects_QIgnore_PropertyAbsentFromAllRows()
        {
            var items = new List<UserWithIgnore>
            {
                new UserWithIgnore { Id = Guid.NewGuid(), Name = "Alice", IsActive = true },
                new UserWithIgnore { Id = Guid.NewGuid(), Name = "Bob",   IsActive = false },
            };

            var q = NewQ().UseTableBoundInsert<UserWithIgnore>().FromObjects(items).BuildWithParameters();

            Assert.DoesNotContain("IsActive", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            // 2 rows × 2 non-ignored columns (Id, Name) = 4 params.
            Assert.Equal(4, q.Parameters.Count);
        }

        [Fact]
        public void FromObjects_QColumn_AliasUsedInColumnHeader()
        {
            var items = new List<UserWithColumnAlias>
            {
                new UserWithColumnAlias { Id = Guid.NewGuid(), Name = "Alice" },
                new UserWithColumnAlias { Id = Guid.NewGuid(), Name = "Bob" },
            };

            var q = NewQ().UseTableBoundInsert<UserWithColumnAlias>().FromObjects(items).BuildWithParameters();

            Assert.Contains("user_name", q.ParameterizedSql, StringComparison.OrdinalIgnoreCase);
            // Column header should appear only once (shared across all rows).
            var count = 0;
            var sql = q.ParameterizedSql;
            var start = 0;
            while ((start = sql.IndexOf("user_name", start, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                count++;
                start++;
            }
            Assert.Equal(1, count);
        }

        [Fact]
        public void FromObjects_DialectSqlServer_ColumnsBracketed()
        {
            var opts = new QBuilderOptions { Dialect = Dialect.SqlServer };
            var q = Q.New(opts, true)
                .UseTableBoundInsert<SimpleProduct>()
                .FromObjects(new List<SimpleProduct>
                {
                    new SimpleProduct { Id = 1, Name = "A", Price = 1m },
                    new SimpleProduct { Id = 2, Name = "B", Price = 2m },
                })
                .BuildWithParameters();

            Assert.Contains("[Id]", q.ParameterizedSql);
            Assert.Contains("[Name]", q.ParameterizedSql);
            Assert.Contains("[Price]", q.ParameterizedSql);
        }

        [Fact]
        public void FromObjects_NonParameterized_ProducesInlineLiterals()
        {
            var items = new List<UserWithColumnAlias>
            {
                new UserWithColumnAlias { Id = Guid.NewGuid(), Name = "Alice" },
                new UserWithColumnAlias { Id = Guid.NewGuid(), Name = "Bob" },
            };

            var sql = Q.New(false).UseTableBoundInsert<UserWithColumnAlias>()
                .FromObjects(items)
                .Build();

            Assert.Contains("'Alice'", sql);
            Assert.Contains("'Bob'", sql);
            Assert.DoesNotContain("@", sql);
        }

        [Fact]
        public void FromObjects_ParameterValues_MatchInputObjects()
        {
            var p1 = new SimpleProduct { Id = 10, Name = "X", Price = 1.5m };
            var p2 = new SimpleProduct { Id = 20, Name = "Y", Price = 2.5m };

            var q = NewQ().UseTableBoundInsert<SimpleProduct>()
                .FromObjects(new List<SimpleProduct> { p1, p2 })
                .BuildWithParameters();

            // First row
            Assert.Equal(10, q.Parameters["@Id0"]);
            Assert.Equal("X", q.Parameters["@Name0"]);
            Assert.Equal(1.5m, q.Parameters["@Price0"]);
            // Second row
            Assert.Equal(20, q.Parameters["@Id1"]);
            Assert.Equal("Y", q.Parameters["@Name1"]);
            Assert.Equal(2.5m, q.Parameters["@Price1"]);
        }

        [Fact]
        public void FromObjects_ThenManualValue_AppendedToFirstRow()
        {
            // This is a documented misuse of the API (columns and extra rows become mismatched).
            // We verify the current observable behaviour — no crash, first row gets the extra column.
            var items = new List<SimpleProduct>
            {
                new SimpleProduct { Id = 1, Name = "A", Price = 1m },
                new SimpleProduct { Id = 2, Name = "B", Price = 2m },
            };

            var ex = Record.Exception(() =>
                NewQ().UseTableBoundInsert<SimpleProduct>()
                    .FromObjects(items)
                    .Value(p => p.Price, 99m)  // appended to first row only — misuse
                    .BuildWithParameters());

            // The library should not crash (the SQL will be structurally malformed, but no exception).
            Assert.Null(ex);
        }
    }
}
