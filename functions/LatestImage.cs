using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace RpiSurveillance.Functions
{
    public static class LatestImage
    {
        [FunctionName("LatestImage")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger] HttpRequest request,
            [Blob("latest-pic/latest.jpg", FileAccess.Read)] Stream latestPic,
            ILogger log)
        {
            log.LogInformation($"C# HTTP trigger function processed a request.");
            var memStream = new MemoryStream();
            await latestPic.CopyToAsync(memStream);
            memStream.Position = 0;

            return CachedImageResponse(memStream);
        }

        private static HttpResponseMessage CachedImageResponse(Stream stream)
        {
            return new HttpResponseMessage
            {
                Content = new StreamContent(stream)
                {
                    Headers = {
                        ContentType = new MediaTypeHeaderValue("image/jpeg")
                    }
                },
                Headers =
                {
                    CacheControl = new CacheControlHeaderValue
                    {
                        Private = true,
                        MaxAge = TimeSpan.FromMinutes(1)
                    }
                }
            };
        }
    }
}