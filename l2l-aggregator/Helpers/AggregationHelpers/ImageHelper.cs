using Avalonia.Media.Imaging;
using DM_wraper_NS;
using l2l_aggregator.Models;
using l2l_aggregator.Services;
using l2l_aggregator.ViewModels;
using l2l_aggregator.ViewModels.VisualElements;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace l2l_aggregator.Helpers.AggregationHelpers
{
    public class ImageHelper
    {
        public Image<Rgba32> CropImage(Image<Rgba32> source, double xCell, double yCell, double sizeWidth, double sizeHeight, double scaleXObrat, double scaleYObrat, float angleDegrees)
        {
            float x = (float)(xCell * scaleXObrat);
            float y = (float)(yCell * scaleYObrat);
            float width = (float)(sizeWidth * scaleXObrat);
            float height = (float)(sizeHeight * scaleXObrat);

            // 1. Оптимизация для нулевого угла
            if (angleDegrees % 360 == 0)
            {
                var rect = new Rectangle((int)x, (int)y, (int)width, (int)height);
                return source.Clone(ctx => ctx.Crop(rect));
            }

            float centerX = x + width / 2f;
            float centerY = y + height / 2f;
            float radians = -angleDegrees * MathF.PI / 180f;

            // 2. Вычисляем bounding box для повернутой области
            float halfWidth = width / 2f;
            float halfHeight = height / 2f;
            float cos = MathF.Cos(radians);
            float sin = MathF.Sin(radians);

            // Углы относительно центра
            (float, float)[] corners =
            {
                (-halfWidth, -halfHeight),
                ( halfWidth, -halfHeight),
                ( halfWidth,  halfHeight),
                (-halfWidth,  halfHeight)
            };

            // Поворачиваем углы и находим границы
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var (dx, dy) in corners)
            {
                float xRot = dx * cos - dy * sin;
                float yRot = dx * sin + dy * cos;

                minX = Math.Min(minX, xRot);
                maxX = Math.Max(maxX, xRot);
                minY = Math.Min(minY, yRot);
                maxY = Math.Max(maxY, yRot);
            }

            float bbWidth = maxX - minX;
            float bbHeight = maxY - minY;

            // 3. Вычисляем координаты bounding box в исходном изображении
            float bbX = centerX + minX;
            float bbY = centerY + minY;

            // Корректировка границ
            bbX = Math.Clamp(bbX, 0, source.Width);
            bbY = Math.Clamp(bbY, 0, source.Height);
            bbWidth = Math.Clamp(bbWidth, 1, source.Width - bbX);
            bbHeight = Math.Clamp(bbHeight, 1, source.Height - bbY);

            // 4. Работаем только с нужной областью
            var boundingBox = new Rectangle((int)bbX, (int)bbY, (int)bbWidth, (int)bbHeight);
            using var tempImage = source.Clone(ctx => ctx.Crop(boundingBox));

            // 5. Поворачиваем только вырезанную область
            tempImage.Mutate(ctx => ctx.Rotate(-angleDegrees));

            // 6. Вычисляем центральную область после поворота
            int centerXTemp = tempImage.Width / 2;
            int centerYTemp = tempImage.Height / 2;
            var cropRect = new Rectangle(
                centerXTemp - (int)halfWidth,
                centerYTemp - (int)halfHeight,
                (int)width,
                (int)height
            );

            // 7. Корректируем итоговый прямоугольник
            cropRect.X = Math.Max(0, cropRect.X);
            cropRect.Y = Math.Max(0, cropRect.Y);
            cropRect.Width = Math.Min(cropRect.Width, tempImage.Width - cropRect.X);
            cropRect.Height = Math.Min(cropRect.Height, tempImage.Height - cropRect.Y);

            return tempImage.Clone(ctx => ctx.Crop(cropRect));
        }
        //public Image<Rgba32> CropImage(Image<Rgba32> source, double xCell, double yCell, double sizeWidth, double sizeHeight, double scaleXObrat, double scaleYObrat, float angleDegrees)
        //{
        //    float x = (float)(xCell * scaleXObrat);
        //    float y = (float)(yCell * scaleYObrat);
        //    float width = (float)(sizeWidth * scaleXObrat);
        //    float height = (float)(sizeHeight * scaleYObrat);

        //    float centerX = x + width / 2f;
        //    float centerY = y + height / 2f;

        //    float radians = -angleDegrees * (float)Math.PI / 180f;
        //    var rotateMatrix =
        //        Matrix3x2.CreateTranslation(-centerX, -centerY) *
        //        Matrix3x2.CreateRotation(radians) *
        //        Matrix3x2.CreateTranslation(centerX, centerY);

        //    var result = source.Clone(ctx =>
        //    {
        //        ctx.Transform(new AffineTransformBuilder().AppendMatrix(rotateMatrix));
        //        ctx.Crop(new Rectangle(
        //            (int)(centerX - width / 2f),
        //            (int)(centerY - height / 2f),
        //            (int)Math.Ceiling(width),
        //            (int)Math.Ceiling(height)
        //        ));
        //    });

        //    return result;
        //}

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

        public ObservableCollection<DmCellViewModel> BuildCellViewModels(
          in result_data dmrData,
          double scaleX, double scaleY,
          SessionService sessionService,
          ObservableCollection<TemplateField> fields,
          ArmJobSgtinResponse response,
          AggregationViewModel thisModel, int minX, int minY)
        {
            var cells = new ObservableCollection<DmCellViewModel>();
            //string json = BuildResultJson(dmrData);
            foreach (var dmd in dmrData.BOXs)
            {
                var dmVm = new DmCellViewModel(thisModel)
                {
                    X = (dmd.poseX - (dmd.width / 2) - minX) * scaleX,
                    Y = ((dmd.poseY - (dmd.height / 2) - minY) * scaleY),
                    SizeWidth = dmd.width * scaleX,
                    SizeHeight = dmd.height * scaleY,
                    Angle = -(dmd.alpha),
                    Dm_data = new DmSquareViewModel
                    {
                        X = (dmd.DM.poseX - (dmd.DM.width / 2)),
                        Y = (dmd.DM.poseY - (dmd.DM.height / 2)),
                        SizeWidth = dmd.DM.width,
                        SizeHeight = dmd.DM.height,
                        Angle = -dmd.DM.alpha,
                        IsValid = !dmd.DM.isError,
                        Data = dmd.DM.data
                    }
                };

                bool allValid = false;

                foreach (var ocr in dmd.OCR)
                {
                    var validBarcodes = new HashSet<string>();

                    var propSgtin = typeof(ArmJobSgtinRecord).GetProperty(ocr.Name);
                    if (propSgtin != null)
                    {
                        foreach (var r in response.RECORDSET)
                        {
                            var value = propSgtin.GetValue(r);
                            if (value is string str)
                                validBarcodes.Add(str);
                        }
                    }

                    if (propSgtin == null)
                    {
                        var propInfo = typeof(ArmJobInfoRecord).GetProperty(ocr.Name);
                        if (propInfo != null)
                        {
                            var val = propInfo.GetValue(sessionService.SelectedTaskInfo);
                            if (val != null)
                                validBarcodes.Add(val.ToString());
                        }
                    }

                    bool isValid = validBarcodes.Contains(ocr.Text);

                    dmVm.OcrCells.Add(new SquareCellViewModel
                    {
                        X = (ocr.poseX - (ocr.width / 2)),
                        Y = (ocr.poseY - (ocr.height / 2)),
                        SizeWidth = ocr.width,
                        SizeHeight = ocr.height,
                        Angle = ocr.alpha,
                        IsValid = isValid,
                        OcrName = ocr.Name,
                        OcrText = ocr.Text
                    });
                    //thisModel.OnCellClicked(dmVm);

                    if (!isValid)
                        allValid = false;
                }

                if (dmd.OCR.Count != fields.Count)
                {
                    allValid = false;
                }
                if (dmd.DM.isError)
                    allValid = false;
                //if (dmd.DM.data != null)
                //{
                //    dmVm.Dm_data = new DmSquareViewModel
                //    {
                //        X = dmd.DM.poseX,
                //        Y = dmd.DM.poseY,
                //        SizeWidth = dmd.DM.width,
                //        SizeHeight = dmd.DM.height,
                //        Angle = -dmd.DM.alpha,
                //        IsValid = !dmd.DM.isError,
                //        Data = dmd.DM.data
                //    };
                //    if (dmd.DM.isError)
                //        allValid = false;
                //}
                dmVm.IsValid = allValid;
                cells.Add(dmVm);
            }

            return cells;
        }


        public Image<Rgba32> GetCroppedImage(result_data dmrData, int minX, int minY, int maxX, int maxY)
        {
            int cropWidth = maxX - minX;
            int cropHeight = maxY - minY;

            if (dmrData.rawImage is Image<Rgba32> img)
            {
                try
                {
                    return img.Clone(ctx =>
                        ctx.Crop(new Rectangle(minX, minY, cropWidth, cropHeight))
                    );
                }
                catch
                {
                    return img.Clone(); // fallback в случае ошибки
                }
            }
            else
            {
                throw new InvalidCastException("rawImage не является Image<Rgba32>. Проверь тип в момент распознавания.");
            }
        }

        public void SaveRawImageAsJpeg(result_data dmrData, string outputPath, int quality = 90)
        {
            if (dmrData.rawImage == null)
                throw new InvalidOperationException("Изображение не загружено или результат сканирования отсутствует.");

            if (dmrData.rawImage is SixLabors.ImageSharp.Image<Rgba32> image)
            {
                var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
                {
                    Quality = quality
                };

                var dir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                image.Save(outputPath, encoder);
            }
            else
            {
                throw new InvalidCastException("rawImage не является Image<Rgba32> и не может быть сохранено в JPG.");
            }
        }
        public string BuildResultJson(result_data dmrData)
        {
            var result = new List<CellData>();
            int idCounter = 1;

            foreach (var cell in dmrData.BOXs)
            {
                var cellData = new CellData
                {
                    cell_id = idCounter++,
                    poseX = cell.poseX,
                    poseY = cell.poseY,
                    width = cell.width,
                    height = cell.height,
                    angle = cell.alpha,
                    cell_dm = new DM_data
                    {
                        data = cell.DM.data,
                        poseX = cell.DM.poseX,
                        poseY = cell.DM.poseY,
                        width = cell.DM.width,
                        height = cell.DM.height,
                        alpha = cell.DM.alpha,
                        isError = cell.DM.isError
                    },
                    cell_ocr = cell.OCR.Select(o => new OcrField
                    {
                        data = o.Text,
                        name = o.Name,
                        poseX = o.poseX,
                        poseY = o.poseY,
                        width = o.width,
                        height = o.height,
                        angle = o.alpha
                    }).ToList()
                };

                result.Add(cellData);
            }

            var json = JsonSerializer.Serialize(new { cells = result }, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return json;
        }

        public Bitmap ConvertToAvaloniaBitmap(Image<Rgba32> image)
        {
            using var ms = new MemoryStream();
            image.SaveAsBmp(ms);
            ms.Position = 0;
            return new Bitmap(ms);
        }

        public Image<Rgba32> ConvertFromImageSharpImage(SixLabors.ImageSharp.Image source)
        {
            return source.CloneAs<Rgba32>();
        }
        public class OcrField
        {
            public string data { get; set; }
            public string name { get; set; }
            public int poseX { get; set; }
            public int poseY { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public int angle { get; set; }
        }
        public class DM_data
        {
            public string data { get; set; }
            public int poseX { get; set; }
            public int poseY { get; set; }
            public int height { get; set; }
            public int width { get; set; }
            public int alpha { get; set; }
            public bool isError { get; set; }
        }
        public class CellData
        {
            public int cell_id { get; set; }
            public int poseX { get; set; }
            public int poseY { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public int angle { get; set; }
            public DM_data cell_dm { get; set; }
            public List<OcrField> cell_ocr { get; set; }
        }
    }
}
