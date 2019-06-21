using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using RpiSurveillance.Functions.Lib;

namespace RpiSurveillance.Functions
{
    public static class LatestImage
    {
        [FunctionName("LatestImage")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(Route = "image/latest")] HttpRequest request,
            [Blob("latest-pic/latest.jpg", FileAccess.Read)] Stream latestPic,
            ILogger log)
        {
            log.LogInformation($"C# LatestImage HTTP trigger function processed a request.");
            var memStream = new MemoryStream();
            await latestPic.CopyToAsync(memStream);
            memStream.Position = 0;
            return CachedImageResponse.Create(memStream, TimeSpan.FromSeconds(10));
        }
    }
}