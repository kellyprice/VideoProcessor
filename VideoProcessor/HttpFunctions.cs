using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System.Net.Http;

namespace VideoProcessor
{
    public static class HttpFunctions
    {
        // chaining
        [FunctionName(nameof(ProcessVideoStarter))]
        public static async Task<IActionResult> ProcessVideoStarter(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
                [DurableClient] IDurableOrchestrationClient starter,
                ILogger log)
        {
            var video = req.GetQueryParameterDictionary()["video"];

            if (video == null)
            {
                return new BadRequestObjectResult("please pass the video location in the query string...");
            }

            string instanceId = await starter.StartNewAsync("ProcessVideoOrchestrator", null, video);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        // fan-out fan-in
        [FunctionName(nameof(ProcessVideoWithBitRateStarter))]
        public static async Task<IActionResult> ProcessVideoWithBitRateStarter(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
                [DurableClient] IDurableOrchestrationClient starter,
                ILogger log)
        {
            var video = req.GetQueryParameterDictionary()["video"];

            if (video == null)
            {
                return new BadRequestObjectResult("please pass the video location in the query string...");
            }

            string instanceId = await starter.StartNewAsync("ProcessVideoWithBitRateOrchestrator", null, video);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        // human interaction
        [FunctionName(nameof(ProcessVideoWithInteractionStarter))]
        public static async Task<IActionResult> ProcessVideoWithInteractionStarter(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
                [DurableClient] IDurableOrchestrationClient starter,
                ILogger log)
        {
            var video = req.GetQueryParameterDictionary()["video"];

            if (video == null)
            {
                return new BadRequestObjectResult("please pass the video location in the query string...");
            }

            string instanceId = await starter.StartNewAsync("ProcessVideoWithInteractionOrchestrator", null, video);

            log.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        // perpetual
        [FunctionName(nameof(PeriodicTaskStarter))]
        public static async Task<IActionResult> PeriodicTaskStarter(
                [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req,
                [DurableClient] IDurableOrchestrationClient starter,
                ILogger log)
        {
            string instanceId = await starter.StartNewAsync("PeriodicTaskOrchestrator", null, 0);

            var payload = starter.CreateHttpManagementPayload(instanceId);

            return new OkObjectResult(payload);
        }
    }
}
