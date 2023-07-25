using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Threading;

namespace VideoProcessor
{
    public static class OrchestratorFunctions
    {
        // chaining
        [FunctionName(nameof(ProcessVideoOrchestrator))]
        public static async Task<object> ProcessVideoOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log = context.CreateReplaySafeLogger(log);
            var transcodedLocation = "";
            var thumbnailLocation = "";
            var withIntroLocation = "";

            try
            {

                var videoLocation = context.GetInput<string>();

                log.LogInformation("calling transcode");
                transcodedLocation = await context.CallActivityAsync<string>("TranscodeVideo", videoLocation);

                log.LogInformation("extract thumbnail");
                thumbnailLocation = await context.CallActivityWithRetryAsync<string>(
                    "ExtractThumbnail",
                    new RetryOptions(TimeSpan.FromSeconds(5), 4),
                    transcodedLocation);

                log.LogInformation("prepend intro");
                withIntroLocation = await context.CallActivityAsync<string>("PrependIntro", transcodedLocation);

                return new
                {
                    Transcoded = transcodedLocation,
                    Thumbnail = thumbnailLocation,
                    WithIntro = withIntroLocation,
                };
            }
            catch (Exception e)
            {
                log.LogError($"caught an error from an activity: {e.Message}");

                await context.CallActivityAsync<string>("Cleanup", new[] { transcodedLocation, thumbnailLocation, withIntroLocation });

                return new
                {
                    Error = "Failed to process uploaded video",
                    Message = e.Message,
                };
            }
        }

        // fan-out fan-in
        [FunctionName(nameof(ProcessVideoWithBitRateOrchestrator))]
        public static async Task<object> ProcessVideoWithBitRateOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log = context.CreateReplaySafeLogger(log);
            var videoLocation = context.GetInput<string>();
            var transcodedLocation = "";
            var thumbnailLocation = "";
            var withIntroLocation = "";

            try
            {
                var bitRates = await context.CallActivityAsync<int[]>("GetTranscodeBitRates", null);
                var transcodedTasks = new List<Task<VideoFileInfo>>();

                foreach (var bitRate in bitRates)
                {
                    log.LogInformation($"calling transcode with bitrate {bitRate}kbps");

                    var info = new VideoFileInfo() { Location = videoLocation, BitRate = bitRate };
                    var task = context.CallActivityAsync<VideoFileInfo>("TranscodeVideoWithBitRate", info);
                    transcodedTasks.Add(task);
                }

                var transcodeResults = await Task.WhenAll(transcodedTasks);

                transcodedLocation = transcodeResults.OrderByDescending(x => x.BitRate).Select(x => x.Location).First();

                log.LogInformation("extract thumbnail");
                thumbnailLocation = await context.CallActivityWithRetryAsync<string>(
                    "ExtractThumbnail",
                    new RetryOptions(TimeSpan.FromSeconds(5), 4),
                    transcodedLocation);

                log.LogInformation("prepend intro");
                withIntroLocation = await context.CallActivityAsync<string>("PrependIntro", transcodedLocation);

                return new
                {
                    Transcoded = transcodedLocation,
                    Thumbnail = thumbnailLocation,
                    WithIntro = withIntroLocation,
                };
            }
            catch (Exception e)
            {
                log.LogError($"caught an error from an activity: {e.Message}");

                await context.CallActivityAsync<string>("Cleanup", new[] { transcodedLocation, thumbnailLocation, withIntroLocation });

                return new
                {
                    Error = "Failed to process uploaded video",
                    Message = e.Message,
                };
            }
        }

        // human interaction
        [FunctionName(nameof(ProcessVideoWithInteractionOrchestrator))]
        public static async Task<object> ProcessVideoWithInteractionOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log = context.CreateReplaySafeLogger(log);
            var videoLocation = context.GetInput<string>();
            var transcodedLocation = "";
            var thumbnailLocation = "";
            var withIntroLocation = "";
            var approvalResult = "Unknown";

            try
            {
                var bitRates = await context.CallActivityAsync<int[]>("GetTranscodeBitRates", null);
                var transcodedTasks = new List<Task<VideoFileInfo>>();

                foreach (var bitRate in bitRates)
                {
                    log.LogInformation($"calling transcode with bitrate {bitRate}kbps");

                    var info = new VideoFileInfo() { Location = videoLocation, BitRate = bitRate };
                    var task = context.CallActivityAsync<VideoFileInfo>("TranscodeVideoWithBitRate", info);
                    transcodedTasks.Add(task);
                }

                var transcodeResults = await Task.WhenAll(transcodedTasks);

                transcodedLocation = transcodeResults.OrderByDescending(x => x.BitRate).Select(x => x.Location).First();

                log.LogInformation("extract thumbnail");
                thumbnailLocation = await context.CallActivityWithRetryAsync<string>(
                    "ExtractThumbnail",
                    new RetryOptions(TimeSpan.FromSeconds(5), 4),
                    transcodedLocation);

                log.LogInformation("prepend intro");
                withIntroLocation = await context.CallActivityAsync<string>("PrependIntro", transcodedLocation);

                await context.CallActivityAsync("SendApprovalRequestEmail", withIntroLocation);

                try
                {
                    approvalResult = await context.WaitForExternalEvent<string>("ApprovalResult", TimeSpan.FromSeconds(300));
                }
                catch
                {
                    log.LogWarning("Timed out waiting for approval");
                    approvalResult = "Timed out";
                }

                // copy and paste the below code into PowerShell to approve. this is for testing only and
                // should not be used in production as it exposes secrets. 
                // Invoke-RestMethod -Method Post -ContentType "application/json" -Body '"Approved"' -UseBasicParsing -Uri {get url from json}"

                if (approvalResult == "Approved")
                {
                    await context.CallActivityAsync("PublishVideo", withIntroLocation);
                }
                else
                {
                    await context.CallActivityAsync("RejectVideo", withIntroLocation);
                }

                return new
                {
                    Transcoded = transcodedLocation,
                    Thumbnail = thumbnailLocation,
                    WithIntro = withIntroLocation,
                    Approval = approvalResult,
                };
            }
            catch (Exception e)
            {
                log.LogError($"caught an error from an activity: {e.Message}");

                await context.CallActivityAsync<string>("Cleanup", new[] { transcodedLocation, thumbnailLocation, withIntroLocation });

                return new
                {
                    Error = "Failed to process uploaded video",
                    Message = e.Message,
                };
            }
        }

        // perpetual
        [FunctionName(nameof(PeriodicTaskOrchestrator))]
        public static async Task<int> PeriodicTaskOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            log = context.CreateReplaySafeLogger(log);

            var timesRun = context.GetInput<int>();
            timesRun++;

            log.LogInformation($"starting the periodic task activity {context.InstanceId}, {timesRun}");

            await context.CallActivityAsync(nameof(ActivityFunctions.PeriodicActivity), timesRun);

            var nextRun = context.CurrentUtcDateTime.AddSeconds(10);

            await context.CreateTimer(nextRun, CancellationToken.None);

            context.ContinueAsNew(timesRun);

            return timesRun;

        }
    }
}
