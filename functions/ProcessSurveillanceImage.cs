using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Collections.Generic;

namespace RpiSurveillance.Functions
{
    public static class ProcessSurveillanceImage
    {
        const int WIDTH = 640, HEIGHT = 480;
        const int MATCH_THRESHOLD = 3;
        static readonly Font Font = SixLabors.Fonts.SystemFonts.Find("Verdana").CreateFont(14);
        static readonly string ComputerVisionApiKey = Environment.GetEnvironmentVariable("ComputerVisionApiKey");
        static readonly string StorageSasToken = Environment.GetEnvironmentVariable("StorageSasToken");

        private static readonly VisualFeatureTypes[] features =
        {
            VisualFeatureTypes.Description,
            VisualFeatureTypes.Faces,
            VisualFeatureTypes.Objects
        };

        private static readonly string[] TagsOfInterest =
        {
            "man", "woman", "person", "girl", "boy", "human",
            "group", "people"
        };


        [FunctionName("ProcessSurveillanceImage")]
        public static async Task Run(
            [BlobTrigger("pics/{name}")] CloudBlockBlob picture,
            [Blob("latest-pic/latest.jpg", FileAccess.ReadWrite)] CloudBlockBlob output, 
            string name,
            ILogger log)
        {
            log.LogInformation($"C# Blob trigger function processed a request for file name {name}.");
            log.LogDebug("Running computer vision");

            var result = await AnalyzePicture(picture, log);
            if (result.Matches >= MATCH_THRESHOLD)
            {
                var text = $"{picture.Name}\n{result.Description}";
                using (var outputStream = await output.OpenWriteAsync())
                {
                    await ProcessPicture(text, result.Persons.Select(b => b.Rectangle).ToList(), picture, outputStream);
                    log.LogInformation($"Latest picture uploaded: {name} ({outputStream.Position} bytes)");
                    await outputStream.CommitAsync();
                }
            }
        }

        private static async Task<AnalysisResult> AnalyzePicture(CloudBlockBlob picture, ILogger log)
        {
            ComputerVisionClient computerVision = new ComputerVisionClient(new ApiKeyServiceClientCredentials(ComputerVisionApiKey))
            {
                Endpoint = "https://westcentralus.api.cognitive.microsoft.com"
            };

            ImageAnalysis analysis = await computerVision.AnalyzeImageAsync($"{picture.Uri}?{StorageSasToken}", features);

            var description = string.Join(", ", analysis.Description.Captions.Select(c => c.Text));
            log.LogInformation("Description: {0}", description);
            log.LogInformation("Tags: {0}", string.Join(' ', analysis.Description.Tags));
            log.LogInformation("Faces: {0}", string.Join(", ", analysis.Faces.Select(f => f.Age + "yo " + f.Gender)));
            log.LogInformation("Objects: {0}", string.Join(", ", analysis.Objects.Select(o => o.ObjectProperty)));

            int matches = 0;
            matches += analysis.Description.Tags.Intersect(TagsOfInterest).Count();
            matches += analysis.Faces.Count() * 10;
            var persons = analysis.Objects.Where(o => o.ObjectProperty == "person").ToList();
            matches += persons.Count * 10;

            return new AnalysisResult
            {
                Matches = matches,
                Description = !string.IsNullOrEmpty(description) ? Char.ToUpper(description[0]) + description.Substring(1) : string.Empty,
                Persons = persons
            };
        }

        private static async Task ProcessPicture(string caption, List<BoundingRect> boxes, CloudBlockBlob picture, Stream outputStream)
        {
            using (var memStream = new MemoryStream())
            {
                await picture.DownloadToStreamAsync(memStream);
                memStream.Position = 0;

                var image = Image.Load(memStream);
                image.Mutate(i =>
                {
                    foreach (var box in boxes)
                    {
                        var points = new[]
                        {
                            new PointF(box.X, box.Y),
                            new PointF(box.X + box.W, box.Y),
                            new PointF(box.X + box.W, box.Y + box.H),
                            new PointF(box.X, box.Y + box.H)
                        };
                        i.DrawPolygon(new Rgba32(0, 255, 255), 2, points);
                    }

                    i.Resize(new ResizeOptions{
                        Size = new SixLabors.Primitives.Size {
                            Width = WIDTH,
                            Height = HEIGHT
                        },
                        Mode = ResizeMode.Max
                    });

                    i.DrawText(caption, Font, new Rgba32(255, 255, 255), new PointF(10, 10));
                });

                image.SaveAsJpeg(outputStream, new JpegEncoder { Quality = 25 });
            }
        }
    }

    class AnalysisResult
    {
        public int Matches { get; set; }
        public string Description { get; set; }
        public List<DetectedObject> Persons { get; set; }
    }
}
