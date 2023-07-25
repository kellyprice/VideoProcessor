using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VideoProcessor
{
    public class ActivityFunctions
    {
        [FunctionName(nameof(TranscodeVideo))]
        public static async Task<string> TranscodeVideo([ActivityTrigger] string inputVideo, ILogger log)
        {
            log.LogInformation($"Transcoding {inputVideo}.");
            await Task.Delay(5000);
            return $"{inputVideo}-transcoded.mp4";
        }

        [FunctionName(nameof(TranscodeVideoWithBitRate))]
        public static async Task<VideoFileInfo> TranscodeVideoWithBitRate([ActivityTrigger] VideoFileInfo inputVideo, ILogger log)
        {
            log.LogInformation($"Transcoding {inputVideo.Location} to {inputVideo.BitRate}.");
            await Task.Delay(inputVideo.BitRate + 1000);
            var transcodedLocation = $"{inputVideo.Location}-{inputVideo.BitRate}kbps.mp4";
            return new VideoFileInfo
            {
                Location = transcodedLocation,
                BitRate = inputVideo.BitRate,
            };
        }

        [FunctionName(nameof(GetTranscodeBitRates))]
        public static int[] GetTranscodeBitRates([ActivityTrigger] object input)
        {
            return new int[] { 1000, 2000, 3000, 4000 };
        }

        [FunctionName(nameof(ExtractThumbnail))]
        public static async Task<string> ExtractThumbnail([ActivityTrigger] string inputVideo, ILogger log)
        {
            log.LogInformation($"Extracting thumbnail {inputVideo}.");

            if (inputVideo.Contains("error"))
            {
                throw new InvalidOperationException("Failed to extract thumbnail");
            }

            await Task.Delay(5000);
            return $"{inputVideo}-thumbnail.mp4";
        }

        [FunctionName(nameof(PrependIntro))]
        public static async Task<string> PrependIntro([ActivityTrigger] string inputVideo, ILogger log)
        {
            log.LogInformation($"Prepending intro to {inputVideo}.");
            await Task.Delay(5000);
            return $"{inputVideo}-withintro.mp4";
        }

        [FunctionName(nameof(Cleanup))]
        public static async Task<string> Cleanup([ActivityTrigger] string[] files, ILogger log)
        {
            foreach (var file in files.Where(x => !string.IsNullOrEmpty(x)))
            {
                log.LogInformation($"deleting {file}");

                await Task.Delay(1000);
            }

            return $"Cleaned up successsfully";
        }

        [FunctionName(nameof(SendApprovalRequestEmail))]
        public static async Task SendApprovalRequestEmail([ActivityTrigger] string inputVideo, ILogger log)
        {

            log.LogInformation($"sending approval email for {inputVideo}");

            await Task.Delay(1000);
        }

        [FunctionName(nameof(PublishVideo))]
        public static async Task PublishVideo([ActivityTrigger] string inputVideo, ILogger log)
        {

            log.LogInformation($"Publishing {inputVideo}");

            await Task.Delay(1000);
        }

        [FunctionName(nameof(RejectVideo))]
        public static async Task RejectVideo([ActivityTrigger] string inputVideo, ILogger log)
        {

            log.LogInformation($"Rejecting {inputVideo}");

            await Task.Delay(1000);
        }

        [FunctionName(nameof(PeriodicActivity))]
        public static void PeriodicActivity([ActivityTrigger] int timesRun, ILogger log)
        {

            log.LogWarning($"Running the periodic activity, times run = {timesRun}");
        }
    }
}
