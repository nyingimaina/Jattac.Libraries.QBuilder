using System;
using System.Collections.Generic;
using Jattac.Libraries.QBuilder;
using Jattac.QBuilderTests.Models;
using Xunit;

namespace Jattac.QBuilderTests
{
    /// <summary>
    /// Tests covering the issues identified and fixed in the code review.
    /// </summary>
    public class FixedIssuesTests
    {
        // ── LIKE pattern correctness ─────────────────────────────────────────

        [Fact]
        public void StartsWith_NonParameterized_ProducesCorrectLikePattern()
        {
            var result = new QBuilder(parameterize: false)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereStartsWith(x => x.Name, "foo")
                .Then()
                .Build();

            Assert.Contains("Like 'foo%'", result);
        }

        [Fact]
        public void EndsWith_NonParameterized_ProducesCorrectLikePattern()
        {
            var result = new QBuilder(parameterize: false)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereEndsWith(x => x.Name, "bar")
                .Then()
                .Build();

            Assert.Contains("Like '%bar'", result);
        }

        [Fact]
        public void Contains_NonParameterized_ProducesCorrectLikePattern()
        {
            var result = new QBuilder(parameterize: false)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereContains(x => x.Name, "baz")
                .Then()
                .Build();

            Assert.Contains("Like '%baz%'", result);
        }

        [Fact]
        public void StartsWith_Parameterized_ValueHasPercentSuffix()
        {
            var qBuilder = new QBuilder(parameterize: true)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereStartsWith(x => x.Name, "foo")
                .Then();

            var result = qBuilder.BuildWithParameters();

            Assert.Single(result.Parameters);
            Assert.Equal("foo%", result.Parameters["@Name0"]);
        }

        [Fact]
        public void EndsWith_Parameterized_ValueHasPercentPrefix()
        {
            var qBuilder = new QBuilder(parameterize: true)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereEndsWith(x => x.Name, "bar")
                .Then();

            var result = qBuilder.BuildWithParameters();

            Assert.Single(result.Parameters);
            Assert.Equal("%bar", result.Parameters["@Name0"]);
        }

        // ── WhereNotIn parameterization ──────────────────────────────────────

        [Fact]
        public void WhereNotIn_Parameterized_BindsAllValues()
        {
            var qBuilder = new QBuilder(parameterize: true)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereNotIn<int, int>(x => x.Id, new List<int> { 1, 2, 3 })
                .Then();

            var result = qBuilder.BuildWithParameters();

            Assert.Equal(3, result.Parameters.Count);
            Assert.Contains("not in", result.ParameterizedSql);
        }

        [Fact]
        public void WhereNotIn_NonParameterized_EscapesSingleQuotes()
        {
            var result = new QBuilder(parameterize: false)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereNotIn<string, string>(x => x.Name, new List<string> { "O'Brien" })
                .Then()
                .Build();

            Assert.Contains("O''Brien", result);
        }

        // ── WhereExplicitly guard in parameterized mode ──────────────────────

        [Fact]
        public void WhereExplicitly_InParameterizedMode_ThrowsInvalidOperationException()
        {
            var filter = new QBuilder(parameterize: true)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseFilter();

            Assert.Throws<InvalidOperationException>(() => filter.WhereExplicitly("1=1"));
        }

        [Fact]
        public void WhereExplicitly_InNonParameterizedMode_Works()
        {
            var result = new QBuilder(parameterize: false)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseFilter()
                .WhereExplicitly("1=1")
                .Then()
                .Build();

            Assert.Contains("1=1", result);
        }

        // ── BuildWithParameters guard ────────────────────────────────────────

        [Fact]
        public void BuildWithParameters_WhenNotParameterized_ThrowsInvalidOperationException()
        {
            var qBuilder = new QBuilder(parameterize: false)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then();

            Assert.Throws<InvalidOperationException>(() => qBuilder.BuildWithParameters());
        }

        // ── Build() called twice guard ───────────────────────────────────────

        [Fact]
        public void Build_CalledTwice_ThrowsInvalidOperationException()
        {
            var qBuilder = new QBuilder(parameterize: false)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then();

            qBuilder.Build();

            Assert.Throws<InvalidOperationException>(() => qBuilder.Build());
        }

        // ── WhereIn single-quote escaping ────────────────────────────────────

        [Fact]
        public void WhereIn_NonParameterized_EscapesSingleQuotes()
        {
            var result = new QBuilder(parameterize: false)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereIn<string, string>(x => x.Name, new List<string> { "O'Reilly" })
                .Then()
                .Build();

            Assert.Contains("O''Reilly", result);
        }

        // ── WhereIn / WhereNotIn with null/empty inputs ──────────────────────

        [Fact]
        public void WhereIn_NullValues_ReturnsWithoutFilter()
        {
            var result = new QBuilder(parameterize: false)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereIn<int, int>(x => x.Id, null)
                .Then()
                .Build();

            Assert.DoesNotContain("Where", result);
        }

        [Fact]
        public void WhereIn_EmptyValues_ReturnsWithoutFilter()
        {
            var result = new QBuilder(parameterize: false)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereIn<int, int>(x => x.Id, new List<int>())
                .Then()
                .Build();

            Assert.DoesNotContain("Where", result);
        }

        // ── Parameterized WhereIn ────────────────────────────────────────────

        [Fact]
        public void WhereIn_Parameterized_BindsAllValues()
        {
            var qBuilder = new QBuilder(parameterize: true)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereIn<int, int>(x => x.Id, new List<int> { 10, 20 })
                .Then();

            var result = qBuilder.BuildWithParameters();

            Assert.Equal(2, result.Parameters.Count);
            Assert.Contains(" in (", result.ParameterizedSql);
        }
    }
}
