using System;

namespace Jattac.QBuilderTests.Models
{
    internal class WorkflowInstanceState
    {
        public Guid WorkflowInstanceId { get; set; }
        public DateTime Created { get; set; }
        public Guid UrgencyId { get; set; }
    }
}