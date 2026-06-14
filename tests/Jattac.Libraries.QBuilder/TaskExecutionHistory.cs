using System;

namespace Jattac.QBuilderTests
{
    internal class TaskExecutionHistory
    {
        public object EndTime { get; internal set; }

        public object TaskDefinitionId { get; set; }
        public object Id { get; internal set; }
        public object Succeeded { get; internal set; }
        public object Message { get; internal set; }
        internal object StartTime { get; set; }
    }
}