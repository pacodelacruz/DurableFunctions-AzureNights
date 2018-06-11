using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using PacodelaCruz.DurableFunctions.Approval.Models;

namespace PacodelaCruz.DurableFunctions.Approval
{
    public static class GetApprovalStatus
    {
        /// <summary>
        /// Http API to get the status o
        /// </summary>
        /// <param name="req"></param>
        /// <param name="orchestrationClient"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("GetApprovalStatus")]
        public static async Task<HttpResponseMessage> Run(
                                                        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "getstatus")] HttpRequestMessage req,
                                                        [OrchestrationClient] DurableOrchestrationClient orchestrationClient,
                                                        TraceWriter log)
        {
            try
            {
                string applicantId = req.RequestUri.ParseQueryString().GetValues("applicantId")[0];

                log.Info($"Getting worfklow status for applicant Id: {applicantId}");

                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(Environment.GetEnvironmentVariable("Blob:StorageConnection"));
                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();
                CloudTable table = tableClient.GetTableReference("WorkflowCorrelations");
                TableOperation retrieveOperation = TableOperation.Retrieve<WorkflowCorrelationTableEntity>("ApprovalWorkflow", applicantId);
                TableResult retrievedResult = await table.ExecuteAsync(retrieveOperation);

                if (retrievedResult.Result != null)
                {
                    string instanceId = ((WorkflowCorrelationTableEntity)retrievedResult.Result).InstanceId;
                    return orchestrationClient.CreateCheckStatusResponse(req, instanceId);
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("ApplicationId was not found") };
                }

            }
            catch (Exception ex)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent($"Internal Serverless Error: {ex.Message}") };
            }
        }
    }
}
