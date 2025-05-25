using FastReport;
using FastReport.Export.Zpl;
using l2l_aggregator.Helpers.AggregationHelpers;
using l2l_aggregator.Services.Notification.Interface;
using MD.Devices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace l2l_aggregator.Services.Printing
{
    public class PrintingService
    {
        private readonly INotificationService _notificationService;
        private readonly SessionService _sessionService;
        private ILogger logger;

        public PrintingService(
            INotificationService notificationService,
            SessionService sessionService)
        {
            _notificationService = notificationService;
            _sessionService = sessionService;
        }

        public void PrintReport(byte[] frxBytes, bool typePrint)
        {
            if (_sessionService.PrinterModel == "Zebra")
            {
                PrintToZebraPrinter(frxBytes, typePrint);
            }
            else
            {
                _notificationService.ShowMessage($"Модель принтера '{_sessionService.PrinterModel}' не поддерживается.");
            }
        }
        public void CheckConnectPrinter(string printerIP, string printerModel)
        {
            try
            {
                if (printerModel == "Zebra")
                {
                    ConnectToZebraPrinter(_sessionService.PrinterIP);
                }
                else
                {
                    _notificationService.ShowMessage($"Модель принтера '{_sessionService.PrinterModel}' не поддерживается.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка подключения к принтеру: {ex.Message}");
                throw;

            }
        }

        private PrinterTCP ConnectToZebraPrinter(string printerIP)
        {
            var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("PrinterTest");
            var config = PrinterConfigBuilder.Build(printerIP);
            var device = new PrinterTCP("TestCamera", logger);

            try
            {
                device.Configure(config);
                device.StartWork();
                _notificationService.ShowMessage("> Ожидание запуска...");
                WaitForState(device, DeviceStatusCode.Run, 10);
                _notificationService.ShowMessage("> Принтер успешно запущен");
                DisconnectToZebraPrinter(device);
                return device;
            }
            catch (Exception ex)
            {
                device.StopWork();
                throw;
            }
        }
        private void DisconnectToZebraPrinter(PrinterTCP device)
        {
            try
            {
                device.StopWork();
                _notificationService.ShowMessage("> Ожидание остановки...");
                WaitForState(device, DeviceStatusCode.Ready, 10);
                Thread.Sleep(2000);
                _notificationService.ShowMessage("> Принтер остановлен");
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка остановки принтера: {ex.Message}");
                throw;
            }
        }

        private void PrintToZebraPrinter(byte[] frxBytes, bool typePrint)
        {
            byte[] zplBytes;
            if (typePrint)
            {
                zplBytes = GenerateZplFromReportBOX(frxBytes);
            }
            else
            {
                zplBytes = GenerateZplFromReportPALLET(frxBytes);
            }
            PrinterTCP device = null;
            try
            {
                device = ConnectToZebraPrinter(_sessionService.PrinterIP);
                PrintZpl(device, zplBytes);
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка принтера: {ex.Message}");
            }
        }



        private void PrintZpl(PrinterTCP device, byte[] zplBytes)
        {
            try
            {
                device.Send(zplBytes);
                Thread.Sleep(1000); // подождать для завершения отправки
                _notificationService.ShowMessage($"> Состояние устройства: {device.Status}");
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка печати: {ex.Message}");
            }
            finally
            {
                DisconnectToZebraPrinter(device);
            }
        }
        private void WaitForState(Device device, DeviceStatusCode desiredState, int timeoutSeconds)
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);

            while (device.Status != desiredState)
            {
                if (DateTime.UtcNow - startTime > timeout)
                    throw new TimeoutException($"Устройство '{device.Name}' не достигло состояния {desiredState} за {timeoutSeconds} секунд.");

                Thread.Sleep(100);
            }
        }

        //перенести
        private byte[] GenerateZplFromReportBOX(byte[] frxBytes)
        {
            using var report = new Report();
            using (var ms = new MemoryStream(frxBytes))
            {
                report.Load(ms);
            }

            var labelData = new
            {
                DISPLAY_BAR_CODE = _sessionService.SelectedTaskSscc.DISPLAY_BAR_CODE,
                IN_BOX_QTY = _sessionService.SelectedTaskInfo.IN_BOX_QTY,
                MNF_DATE = _sessionService.SelectedTaskInfo.MNF_DATE_VAL,
                EXPIRE_DATE = _sessionService.SelectedTaskInfo.EXPIRE_DATE_VAL,
                SERIES_NAME = _sessionService.SelectedTaskInfo.SERIES_NAME,
                PRINT_NAME = _sessionService.SelectedTaskInfo.RESOURCE_NAME,
                LEVEL_QTY = 0,
                CNT = 0
                //LEVEL_QTY = _sessionService.SelectedTaskInfo.QTY ?? 0,
                //CNT = _sessionService.SelectedTaskInfo.RES_BOXID
            };
            // Регистрируем данные в отчете
            report.RegisterData(new List<object> { labelData }, "LabelQry");

            report.GetDataSource("LabelQry").Enabled = true;
            // Подготавливаем отчет
            report.Prepare();

            var exporter = new ZplExport();
            using var exportStream = new MemoryStream();
            exporter.Export(report, exportStream);

            return exportStream.ToArray();
        }
        private byte[] GenerateZplFromReportPALLET(byte[] frxBytes)
        {
            using var report = new Report();
            using (var ms = new MemoryStream(frxBytes))
            {
                report.Load(ms);
            }

            var labelData = new
            {
                DISPLAY_BAR_CODE = _sessionService.SelectedTaskSscc.DISPLAY_BAR_CODE,
                SERIES_NAME = _sessionService.SelectedTaskInfo.SERIES_NAME,
                RESOURCE_NAME = _sessionService.SelectedTaskInfo.RESOURCE_NAME,
                CNT = 0
                //LEVEL_QTY = _sessionService.SelectedTaskInfo.QTY ?? 0,
                //CNT = _sessionService.SelectedTaskInfo.RES_BOXID
            };
            // Регистрируем данные в отчете
            report.RegisterData(new List<object> { labelData }, "LabelQry");

            report.GetDataSource("LabelQry").Enabled = true;
            // Подготавливаем отчет
            report.Prepare();

            var exporter = new ZplExport();
            using var exportStream = new MemoryStream();
            exporter.Export(report, exportStream);

            return exportStream.ToArray();
        }
    }
}
