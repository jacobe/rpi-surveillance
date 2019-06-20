using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.ImageSharp.PixelFormats;

namespace RpiSurveillance.Functions
{
    public static class ProcessSurveillanceImage
    {
        const int WIDTH = 640, HEIGHT = 480;
        static readonly Font Font = SixLabors.Fonts.SystemFonts.Find("Verdana").CreateFont(14);

        [FunctionName("ProcessSurveillanceImage")]
        public static async Task Run(
            [BlobTrigger("pics/{name}")] CloudBlockBlob picture,
            [Blob("latest-pic/latest.jpg", FileAccess.Write)] Stream output, 
            string name,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function processed a request for file name {name}.");
            await ProcessAndUpload(picture, output);
            log.LogInformation($"Latest picture uploaded: {name} ({output.Position} bytes)");
        }

        private static async Task ProcessAndUpload(CloudBlockBlob picture, Stream output)
        {
            using (var memStream = new MemoryStream())
            {
                await picture.DownloadToStreamAsync(memStream);
                memStream.Position = 0;

                var image = Image.Load(memStream);
                image.Mutate(i =>
                {
                    i.Resize(new ResizeOptions{
                        Size = new SixLabors.Primitives.Size {
                            Width = WIDTH,
                            Height = HEIGHT
                        },
                        Mode = ResizeMode.Max
                    })
                    .DrawText(picture.Name, Font, new Rgba32(255, 255, 255), new PointF(10, 10));
                });

                image.SaveAsJpeg(output);
            }
        }
    }
}
