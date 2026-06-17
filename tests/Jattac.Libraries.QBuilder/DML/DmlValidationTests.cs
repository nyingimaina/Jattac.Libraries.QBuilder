namespace Jattac.QBuilderTests.DML
{
    using System;
    using Jattac.Libraries.QBuilder;
    using Jattac.QBuilderTests.Models;
    using Xunit;

    /// <summary>
    /// TDD contract tests for DML builder guard conditions.
    /// Written before implementation — all tests should fail red, then pass green after builders exist.
    /// </summary>
    public class DmlValidationTests
    {
        // ── DELETE ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Delete_WithNoWhere_ThrowsDangerousDelete()
        {
            var ex = Record.Exception(() =>
                Q.New(true)
                    .UseTableBoundDelete<User>()
                    .BuildWithParameters());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("ForEntireTable", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Delete_WithWhereCondition_DoesNotThrow()
        {
            var ex = Record.Exception(() =>
                Q.New(true)
                    .UseTableBoundDelete<User>()
                    .WhereEqualTo(u => u.Id, Guid.NewGuid())
                    .BuildWithParameters());

            Assert.Null(ex);
        }

        [Fact]
        public void Delete_ForEntireTable_DoesNotThrow()
        {
            var ex = Record.Exception(() =>
                Q.New(true)
                    .UseTableBoundDelete<User>()
                    .ForEntireTable()
                    .BuildWithParameters());

            Assert.Null(ex);
        }

        // ── UPDATE ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Update_WithNoSet_ThrowsNoColumnsSpecified()
        {
            var ex = Record.Exception(() =>
                Q.New(true)
                    .UseTableBoundUpdate<User>()
                    .WhereEqualTo(u => u.Id, Guid.NewGuid())
                    .BuildWithParameters());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("Set", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Update_WithSetButNoWhere_ThrowsDangerousUpdate()
        {
            var ex = Record.Exception(() =>
                Q.New(true)
                    .UseTableBoundUpdate<User>()
                    .Set(u => u.Name, "Alice")
                    .BuildWithParameters());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("ForEntireTable", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Update_WithSetAndWhere_DoesNotThrow()
        {
            var ex = Record.Exception(() =>
                Q.New(true)
                    .UseTableBoundUpdate<User>()
                    .Set(u => u.Name, "Alice")
                    .WhereEqualTo(u => u.Id, Guid.NewGuid())
                    .BuildWithParameters());

            Assert.Null(ex);
        }

        [Fact]
        public void Update_WithSetAndForEntireTable_DoesNotThrow()
        {
            var ex = Record.Exception(() =>
                Q.New(true)
                    .UseTableBoundUpdate<User>()
                    .Set(u => u.Name, "Alice")
                    .ForEntireTable()
                    .BuildWithParameters());

            Assert.Null(ex);
        }

        // ── INSERT ────────────────────────────────────────────────────────────────────

        [Fact]
        public void Insert_WithNoValues_ThrowsNoValues()
        {
            var ex = Record.Exception(() =>
                Q.New(true)
                    .UseTableBoundInsert<User>()
                    .BuildWithParameters());

            Assert.IsType<InvalidOperationException>(ex);
            Assert.Contains("Value", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Insert_WithValues_DoesNotThrow()
        {
            var ex = Record.Exception(() =>
                Q.New(true)
                    .UseTableBoundInsert<User>()
                    .Value(u => u.Id, Guid.NewGuid())
                    .Value(u => u.Name, "Alice")
                    .Value(u => u.IsActive, true)
                    .BuildWithParameters());

            Assert.Null(ex);
        }
    }
}
