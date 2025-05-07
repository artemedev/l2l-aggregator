using Avalonia.Media.Imaging;
using DM_process_NS;
using DM_wraper_NS;
using l2l_aggregator.Models;
using l2l_aggregator.ViewModels;
using l2l_aggregator.ViewModels.VisualElements;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            string filePath = "./tmp/template.xml"; // Или .fr3 — в зависимости от вашего формата

            SaveXmlToFile(base64Template, filePath);
            _dmrDataReady = new TaskCompletionSource<bool>();
            _recognWrapper.SendPrintPatternXML(base64Template);
            _DMP.update_PP();
            //_recognWrapper.SendShotFrameComand();
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

        public Bitmap GetCroppedImage(result_data dmrData)
        {
            int minX = dmrData.BOXs.Min(d => d.poseX);
            int minY = dmrData.BOXs.Min(d => d.poseY);
            int maxX = dmrData.BOXs.Max(d => d.poseX + d.width);
            int maxY = dmrData.BOXs.Max(d => d.poseY + d.height);

            using var ms = new MemoryStream();
            using var cropped = dmrData.processedImage.Clone(ctx => ctx.Crop(new SixLabors.ImageSharp.Rectangle(minX, minY, maxX - minX, maxY - minY)));
            cropped.SaveAsBmp(ms);
            ms.Seek(0, SeekOrigin.Begin);

            return new Bitmap(ms);
        }

        public ObservableCollection<DmCellViewModel> BuildCellViewModels(
            result_data dmrData,
            double scaleX, double scaleY,
            SessionService sessionService,
            ArmJobSgtinResponse response,
            AggregationViewModel thisModel)
        {
            var cells = new ObservableCollection<DmCellViewModel>();

            int minX = dmrData.BOXs.Min(d => d.poseX);
            int minY = dmrData.BOXs.Min(d => d.poseY);

            foreach (var dmd in dmrData.BOXs)
            {
                var dmVm = new DmCellViewModel(thisModel)
                {
                    X = (dmd.poseX - minX) * scaleX,
                    Y = (dmd.poseY - minY) * scaleY,
                    SizeWidth = dmd.width * scaleX,
                    SizeHeight = dmd.height * scaleY,
                    Angle = dmd.alpha
                };

                bool allValid = true;

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

                dmVm.IsValid = allValid;
                cells.Add(dmVm);
            }

            return cells;
        }
    }
}
