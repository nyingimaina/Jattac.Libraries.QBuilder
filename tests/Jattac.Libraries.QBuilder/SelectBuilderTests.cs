using Jattac.Libraries.QBuilder;
using Jattac.QBuilderTests.Models;
using Xunit;

namespace Jattac.QBuilderTests
{
    public class SelectBuilderTests
    {
        /*[Fact]
        public void DerivedTableSelect()
        {
            var query = new QBuilder()
                .UseSelector()
                .Select<WorkflowInstance>("*")
                .SelectDistinctRows()
                .Then()
                .UseJoiner()
                .InnerJoin<WorkflowInstanceState, WorkflowInstance>(nameof(WorkflowInstanceState.WorkflowInstanceId), nameof(WorkflowInstance.Id))
                .BeginInnerJoinToDerivedTable("latestState", nameof(WorkflowInstanceState.WorkflowInstanceId), nameof(WorkflowInstance.Id))
                .UseSelector()
                .Select<WorkflowInstanceState>(nameof(WorkflowInstanceState.WorkflowInstanceId))
                .SelectAggregated<WorkflowInstanceState>("Created", "BiggestCreated", "Avg")
                .Then()
                .UseGrouper()
                .GroupBy<WorkflowInstanceState>(nameof(WorkflowInstanceState.WorkflowInstanceId))
                .Then()
                .FinishJoinToDerivedTable()
                .Then()
                .Build();
            Assert.True(true);
        }*/
    }
}