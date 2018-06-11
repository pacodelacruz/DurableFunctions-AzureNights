using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Text;

namespace PacodelaCruz.DurableFunctions.Approval.Models
{
    public class WorkflowCorrelationTableEntity : TableEntity
    {
        public WorkflowCorrelationTableEntity(string workflowType, string entityId)
        {
            this.PartitionKey = workflowType;
            this.RowKey = entityId;
        }

        public WorkflowCorrelationTableEntity()
        {

        }
        public string InstanceId { get; set; }
    }
}
