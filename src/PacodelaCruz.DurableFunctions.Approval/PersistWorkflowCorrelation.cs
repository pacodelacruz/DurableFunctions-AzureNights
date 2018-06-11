using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using PacodelaCruz.DurableFunctions.Approval.Models;
using PacodelaCruz.DurableFunctions.Models;

namespace PacodelaCruz.DurableFunctions.Approval
{
    public static class PersistWorkflowCorrelation
    {
        /// <summary>
        /// Persist the correlation between an entity Id and its workflow instance id. 
        /// This allows to query the status of a worfklow instance based on a business entity id. 
        /// </summary>
        /// <param name="workflowCorrelation"></param>
        /// <param name="log"></param>
        [FunctionName("PersistWorkflowCorrelation")]
        [return: Table("WorkflowCorrelations")]
        public static WorkflowCorrelationTableRecord Run(
                                    [ActivityTrigger] WorkflowCorrelation workflowCorrelation, 
                                    TraceWriter log)
        {
            log.Info($"Persisting Correlation");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("Blob:StorageConnection"));
            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("WorkflowCorrelations");

            TableOperation operation = TableOperation.Retrieve<WorkflowCorrelationTableEntity>("ApprovalWorkflow", workflowCorrelation.EntityId);
            TableResult result = table.ExecuteAsync(operation).Result;

            string entityId = workflowCorrelation.EntityId;
            
            if (result.Result != null)
            {
                // When the EntityId already exists, append the timestamp
                entityId = $"{workflowCorrelation.EntityId}_{DateTime.Now.ToString("yyyyMMddTHHmmssfff")}";
            }

            return new WorkflowCorrelationTableRecord { PartitionKey = "ApprovalWorkflow", RowKey = entityId, InstanceId = workflowCorrelation.WorkflowInstanceId };
        }


    }
}
