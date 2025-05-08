using Avalonia.Media.Imaging;
using SkiaSharp;
using System.IO;

namespace l2l_aggregator.Helpers.AggregationHelpers
{
    public class ImageHelper
    {
        public Bitmap CropImage(Bitmap source, double xCell, double yCell, double sizeWidth, double sizeHeight, double scaleXObrat, double scaleYObrat)
        {
            int cellWidth = (int)(sizeWidth * scaleXObrat);
            int cellHeight = (int)(sizeHeight * scaleYObrat);

            int x = (int)((xCell - (sizeWidth / 2)) * scaleXObrat);
            int y = (int)((yCell - (sizeHeight / 2)) * scaleYObrat);


            var rect = new SKRectI(x, y, x + cellWidth, y + cellHeight);
            using var sourceSK = ConvertAvaloniaBitmapToSKBitmap(source);
            var croppedSK = new SKBitmap(cellWidth, cellHeight);

            using (var canvas = new SKCanvas(croppedSK))
            {
                var destRect = new SKRectI(0, 0, cellWidth, cellHeight);
                canvas.DrawBitmap(sourceSK, rect, destRect);
            }

            return ConvertSKBitmapToAvaloniaBitmap(croppedSK);
        }

        public Bitmap ConvertSKBitmapToAvaloniaBitmap(SKBitmap skBitmap)
        {
            using var image = SKImage.FromPixels(skBitmap.PeekPixels());
            using var data = image.Encode();
            var bytes = data.ToArray();

            using var ms = new MemoryStream(bytes);
            return new Bitmap(ms);
        }

        public SKBitmap ConvertAvaloniaBitmapToSKBitmap(Bitmap avaloniaBitmap)
        {
            using var ms = new MemoryStream();
            avaloniaBitmap.Save(ms);
            ms.Position = 0;
            return SKBitmap.Decode(ms);
        }
    }
}
