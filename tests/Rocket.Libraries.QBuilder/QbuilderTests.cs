using Rocket.Libraries.Qurious;
using Rocket.Libraries.QuriousTests.Models;
using Xunit;

namespace Rocket.Libraries.QuriousTests
{
    public class QbuilderTests
    {
        [Fact]
        public void DerivedTableJoinsWorkFine()
        {
            var derivedQuery = new QBuilder("derived", parameterize: false)
                .UseTableBoundSelector<WorkflowInstanceState>()
                .SelectAggregated(t => t.Created, "Created", "Max")
                .Select(t => t.WorkflowInstanceId)
                .Then()
                .UseTableBoundGrouper<WorkflowInstanceState>()
                .Then();

            var query = new QBuilder("t", parameterize: false)
                .UseTableBoundSelector<WorkflowInstance>()
                .Select(t => t.Id)
                .Then()
                .UseDerivedTableSelector(derivedQuery)
                .Select("*")
                .Then()
                .UseJoiner()
                .UseDerivedTableJoiner<WorkflowInstance>()
                .InnerJoin(t => t.Id, derivedQuery, nameof(WorkflowInstanceState.WorkflowInstanceId))
                .InnerJoin<User, WorkflowInstance>(nameof(User.Id), nameof(WorkflowInstance.UserId))
                .Then()
                .Build();
        }

        [Fact]
        public void NonDerivedTableJoinsWorkFine()
        {
            var query = new QBuilder("t", parameterize: false)
                .UseTableBoundSelector<WorkflowInstance>()
                .Select(t => t.Id)
                .Then()
                .UseTableBoundSelector<WorkflowInstanceState>()
                .Select(t => t.Created)
                .Then()
                .UseTableBoundJoinBuilder<WorkflowInstance, WorkflowInstanceState>()
                .InnerJoin(l => l.Id, r => r.WorkflowInstanceId)
                .Build();
        }
    }
}