using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace RpiSurveillance.Functions
{
    public static class ProcessSurveillanceImage
    {
        [FunctionName("ProcessSurveillanceImage")]
        public static async Task Run(
            [BlobTrigger("pics/{name}")] Stream picStream,
            [Blob("latest-pic/latest.jpg", FileAccess.Write)] Stream latestPicture, 
            string name,
            CancellationToken cancellationToken,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request for file name {name}.");

            await picStream.CopyToAsync(latestPicture, 4096, cancellationToken);

            log.LogInformation($"Latest picture uploaded: {name} ({picStream.Position} bytes)");
        }
    }
}
