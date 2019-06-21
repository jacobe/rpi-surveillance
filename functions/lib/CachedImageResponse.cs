using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace RpiSurveillance.Functions.Lib
{
    public static class CachedImageResponse
    {
        public static HttpResponseMessage Create(Stream stream, TimeSpan maxAge)
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
                        MaxAge = maxAge
                    }
                }
            };
        }
    }
}