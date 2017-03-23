#r "System.Drawing"

using ImageResizer;
using System.Drawing;
using System.Drawing.Imaging;

public static void Run(Stream inputImage, string imageName, Stream resizedImage, TraceWriter log)
{
    log.Info($"C# Blob trigger function Processed blob\n Name:{imageName} \n Size: {inputImage.Length} Bytes");

    if (inputImage.Length > 4000000)
    {
        var settings = new ImageResizer.ResizeSettings
        {
            MaxWidth = 500,
            Format = "png"
        };

        ImageResizer.ImageBuilder.Current.Build(inputImage, resizedImage, settings);
        log.Info($"C# Blob trigger function resized the Image!");
    }

}