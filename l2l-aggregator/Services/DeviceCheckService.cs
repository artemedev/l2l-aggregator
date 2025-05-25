using DM_wraper_NS;
using l2l_aggregator.Services.DmProcessing;
using l2l_aggregator.Services.Printing;
using l2l_aggregator.Services.ScannerService.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace l2l_aggregator.Services
{
    public class DeviceCheckService
    {
        private readonly DmScanService _dmScanService;
        private readonly IScannerPortResolver _scannerPortResolver;
        private readonly PrintingService _printingService;

        public DeviceCheckService(DmScanService dmScanService, IScannerPortResolver scannerPortResolver, PrintingService printingService)
        {
            _dmScanService = dmScanService;
            _scannerPortResolver = scannerPortResolver;
            _printingService = printingService;
        }

        public async Task<(bool Success, string Message)> CheckCameraAsync(SessionService session)
        {
            if (!session.CheckCamera)
                return (true, null);

            if (string.IsNullOrWhiteSpace(session.CameraIP))
                return (false, "IP камеры не задан!");

            try
            {
                var recognParams = new recogn_params
                {
                    CamInterfaces = "GigEVision2",
                    cameraName = session.CameraIP,
                    _Preset = new camera_preset(session.CameraModel),
                    softwareTrigger = true,
                    hardwareTrigger = false,
                    DMRecogn = true,
                    OCRRecogn = true,
                    packRecogn = true
                };

                var success = _dmScanService.ConfigureParams(recognParams);
                if (!success)
                    return (false, "Не удалось настроить камеру");

                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка настройки камеры: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> CheckPrinterAsync(SessionService sessionService)
        {
            if (!sessionService.CheckPrinter)
                return (true, null);

            if (string.IsNullOrWhiteSpace(sessionService.PrinterIP))
                return (false, "IP принтера не задан!");

            try
            {
                _printingService.CheckConnectPrinter(sessionService.PrinterIP, sessionService.PrinterModel);
                return (true, null);
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка принтера: {ex.Message}");
            }

        }

        public async Task<(bool Success, string Message)> CheckControllerAsync(SessionService session)
        {
            if (!session.CheckController)
                return (true, null);

            if (string.IsNullOrWhiteSpace(session.ControllerIP))
                return (false, "IP контроллера не задан!");

            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var reply = await ping.SendPingAsync(session.ControllerIP, 300);
                return reply.Status == System.Net.NetworkInformation.IPStatus.Success
                    ? (true, null)
                    : (false, $"Контроллер {session.ControllerIP} недоступен!");
            }
            catch
            {
                return (false, $"Контроллер {session.ControllerIP} недоступен!");
            }
        }

        public Task<(bool Success, string Message)> CheckScannerAsync(SessionService session)
        {
            if (!session.CheckScanner)
                return Task.FromResult((true, (string)null));

            if (string.IsNullOrWhiteSpace(session.ScannerPort))
                return Task.FromResult((false, "Порт сканера не задан!"));

            if (session.ScannerModel != "Honeywell")
                return Task.FromResult((false, $"фывфывфывСканер модели '{session.ScannerModel}' не поддерживается."));

            var availablePorts = _scannerPortResolver.GetHoneywellScannerPorts();
            return Task.FromResult(availablePorts.Contains(session.ScannerPort)
                ? (true, (string)null)
                : (false, $"Сканер на порту {session.ScannerPort} недоступен!"));
        }
    }

}
