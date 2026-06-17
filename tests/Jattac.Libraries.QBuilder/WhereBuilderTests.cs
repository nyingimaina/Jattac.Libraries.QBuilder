using System;
using Jattac.QBuilderTests.Models;
using Xunit;
using Jattac.Libraries.QBuilder;

namespace Jattac.QBuilderTests
{
    class TestTable
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class WhereBuilderTests
    {
        [Fact]
        public void TestBasicParameterizedWhere()
        {
            var qBuilder = new QBuilder(parameterize: true)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereEqualTo(x => x.Name, "TestName")
                .Then();

            var result = qBuilder.BuildWithParameters();
            var parameterName = "@Name0";
            var normalized = result.ParameterizedSql.Replace("\r\n", "\n").Replace("\r", "\n");
            // Parameterized SQL uses bare @param names — no quotes around the placeholder
            Assert.Equal($"Select * from (Select \ntTestTable.Id From TestTable tTestTable\nWhere tTestTable.Name  = {parameterName}\n) as t", normalized);
        }

        // BUG-1 regression: WhereIsNull must exist on TableBoundWhereBuilder<T>
        // This test confirms the method is present and generates correct SQL.
        [Fact]
        public void WhereIsNull_GeneratesIsNullSql()
        {
            var sql = new QBuilder(parameterize: false)
                .UseTableBoundSelector<User>()
                    .Select(u => u.Id)
                .Then()
                .UseTableBoundFilter<User>()
                    .WhereIsNull(u => u.DeletedAt)
                .Then()
                .Build();

            var normalized = sql.Replace("\r\n", "\n").Replace("\r", "\n");
            Assert.Contains("IS NULL", normalized);
            Assert.Contains("tUser.DeletedAt", normalized);
        }

        [Fact]
        public void TestBasicUnParameterizedWhere()
        {
            var qBuilder = new QBuilder(parameterize: false)
                .UseTableBoundSelector<TestTable>()
                .Select(x => x.Id)
                .Then()
                .UseTableBoundFilter<TestTable>()
                .WhereEqualTo(x => x.Name, "TestName")
                .Then();

            var result = qBuilder.Build();
            var value = "TestName";
            var normalized = result.Replace("\r\n", "\n").Replace("\r", "\n");
            Assert.Equal($"Select * from (Select \ntTestTable.Id From TestTable tTestTable\nWhere tTestTable.Name  = '{value}'\n) as t", normalized);
        }
    }
}