using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Threading;
using PacodelaCruz.DurableFunctions.Models;
using PacodelaCruz.DurableFunctions.Approval.Models;

namespace PacodelaCruz.DurableFunctions.Approval
{
    public static class RequestApprovalOrchestration
    {
        /// <summary>
        /// Durable Orchestration
        /// Orchestrates a Request Approval Process using the Durable Functions Human Interaction Pattern
        /// The Approval Request can be sent via Email using SendGrid or via Slack. 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [FunctionName("OrchestrateRequestApproval")]
        public static async Task<bool> Run(
                                    [OrchestrationTrigger] DurableOrchestrationContext context, 
                                    TraceWriter log)
        {
            if (!context.IsReplaying)
                log.Info($"Starting orchestration {context.InstanceId}");

            context.SetCustomStatus("received");

            var isApproved = false;
            string meansOfApproval = Environment.GetEnvironmentVariable("Workflow:MeansOfApproval");
            ApprovalRequestMetadata approvalRequestMetadata = context.GetInput<ApprovalRequestMetadata>();
            approvalRequestMetadata.InstanceId = context.InstanceId;

            await context.CallActivityAsync("PersistWorkflowCorrelation",
                                            new WorkflowCorrelation {
                                                EntityId = $"{approvalRequestMetadata.ApplicantId}",
                                                WorkflowInstanceId = context.InstanceId
                                            });

            context.SetCustomStatus("in review");

            // Check whether the approval request is to be sent via Email or Slack based on App Settings
            if (meansOfApproval.Equals("email", StringComparison.OrdinalIgnoreCase))
            {
                if (!context.IsReplaying)
                    log.Info("Sending Approval Request Via Email");

                await context.CallActivityAsync("SendApprovalRequestViaEmail", approvalRequestMetadata);
            }
            else
            {
                if (!context.IsReplaying)
                    log.Info("Sending Approval Request Via Slack");

                await context.CallActivityAsync("SendApprovalRequestViaSlack", approvalRequestMetadata);
            }

            // Wait for Response as an external event or a time out. 
            // The approver has a limit to approve otherwise the request will be rejected.
            using (var timeoutCts = new CancellationTokenSource())
            {
                int timeout;
                if (!int.TryParse(Environment.GetEnvironmentVariable("Workflow:Timeout"), out timeout))
                    timeout = 5;
                DateTime expiration = context.CurrentUtcDateTime.AddMinutes(timeout);
                Task timeoutTask = context.CreateTimer(expiration, timeoutCts.Token);

                // This event can come from a click on the Email sent via SendGrid or a selection on the message sent via Slack. 
                Task<bool> approvalResponse = context.WaitForExternalEvent<bool>("ReceiveApprovalResponse");

                Task winner = await Task.WhenAny(approvalResponse, timeoutTask);

                ApprovalResponseMetadata approvalResponseMetadata = new ApprovalResponseMetadata()
                {
                    ReferenceUrl = approvalRequestMetadata.ReferenceUrl
                };

                if (winner == approvalResponse)
                {
                    if (!context.IsReplaying)
                        log.Info("An approval response was received");

                    if (approvalResponse.Result)
                    {
                        approvalResponseMetadata.Status = "approved";
                        context.SetCustomStatus("Approved. Congrats, You are super special!");
                    }
                    else
                    {
                        approvalResponseMetadata.Status = "rejected";
                        context.SetCustomStatus("Rejected. Sorry! Only the best can join us!");
                    }
                }
                else
                {
                    if (!context.IsReplaying)
                        log.Info("The waiting time has exceeded!");

                    approvalResponseMetadata.Status = "rejected";
                    context.SetCustomStatus("Rejected. Sorry! We are extremely busy. Try again in your next life!");
                }

                if (!timeoutTask.IsCompleted)
                {
                    // All pending timers must be completed or cancelled before the function exits.
                    timeoutCts.Cancel();
                }

                // Once the approval process has been finished, the Blob is to be moved to the corresponding container.

                if (!context.IsReplaying) 
                    log.Info("Moving the blob to the corresponding container");
                
                await context.CallActivityAsync<string>("MoveBlob", approvalResponseMetadata);
                return isApproved;
            }
        }
    }
}
