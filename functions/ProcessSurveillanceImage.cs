using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace RpiSurveillance.Functions
{
    public static class ProcessSurveillanceImage
    {
        const int WIDTH = 640, HEIGHT = 480;

        [FunctionName("ProcessSurveillanceImage")]
        public static async Task Run(
            [BlobTrigger("pics/{name}")] CloudBlockBlob picture,
            [Blob("latest-pic/latest.jpg", FileAccess.Write)] Stream output, 
            string name,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function processed a request for file name {name}.");

            log.LogDebug("Resizing picture");
            using (var memStream = new MemoryStream())
            {
                await picture.DownloadToStreamAsync(memStream);
                memStream.Position = 0;
                Resize(memStream, output);

                log.LogInformation($"Latest picture uploaded: {name} ({output.Position} bytes)");
            }
        }

        private static void Resize(Stream input, Stream output)
        {
            var image = Image.Load(input);
            image.Mutate(i =>
                i.Resize(new ResizeOptions{
                    Size = new SixLabors.Primitives.Size {
                        Width = WIDTH,
                        Height = HEIGHT
                    },
                    Mode = ResizeMode.Max
                }));
            image.SaveAsJpeg(output);
        }
    }
}
