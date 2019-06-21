using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;

namespace RpiSurveillance.Functions
{
    public static class CleanupOldImages
    {
        private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(7);
        private static readonly TimeZoneInfo localTimeZone = TimeZoneInfo.FindSystemTimeZoneById(Environment.GetEnvironmentVariable("LocalTimeZone")); // eg. 'Central European Standard Time' on Windows

        [FunctionName("CleanupOldImages")]
        public static async Task Run(
            [TimerTrigger("0 */10 * * * *")] TimerInfo timer,
            ILogger log,
            CancellationToken cancellationToken)
        {
            log.LogInformation($"C# CleanupOldImages timer trigger function executed at: {DateTime.Now}");
            
            var account = StorageAccount.NewFromConnectionString(Environment.GetEnvironmentVariable("AzureWebJobsStorage"));
            var client = account.CreateCloudBlobClient();

            var expirationTime = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, localTimeZone).Subtract(RetentionPeriod);
            log.LogInformation($"Looking for pictures taken before {expirationTime}");

            var picsContainer = client.GetContainerReference("pics");
            int cleanedUp = 0;
            BlobContinuationToken continuationToken = null;
            BlobResultSegment result;
            do
            {
                result = await picsContainer.ListBlobsSegmentedAsync(null, true, BlobListingDetails.Metadata, 10, continuationToken, null, null, cancellationToken);
                var expiredItems = result.Results.Cast<CloudBlockBlob>().Where(i => ParseName(i.Name) <= expirationTime);
                var cleanupTasks = expiredItems.Select(async item =>
                {
                    log.LogInformation("Found expired picture: {0}", item.Name);
                    await item.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, null, null, null, cancellationToken);
                    Interlocked.Increment(ref cleanedUp);
                });

                await Task.WhenAll(cleanupTasks);

                continuationToken = result.ContinuationToken;
            } while (continuationToken != null);

            log.LogInformation("{0} pictures cleaned up", cleanedUp);
        }

        private static DateTimeOffset ParseName(string name)
        {
            var nameWithoutExt = name.Substring(0, name.IndexOf('.')); // remove extension
            var dateTime = DateTimeOffset.ParseExact(nameWithoutExt, "yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            var tzOffset = localTimeZone.GetUtcOffset(dateTime.DateTime);
            return new DateTimeOffset(dateTime.DateTime, tzOffset);
        }
    }
}