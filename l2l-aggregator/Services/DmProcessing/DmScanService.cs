using Avalonia.Media.Imaging;
using DM_process_lib;
using DM_wraper_NS;
using l2l_aggregator.Helpers.AggregationHelpers;
using l2l_aggregator.Models;
using l2l_aggregator.ViewModels;
using l2l_aggregator.ViewModels.VisualElements;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.DmProcessing
{
    public class DmScanService
    {
        private readonly DM_recogn_wraper _recognWrapper;
        private readonly DM_process _dmProcess;
        private static TaskCompletionSource<bool> _dmrDataReady;

        public DmScanService()
        {
            _recognWrapper = new DM_recogn_wraper();
            _recognWrapper.Init();
            _recognWrapper.swNewDMResult += OnNewResult;

            _dmProcess = new DM_process();
            _dmProcess.Init(_recognWrapper);
        }

        private result_data _dmrData;

        public void StartScan(string base64Template)
        {
            string filePath = "./tmp/template.xml"; // Или .fr3 — в зависимости от формата

            //SaveXmlToFile(base64Template, filePath);
            _dmrDataReady = new TaskCompletionSource<bool>();
            _recognWrapper.SendPrintPatternXML(base64Template);
            _DMP.update_PP();
        }
        public void SaveXmlToFile(string xmlContent, string filePath)
        {
            try
            {
                // Убедимся, что директория существует
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Записываем в UTF-8 без BOM — для Linux это стандарт
                File.WriteAllText(filePath, xmlContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Ошибка при сохранении XML-файла: {ex.Message}");
                throw;
            }
        }
        public bool ConfigureParams(recogn_params parameters)
        {
            return _recognWrapper.SetParams(parameters);
        }
        public void StopScan()
        {
            _recognWrapper.SendStopShotComand();
        }
        public void getScan()
        {
            _dmrDataReady = new TaskCompletionSource<bool>();
            _recognWrapper.SendStartShotComand();
            _recognWrapper.SendShotFrameComand();
        }
        public async Task<result_data> WaitForResultAsync()
        {
            await _dmrDataReady.Task;
            return _dmrData;
        }

        private void OnNewResult(int countResult)
        {
            _dmrData = _recognWrapper.GetDMResult();
            _dmrDataReady.TrySetResult(true);
        }

        public async Task<Bitmap> GetCroppedImage(result_data dmrData, int minX, int minY, int maxX, int maxY)
        {
            return await Task.Run(() =>
            {
                //int imageWidth = dmrData.rawImage.Width;
                //int imageHeight = dmrData.rawImage.Height;

                //// Увеличиваем зону обрезки на 20 пикселей с каждой стороны
                //minX = Math.Max(0, minX - 20);
                //minY = Math.Max(0, minY - 20);
                //maxX = Math.Min(imageWidth, maxX + 20);
                //maxY = Math.Min(imageHeight, maxY + 20);

                int cropWidth = maxX - minX;
                int cropHeight = maxY - minY;

                try
                {
                    using var ms = new MemoryStream();
                    dmrData.rawImage.SaveAsBmp(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    return new Bitmap(ms);
                }
                catch
                {
                    using var ms = new MemoryStream();
                    dmrData.rawImage.SaveAsBmp(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    return new Bitmap(ms);
                }
            });
        }

        public ObservableCollection<DmCellViewModel> BuildCellViewModels(
            result_data dmrData,
            double scaleX, double scaleY,
            SessionService sessionService,
            ObservableCollection<TemplateField> fields,
            ArmJobSgtinResponse response,
            AggregationViewModel thisModel, int minX, int minY)
        {
            var cells = new ObservableCollection<DmCellViewModel>();
            string json = BuildResultJson(dmrData);
            //foreach (var dmd in dmrData.BOXs)
            //{
            var boo = dmrData.BOXs.First();
            var dmVm = new DmCellViewModel(thisModel)
            {
                X = (boo.poseX - (boo.width / 2)) * scaleX,
                Y = (boo.poseY - (boo.height / 2)) * scaleY,
                SizeWidth = boo.width * scaleX,
                SizeHeight = boo.height * scaleY,
                Angle = -(boo.alpha)
            };

            bool allValid = false;

            foreach (var ocr in boo.OCR)
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
                    X = ocr.poseX,
                    Y = ocr.poseY,
                    SizeWidth = ocr.width,
                    SizeHeight = ocr.height,
                    Angle = ocr.alpha,
                    IsValid = isValid
                });

                if (!isValid)
                    allValid = false;
            }

            if (boo.OCR.Count != fields.Count)
            {
                allValid = false;
            }
            if (boo.DM.data != null)
            {
                dmVm.OcrCells.Add(new SquareCellViewModel
                {
                    X = boo.DM.poseX,
                    Y = boo.DM.poseY,
                    SizeWidth = boo.DM.width,
                    SizeHeight = boo.DM.height,
                    Angle = -boo.DM.alpha,
                    IsValid = !boo.DM.isError
                });
                if (boo.DM.isError)
                    allValid = false;
            }
            dmVm.IsValid = allValid;
            cells.Add(dmVm);
            //}

            return cells;
        }
        public void SaveRawImageAsJpeg(result_data dmrData, string outputPath, int quality = 90)
        {
            //if (_dmrData.rawImage == null)
            //    throw new InvalidOperationException("Изображение не загружено или результат сканирования отсутствует.");

            //using var image = dmrData.rawImage.Clone(); // Создаем копию
            //var encoder = new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder
            //{
            //    Quality = quality
            //};

            //// Убедимся, что путь существует
            //var dir = Path.GetDirectoryName(outputPath);
            //if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            //    Directory.CreateDirectory(dir);

            //image.Save(outputPath, encoder);
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

        public class CellData
        {
            public int cell_id { get; set; }
            public int poseX { get; set; }
            public int poseY { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public int angle { get; set; }
            public List<OcrField> cell_ocr { get; set; }
        }
    }
}
