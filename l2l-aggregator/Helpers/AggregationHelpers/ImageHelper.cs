using Avalonia.Media.Imaging;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;

namespace l2l_aggregator.Helpers.AggregationHelpers
{
    public class ImageHelper
    {
        public Bitmap CropImage(Bitmap source, double xCell, double yCell, double sizeWidth, double sizeHeight, double scaleXObrat, double scaleYObrat, float angleDegrees)
        {
            // Шаг 1: масштабирование
            float x = (float)(xCell * scaleXObrat);
            float y = (float)(yCell * scaleYObrat);
            float width = (float)(sizeWidth * scaleXObrat);
            float height = (float)(sizeHeight * scaleYObrat);

            // Центр
            float centerX = x + width / 2f;
            float centerY = y + height / 2f;

            // Угол в радианах
            float radians = angleDegrees * (float)Math.PI / 180f; // минус – так же, как в BuildCellViewModels

            // Косинус и синус
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            // Вершины повернутого прямоугольника (от центра)
            SKPoint[] corners = new SKPoint[4];
            float halfW = width / 2f;
            float halfH = height / 2f;

            corners[0] = RotatePoint(centerX, centerY, -halfW, -halfH, cos, sin); // top-left
            corners[1] = RotatePoint(centerX, centerY, halfW, -halfH, cos, sin);  // top-right
            corners[2] = RotatePoint(centerX, centerY, halfW, halfH, cos, sin);   // bottom-right
            corners[3] = RotatePoint(centerX, centerY, -halfW, halfH, cos, sin);  // bottom-left

            // Вычисляем bounding box
            float minX = corners.Min(p => p.X);
            float maxX = corners.Max(p => p.X);
            float minY = corners.Min(p => p.Y);
            float maxY = corners.Max(p => p.Y);

            int bmpWidth = (int)Math.Ceiling(maxX - minX);
            int bmpHeight = (int)Math.Ceiling(maxY - minY);

            using var sourceSK = ConvertAvaloniaBitmapToSKBitmap(source);
            var result = new SKBitmap(bmpWidth, bmpHeight);

            using (var canvas = new SKCanvas(result))
            {
                canvas.Clear(SKColors.Transparent);

                // Переместим канвас, чтобы обрезка попала в (0,0)
                canvas.Translate(-minX, -minY);

                // Область обрезки как путь
                var clipPath = new SKPath();
                clipPath.MoveTo(corners[0]);
                clipPath.LineTo(corners[1]);
                clipPath.LineTo(corners[2]);
                clipPath.LineTo(corners[3]);
                clipPath.Close();

                canvas.ClipPath(clipPath);
                canvas.DrawBitmap(sourceSK, 0, 0);
            }

            return ConvertSKBitmapToAvaloniaBitmap(result);
        }

        private SKPoint RotatePoint(float cx, float cy, float dx, float dy, float cos, float sin)
        {
            // Поворачиваем точку (dx, dy) относительно центра (cx, cy)
            return new SKPoint(
                cx + dx * cos - dy * sin,
                cy + dx * sin + dy * cos
            );
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
