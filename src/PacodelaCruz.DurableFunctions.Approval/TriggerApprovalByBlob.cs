using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.Http;
using PacodelaCruz.DurableFunctions.Models;

namespace PacodelaCruz.DurableFunctions.Approval
{
    public static class TriggerApprovalByBlob
    {
        /// <summary>
        /// Function triggered by a Blob Storage file which starts a Durable Function Orchestration
        /// and sends the blob metadata as context
        /// </summary>
        /// <param name="requestBlob"></param>
        /// <param name="name"></param>
        /// <param name="orchestrationClient"></param>
        /// <param name="log"></param>
        [FunctionName("TriggerApprovalByBlob")]
        public static async void Run(
            [BlobTrigger("%Bindings:InputContainerName%/{name}", Connection = "Blob:StorageConnection")] Stream requestBlob, 
            string name, 
            [OrchestrationClient] DurableOrchestrationClient orchestrationClient, 
            TraceWriter log)
        {
            log.Info($"Blob trigger function Processed blob\n Name:{name} \n Size: {requestBlob.Length} Bytes");
            string blobStorageBasePath = Environment.GetEnvironmentVariable("Blob:StorageBasePath", EnvironmentVariableTarget.Process);
            string blobContainer = Environment.GetEnvironmentVariable("Bindings:InputContainerName", EnvironmentVariableTarget.Process);
            string applicantId = "";
            string applicationName = "";

            // If the blob name containes a '+' sign, it identifies the first part of the blob name as the applicant and the remaining as the application name. Otherwise, the applicant is unknown and the application name is the full blobname. 
            if (name.Contains("_-_"))
            {
                applicantId = Uri.UnescapeDataString(name.Substring(0, name.LastIndexOf("_-_")));
                applicationName = name.Substring(name.LastIndexOf("_-_") + 3);
            }
            else
            {
                applicantId = "unknown";
                applicationName = name;
            }

            ApprovalRequestMetadata requestMetadata = new ApprovalRequestMetadata()
            {
                ApprovalType = "FurryModel",
                ReferenceUrl = $"{blobStorageBasePath}{blobContainer}/{name}",
                ApplicationName = applicationName,
                ApplicantId = applicantId
            };

            var instanceId = await orchestrationClient.StartNewAsync("OrchestrateRequestApproval", requestMetadata);
            log.Info($"Durable Function Ochestration started applicant '{applicantId}' and application '{applicationName}'. InstanceId: {instanceId}");
        }
    }
}
