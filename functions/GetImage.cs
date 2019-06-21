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
    public static class GetImage
    {
        [FunctionName("GetImage")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(Route = "image/{id}.jpg")] HttpRequest request,
            [Blob("pics/{id}.jpg", FileAccess.Read)] Stream picStream,
            string id,
            ILogger log)
        {
            log.LogInformation($"C# GetImage HTTP trigger function processed a request for picture id {id}");
            var memStream = new MemoryStream();
            await picStream.CopyToAsync(memStream);
            memStream.Position = 0;
            return CachedImageResponse.Create(memStream, TimeSpan.FromHours(1));
        }
    }
}