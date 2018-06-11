using System;
using System.Collections.Generic;
using System.Text;

namespace PacodelaCruz.DurableFunctions.Approval.Models
{

    public class WorkflowCorrelationTableRecord
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public string InstanceId { get; set; }
    }
}
