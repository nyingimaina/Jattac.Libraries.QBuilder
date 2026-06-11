using System;
using Xunit;
using Rocket.Libraries.Qurious;

namespace Rocket.Libraries.QuriousTests
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