using FastReport.Export.Zpl;
using l2l_aggregator.Helpers.AggregationHelpers;
using l2l_aggregator.Helpers;
using l2l_aggregator.Services.Notification.Interface;
using MD.Devices;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using l2l_aggregator.Models;
using FastReport;
using l2l_aggregator.Services.Database;

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

        public void PrintReport(byte[] frxBytes)
        {
            if (_sessionService.PrinterModel == "Zebra")
            {
                PrintToZebraPrinter(frxBytes);
            }
            else
            {
                _notificationService.ShowMessage($"Модель принтера '{_sessionService.PrinterModel}' не поддерживается.");
            }
        }
        private byte[] GenerateZplFromReport(byte[] frxBytes)
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
                EXPIRE_DATE = _sessionService.SelectedTaskInfo.EXPIREDATE,
                SERIES_NAME = _sessionService.SelectedTaskInfo.SERIESNAME,
                PRINT_NAME = _sessionService.SelectedTaskInfo.RESOURCE_NAME,
                LEVEL_QTY = _sessionService.SelectedTaskInfo.QTY ?? 0,
                CNT = _sessionService.SelectedTaskInfo.RES_BOXID
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
        private void PrintToZebraPrinter(byte[] frxBytes)
        {
            byte[] zplBytes = GenerateZplFromReport(frxBytes);

            var config = PrinterConfigBuilder.Build(_sessionService.PrinterIP);
            var device = new PrinterTCP("TestCamera", logger);

            try
            {
                device.Configure(config);
                device.StartWork();
                _notificationService.ShowMessage("> Ожидание запуска...");

                WaitForState(device, DeviceStatusCode.Run, 10);
                _notificationService.ShowMessage("> Устройство запущено");

                // Отправляем экспортированный ZPL отчет на принтер
                device.Send(zplBytes);
                Thread.Sleep(1000);// подождать для завершения отправки
                _notificationService.ShowMessage($"> Состояние устройства: {device.Status}");


            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка печати: {ex.Message}");
            }
            finally
            {
                device.StopWork();
                _notificationService.ShowMessage("> Ожидание остановки...");
                WaitForState(device, DeviceStatusCode.Ready, 10);
                _notificationService.ShowMessage("> Работа с устройством остановлена ");

                _notificationService.ShowMessage("> Ждем остановки worker 2 сек...");
                Thread.Sleep(2000);

                _notificationService.ShowMessage($"> Устройство в состоянии {device.Status}.");
                _notificationService.ShowMessage("> Тест завершен");
            }
        }
        private static void WaitForState(Device device, DeviceStatusCode desiredState, int timeoutSeconds)
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
    }
}
