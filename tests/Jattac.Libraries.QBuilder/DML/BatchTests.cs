namespace Jattac.QBuilderTests.DML
{
    using System;
    using System.Collections.Generic;
    using Jattac.Libraries.QBuilder;
    using Jattac.QBuilderTests.Models;
    using Xunit;

    /// <summary>
    /// Unit tests for <c>QBatch</c> — parameter collision renaming, SQL joining, and guard conditions.
    /// No database involved.
    /// </summary>
    public class BatchTests
    {
        private static BuiltQuery MakeUpdate(string id, string name)
        {
            var user = new UserWithKey { Id = Guid.Parse(id), Name = name };
            return Q.New(true).UseTableBoundUpdate<UserWithKey>().FromObject(user).BuildWithParameters();
        }

        private static BuiltQuery MakeDelete(string id)
        {
            var user = new UserWithKey { Id = Guid.Parse(id) };
            return Q.New(true).UseTableBoundDelete<UserWithKey>().FromObject(user).BuildWithParameters();
        }

        [Fact]
        public void QBatch_TwoQueries_SqlJoinedWithSemicolon()
        {
            var q1 = MakeUpdate("00000000-0000-0000-0000-000000000001", "Alice");
            var q2 = MakeUpdate("00000000-0000-0000-0000-000000000002", "Bob");

            var batch = QBatch.New().Add(q1).Add(q2).Build();

            Assert.Contains(";\n", batch.ParameterizedSql);
        }

        [Fact]
        public void QBatch_NoCollision_ParamsMergedAsIs()
        {
            // Two queries with naturally different param names (different suffixes from different builds).
            var q1 = Q.New(true).UseTableBoundInsert<SimpleProduct>()
                .Value(p => p.Id, 1).Value(p => p.Name, "A").Value(p => p.Price, 1m)
                .BuildWithParameters();
            var q2 = Q.New(true).UseTableBoundInsert<SimpleProduct>()
                .Value(p => p.Id, 2).Value(p => p.Name, "B").Value(p => p.Price, 2m)
                .BuildWithParameters();

            // q1 params: @Id0, @Name0, @Price0  (fresh BuiltQuery)
            // q2 params: @Id0, @Name0, @Price0  (another fresh BuiltQuery — will collide)
            // After batch: q2's params should be renamed.
            var batch = QBatch.New().Add(q1).Add(q2).Build();

            // Total 6 unique params.
            Assert.Equal(6, batch.Parameters.Count);
        }

        [Fact]
        public void QBatch_ParamCollision_CollidingParamRenamed()
        {
            var q1 = MakeUpdate("00000000-0000-0000-0000-000000000001", "Alice");
            var q2 = MakeUpdate("00000000-0000-0000-0000-000000000002", "Bob");

            var batch = QBatch.New().Add(q1).Add(q2).Build();

            // Each query generates @Name0 and @Id0. After merging:
            // q1's @Name0 + @Id0 stay; q2's get renamed @Name1 + @Id1.
            Assert.True(batch.Parameters.ContainsKey("@Id0"), "Expected @Id0");
            Assert.True(batch.Parameters.ContainsKey("@Id1"), "Expected @Id1");
            Assert.False(batch.Parameters.ContainsKey("@Id2"), "Should not have @Id2");
        }

        [Fact]
        public void QBatch_ParamCollision_SqlUpdatedToMatchRenamedParam()
        {
            var q1 = MakeUpdate("00000000-0000-0000-0000-000000000001", "Alice");
            var q2 = MakeUpdate("00000000-0000-0000-0000-000000000002", "Bob");

            var batch = QBatch.New().Add(q1).Add(q2).Build();

            var parts = batch.ParameterizedSql.Split(new[] { ";\n" }, StringSplitOptions.None);
            Assert.Equal(2, parts.Length);

            // Second statement must not still say @Id0 — it should say @Id1 after rename.
            Assert.DoesNotContain("@Id0", parts[1]);
            Assert.Contains("@Id1", parts[1]);
        }

        [Fact]
        public void QBatch_ParamCollision_ValuesCorrectAfterRename()
        {
            var id1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var id2 = Guid.Parse("00000000-0000-0000-0000-000000000002");

            var user1 = new UserWithKey { Id = id1, Name = "Alice" };
            var user2 = new UserWithKey { Id = id2, Name = "Bob" };

            var q1 = Q.New(true).UseTableBoundUpdate<UserWithKey>().FromObject(user1).BuildWithParameters();
            var q2 = Q.New(true).UseTableBoundUpdate<UserWithKey>().FromObject(user2).BuildWithParameters();

            var batch = QBatch.New().Add(q1).Add(q2).Build();

            // @Id0 must map to id1, @Id1 must map to id2.
            Assert.Equal(id1, batch.Parameters["@Id0"]);
            Assert.Equal(id2, batch.Parameters["@Id1"]);
        }

        [Fact]
        public void QBatch_ThreeQueries_AllParamsMergedCorrectly()
        {
            var q1 = MakeDelete("00000000-0000-0000-0000-000000000001");
            var q2 = MakeDelete("00000000-0000-0000-0000-000000000002");
            var q3 = MakeDelete("00000000-0000-0000-0000-000000000003");

            var batch = QBatch.New().Add(q1).Add(q2).Add(q3).Build();

            // Each delete has @Id0; after merge: @Id0, @Id1, @Id2.
            Assert.Equal(3, batch.Parameters.Count);
            Assert.True(batch.Parameters.ContainsKey("@Id0"));
            Assert.True(batch.Parameters.ContainsKey("@Id1"));
            Assert.True(batch.Parameters.ContainsKey("@Id2"));
        }

        [Fact]
        public void QBatch_EmptyBatch_ThrowsGuard()
        {
            var ex = Record.Exception(() => QBatch.New().Build());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("No queries", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void QBatch_AddNull_ThrowsArgumentNull()
        {
            var ex = Record.Exception(() => QBatch.New().Add(null));

            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void QBatch_AddEmptySql_ThrowsGuard()
        {
            var ex = Record.Exception(() => QBatch.New().Add(new BuiltQuery()));

            Assert.IsType<InvalidOperationException>(ex);
        }

        [Fact]
        public void QBatch_AddRange_AddsAll()
        {
            var queries = new List<BuiltQuery>
            {
                MakeDelete("00000000-0000-0000-0000-000000000001"),
                MakeDelete("00000000-0000-0000-0000-000000000002"),
                MakeDelete("00000000-0000-0000-0000-000000000003"),
            };

            var batch = QBatch.New().AddRange(queries).Build();
            var parts = batch.ParameterizedSql.Split(new[] { ";\n" }, StringSplitOptions.None);
            Assert.Equal(3, parts.Length);
        }

        [Fact]
        public void QBatch_AddRange_NullCollection_Throws()
        {
            var ex = Record.Exception(() => QBatch.New().AddRange(null));

            Assert.IsType<ArgumentNullException>(ex);
        }

        [Fact]
        public void QBatch_ShortParamName_NoFalseSubstringReplacement()
        {
            // Critical correctness test: @Id0 must not corrupt @Id01.
            // Build q1 with @Id0 (three-char suffix digit 0)
            // Build q2 whose SQL contains @Id01 (two-digit suffix).

            var q1 = new BuiltQuery
            {
                ParameterizedSql = "Delete From User Where User.Id = @Id0",
                Parameters = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["@Id0"] = Guid.NewGuid()
                }
            };

            // Manually craft q2 with @Id01 to simulate a param name that starts with @Id0.
            var id01Value = Guid.NewGuid();
            var q2 = new BuiltQuery
            {
                ParameterizedSql = "Delete From Order Where Order.UserId = @Id01",
                Parameters = new System.Collections.Generic.Dictionary<string, object>
                {
                    ["@Id01"] = id01Value
                }
            };

            var batch = QBatch.New().Add(q1).Add(q2).Build();

            // @Id01 from q2 should NOT have been corrupted into @Id11 or similar.
            // After processing q1: @Id0 in merged dict.
            // Processing q2: @Id01 does not collide with @Id0, so it stays as @Id01.
            Assert.True(batch.Parameters.ContainsKey("@Id01"),
                "Expected @Id01 to be preserved; naive string.Replace would corrupt it to @Id11");
            Assert.Equal(id01Value, batch.Parameters["@Id01"]);

            // q2's SQL must still reference @Id01, not @Id11.
            var parts = batch.ParameterizedSql.Split(new[] { ";\n" }, StringSplitOptions.None);
            Assert.Contains("@Id01", parts[1]);
            Assert.DoesNotContain("@Id11", parts[1]);
        }

        [Fact]
        public void QBatch_BuildReturnValue_MustBeCaptured()
        {
            var q = MakeDelete("00000000-0000-0000-0000-000000000001");
            var batch = QBatch.New().Add(q);

            // Build() must return a non-null BuiltQuery with populated SQL.
            var result = batch.Build();
            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.ParameterizedSql));
        }
    }
}
