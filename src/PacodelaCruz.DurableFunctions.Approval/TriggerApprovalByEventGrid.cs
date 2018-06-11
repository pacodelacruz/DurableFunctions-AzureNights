using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Host;
using PacodelaCruz.DurableFunctions.Models;
using System;

namespace PacodelaCruz.DurableFunctions.Approval
{
    public static class TriggerApprovalByEventGrid
    {
        [FunctionName("TriggerApprovalByEventGrid")]
        public static void EventGridTest(
            [EventGridTrigger]EventGridEvent eventGridEvent, 
            [OrchestrationClient] DurableOrchestrationClient orchestrationClient, 
            TraceWriter log)
        {

            string blobName = eventGridEvent.Subject.Replace(@"/blobServices/default/containers/requests/blobs/", "");
            log.Info($"Event Grid trigger function Processed blob\n Name: '{blobName}'");

            dynamic eventGridData = eventGridEvent.Data;
            string blobUrl = eventGridData?.url;
            string contentLenghtProp;
            int contentLenght = 0;
            if (eventGridData?.contentLength != null)
            {
                contentLenghtProp = (string)eventGridData?.contentLength;
                int.TryParse(contentLenghtProp, out contentLenght);
            }

            log.Info($"Content Lenght '{contentLenght.ToString()}'");

            if (contentLenght > 1000 && contentLenght < 2400000)
            {
                string blobStorageBasePath = Environment.GetEnvironmentVariable("Blob:StorageBasePath", EnvironmentVariableTarget.Process);
                string blobContainer = Environment.GetEnvironmentVariable("Bindings:InputContainerName", EnvironmentVariableTarget.Process);
                string applicantId = "";
                string applicationName = "";

                // If the blob name containes a '+' sign, it identifies the first part of the blob name as the applicant and the remaining as the application name. Otherwise, the applicant is unknown and the application name is the full blobname. 
                if (blobName.Contains("_-_"))
                {
                    applicantId = Uri.UnescapeDataString(blobName.Substring(0, blobName.LastIndexOf("_-_")));
                    applicationName = blobName.Substring(blobName.LastIndexOf("_-_") + 3);
                }
                else
                {
                    applicantId = "unknown";
                    applicationName = blobName;
                }

                ApprovalRequestMetadata requestMetadata = new ApprovalRequestMetadata()
                {
                    ApprovalType = "FurryModel",
                    ReferenceUrl = blobUrl,
                    ApplicationName = applicationName,
                    ApplicantId = applicantId
                };

                var instanceId = orchestrationClient.StartNewAsync("OrchestrateRequestApproval", requestMetadata).Result;
                log.Info($"Durable Function Ochestration started applicant '{applicantId}' and application '{applicationName}'. InstanceId: {instanceId}");
            }
            else
            {
                log.Error($"Event Grid Event has been discarded. Blob content lenght: {contentLenght}");
            }
        }
    }
}