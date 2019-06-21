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
using System.Text;

namespace RpiSurveillance.Functions
{
    public static class ProcessSurveillanceImage
    {
        const int WIDTH = 640, HEIGHT = 480;
        const int SCORE_THRESHOLD = 3;
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
            if (result.Score >= SCORE_THRESHOLD)
            {
                var text = $"{picture.Name}\n{result.Description}";
                using (var outputStream = await output.OpenWriteAsync())
                {
                    await ProcessPicture(text, result.Objects, picture, outputStream);
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

            log.LogInformation("Description: {0}", string.Join(", ", analysis.Description.Captions.Select(c => c.Text)));
            log.LogInformation("Tags:        {0}", string.Join(' ', analysis.Description.Tags));
            log.LogInformation("Faces:       {0}", string.Join(", ", analysis.Faces.Select(f => $"{f.Age}yo {f.Gender.ToString().ToLower()}")));
            log.LogInformation("Objects:     {0}", string.Join(", ", analysis.Objects.Select(o => o.ObjectProperty)));

            var description = new StringBuilder();
            description.AppendJoin(", ", analysis.Description.Captions.Select(c => c.Text));

            var matchingTags = analysis.Description.Tags.Intersect(TagsOfInterest).ToList();
            description.Append("\nTags: ");
            description.AppendJoin(", ", matchingTags);

            var objects = new List<ObjectRect>();
            foreach (var face in analysis.Faces)
            {
                var faceRect = face.FaceRectangle;
                var title = $"{face.Age}yo {face.Gender.ToString().ToLower()}";
                objects.Add(new ObjectRect(faceRect.Left, faceRect.Top, faceRect.Width, faceRect.Height, title, new Rgba32(255, 255, 0)));
            }

            foreach (var person in analysis.Objects.Where(o => o.ObjectProperty == "person"))
            {
                var personRect = person.Rectangle;
                objects.Add(new ObjectRect(personRect.X, personRect.Y, personRect.W, personRect.H, person.ObjectProperty, new Rgba32(0, 255, 255)));
            }

            int score = matchingTags.Count + objects.Count * 10;
            return new AnalysisResult
            {
                Score = score,
                Description = description.ToString(),
                Objects = objects
            };
        }

        private static async Task ProcessPicture(string caption, List<ObjectRect> objectRects, CloudBlockBlob picture, Stream outputStream)
        {
            using (var memStream = new MemoryStream())
            {
                await picture.DownloadToStreamAsync(memStream);
                memStream.Position = 0;

                var image = Image.Load(memStream);
                image.Mutate(i =>
                {
                    foreach (var rect in objectRects)
                    {
                        var points = new[]
                        {
                            new PointF(rect.X, rect.Y),
                            new PointF(rect.X + rect.W, rect.Y),
                            new PointF(rect.X + rect.W, rect.Y + rect.H),
                            new PointF(rect.X, rect.Y + rect.H)
                        };
                        i.DrawPolygon(rect.Color, 2, points);
                        i.DrawText(rect.Title, Font, rect.Color, new PointF(rect.X + 10, rect.Y + 10));
                    }

                    i.DrawText(caption, Font, new Rgba32(255, 255, 255), new PointF(10, 10));
                });

                image.SaveAsJpeg(outputStream, new JpegEncoder { Quality = 25 });
            }
        }
    }

    class AnalysisResult
    {
        public int Score { get; set; }
        public string Description { get; set; }
        public List<ObjectRect> Objects { get; set; }
    }

    class ObjectRect
    {
        public ObjectRect(int x, int y, int w, int h, string title, Rgba32 color)
        {
            X = x;
            Y = y;
            W = w;
            H = h;
            Title = title;
            Color = color;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public string Title { get; set; }
        public Rgba32 Color { get; set; }
    }
}
