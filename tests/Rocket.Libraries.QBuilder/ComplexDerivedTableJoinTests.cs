using Rocket.Libraries.Qurious;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Rocket.Libraries.QuriousTests
{
    public class ComplexDerivedTableJoinTests
    {
        private const string TaskHistoryAlias = "TaskHistoryAlias";

        [Fact]
        public void ItDoesComplexJoins()
        {
            var taskExecutionHistoryQBuilder = TaskHistoryQBuilder();
            var query = new QBuilder("f", parameterize: false)
                .UseTableBoundSelector<Schedule>()
                .Select(schedule => schedule.NextRunTime)
                .Then()
                .UseTableBoundSelector<TaskDefinition>()
                .Select(taskDefinition => taskDefinition.Name, nameof(TaskDescription.DisplayLabel))
                .Select(taskDefinition => taskDefinition.StaticName)
                .Then()
                .UseDerivedTableSelector(taskExecutionHistoryQBuilder)
                .Select(nameof(TaskExecutionHistory.StartTime), nameof(TaskDescription.LastStartTime))
                .Then()
                .UseTableBoundJoinBuilder<Schedule, TaskDefinition>()
                .InnerJoin(schedule => schedule.TaskDefinitionId, taskDefinition => taskDefinition.Id)
                .UseTableBoundFilter<TaskDefinition>()
                .WhereEqualTo(taskDefinition => taskDefinition.CompanyId, 1)
                .And<TaskDefinition>()
                .WhereEqualTo(taskDefinition => taskDefinition.Deleted, 0)
                .Then()
                .UseJoiner()
                .UseDerivedTableJoiner<TaskDefinition>()
                .LeftJoin(taskDefinition => taskDefinition.Id, taskExecutionHistoryQBuilder, nameof(TaskExecutionHistory.TaskDefinitionId))
                /*.UseDerivedTableJoiner<Ender>()
                .LeftJoin(ender => ender.EndTime, taskExecutionHistoryQBuilder, nameof(TaskExecutionHistory.StartTime))*/
                .Then()
                .Build();
        }

        private QBuilder TaskHistoryQBuilder()
        {
            const string latestCompletion = "LatestCompletion";
            var innerQbuilder = new QBuilder(parameterize: false)
                .UseTableBoundSelector<TaskExecutionHistory>()
                .SelectAggregated(taskExecutionHistory => taskExecutionHistory.EndTime, latestCompletion, "Max")
                .Select(taskExecutionHistory => taskExecutionHistory.TaskDefinitionId)
                .Then()
                .UseTableBoundGrouper<TaskExecutionHistory>()
                .GroupBy(taskExecutionHistory => taskExecutionHistory.TaskDefinitionId);

            var outerQbuilder = new QBuilder(TaskHistoryAlias, parameterize: false)
                .UseTableBoundSelector<TaskExecutionHistory>()
                .Select(taskExecutionHistory => taskExecutionHistory.EndTime)
                .Select(taskExecutionHistory => taskExecutionHistory.Id)
                .Select(taskExecutionHistory => taskExecutionHistory.Message)
                .Select(taskExecutionHistory => taskExecutionHistory.StartTime)
                .Select(taskExecutionHistory => taskExecutionHistory.Succeeded)
                .Select(taskExecutionHistory => taskExecutionHistory.TaskDefinitionId)
                .Then()
                .UseJoiner()
                .UseDerivedTableJoiner<TaskExecutionHistory>()
                .InnerJoin(taskExecutionHistory => taskExecutionHistory.TaskDefinitionId, innerQbuilder, nameof(TaskExecutionHistory.TaskDefinitionId))
                .UseDerivedTableJoiner<TaskExecutionHistory>()
                .InnerJoin(taskExecutionHistory => taskExecutionHistory.EndTime, innerQbuilder, latestCompletion)
                .Then();

            return outerQbuilder;
        }
    }
}