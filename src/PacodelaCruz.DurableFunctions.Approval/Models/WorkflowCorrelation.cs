using System;
using System.Collections.Generic;
using System.Text;

namespace PacodelaCruz.DurableFunctions.Approval.Models
{
    public class WorkflowCorrelation
    {
        public string EntityId { get; set; }
        public string WorkflowInstanceId { get; set; }
    }
}
