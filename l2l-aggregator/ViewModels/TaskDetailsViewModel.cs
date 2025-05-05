using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastReport.Utils;
using l2l_aggregator.Helpers;
using l2l_aggregator.Helpers.AggregationHelpers;
using l2l_aggregator.Models;
using l2l_aggregator.Services;
using l2l_aggregator.Services.Api;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Notification;
using l2l_aggregator.Services.Notification.Interface;
using MD.Devices;
using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace l2l_aggregator.ViewModels
{
    public partial class TaskDetailsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ArmJobInfoRecord _task;

        [ObservableProperty]
        private string _infoMessage;

        private readonly DatabaseService _databaseService;
        private readonly HistoryRouter<ViewModelBase> _router;
        private readonly SessionService _sessionService;
        private readonly DataApiService _dataApiService;
        private readonly INotificationService _notificationService;

        public TaskDetailsViewModel(HistoryRouter<ViewModelBase> router, DatabaseService databaseService, SessionService sessionService, DataApiService dataApiService, INotificationService notificationService)
        {
            _dataApiService = dataApiService;
            _databaseService = databaseService;
            _router = router;
            _sessionService = sessionService;
            _notificationService = notificationService;

            //Task = _sessionService.SelectedTask;
            LoadTaskAsync(_sessionService.SelectedTask.DOCID);
        }
        private async Task LoadTaskAsync(long docId)
        {
            try
            {
                var serverUri = await _databaseService.Config.GetConfigValueAsync("ServerUri");
                if (string.IsNullOrWhiteSpace(serverUri))
                {
                    InfoMessage = "Сервер не настроен!";
                    _notificationService.ShowMessage(InfoMessage);

                    return;
                }
                //HttpClientHandler httpClientHandler = new HttpClientHandler();
                //httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                //var client = RestService.For<ITaskApi>(new HttpClient(httpClientHandler)
                //{
                //    BaseAddress = new Uri(serverUri)
                //});
                var request = new ArmJobInfoRequest { docid = docId };
                //ArmJobInfoResponse response = await _dataApiService.GetJobDetailsAsync(docId);
                (var jobInfo, var ssccInfo) = await _dataApiService.GetJobDetailsAsync(docId);
                //await client.GetJob(request);
                Task = jobInfo;
                //_sessionService.SelectedTaskInfo = response.RECORDSET.FirstOrDefault();
                _sessionService.SelectedTaskInfo = jobInfo;
                _sessionService.SelectedTaskSscc = ssccInfo;
                //var requestSscc = new ArmJobSsccRequest { docid = docId };
                //ArmJobSsccResponse respSscc = await _dataApiService.GetSsccAsync(docId);
                //await client.GetJobSscc(requestSscc);
                //_sessionService.SelectedTaskSscc = respSscc.RECORDSET.FirstOrDefault();
            }
            catch (Exception ex)
            {
                InfoMessage = $"Ошибка: {ex.Message}";
                _notificationService.ShowMessage(InfoMessage);

            }
        }

        [RelayCommand]
        public void GoBack()
        {
            // Переход на страницу назад
            _router.GoTo<TaskListViewModel>();
        }

        //[RelayCommand]
        //public void GoAggregation()
        //{
        //    Debug.WriteLine("[APP] Переход на AggregationViewModel...");
        //    // Переход на страницу агрегации
        //    _router.GoTo<AggregationViewModel>();
        //}
        //[RelayCommand]
        //public async Task GoAggregationAsync()
        //{
        //    var session = SessionService.Instance;
        //    // Проверка камеры
        //    if (SessionService.Instance.CheckCamera)
        //    {
        //        if (string.IsNullOrWhiteSpace(SessionService.Instance.CameraIP))
        //        {
        //            InfoMessage = "IP камеры не задан!";
        //            _notificationService.ShowMessage(InfoMessage);
        //            return;
        //        }

        //        bool cameraReachable = await PingDeviceAsync(SessionService.Instance.CameraIP);
        //        if (!cameraReachable)
        //        {
        //            InfoMessage = $"Камера {SessionService.Instance.CameraIP} недоступна!";
        //            _notificationService.ShowMessage(InfoMessage);
        //            return;
        //        }
        //    }

        //    // Проверка принтера
        //    if (SessionService.Instance.CheckPrinter)
        //    {
        //        if (string.IsNullOrWhiteSpace(SessionService.Instance.PrinterIP))
        //        {
        //            InfoMessage = "IP принтера не задан!";
        //            _notificationService.ShowMessage(InfoMessage);
        //            return;
        //        }

        //        try
        //        {
        //            if (SessionService.Instance.PrinterModel == "Zebra")
        //            {
        //                var config = PrinterConfigBuilder.Build(SessionService.Instance.PrinterIP);
        //                var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("PrinterCheck");
        //                var device = new PrinterTCP("PreAggPrinter", logger);
        //                device.Configure(config);

        //                device.StartWork();
        //                DeviceHelper.WaitForState(device, DeviceStatusCode.Run, 10);
        //                device.StopWork();
        //                DeviceHelper.WaitForState(device, DeviceStatusCode.Ready, 10);
        //            }
        //            else
        //            {
        //                InfoMessage = $"Принтер модели '{SessionService.Instance.PrinterModel}' не поддерживается.";
        //                _notificationService.ShowMessage(InfoMessage);
        //                return;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            InfoMessage = $"Ошибка соединения с принтером: {ex.Message}";
        //            _notificationService.ShowMessage(InfoMessage);
        //            return;
        //        }
        //    }

        //    // Проверка контроллера
        //    if (SessionService.Instance.CheckController)
        //    {
        //        if (string.IsNullOrWhiteSpace(SessionService.Instance.ControllerIP))
        //        {
        //            InfoMessage = "IP контроллера не задан!";
        //            _notificationService.ShowMessage(InfoMessage);
        //            return;
        //        }

        //        bool controllerReachable = await PingDeviceAsync(SessionService.Instance.ControllerIP);
        //        if (!controllerReachable)
        //        {
        //            InfoMessage = $"Контроллер {SessionService.Instance.ControllerIP} недоступен!";
        //            _notificationService.ShowMessage(InfoMessage);
        //            return;
        //        }
        //    }

        //    // Проверка сканера
        //    if (SessionService.Instance.CheckScanner)
        //    {
        //        if (string.IsNullOrWhiteSpace(SessionService.Instance.ScannerPort))
        //        {
        //            InfoMessage = "Порт сканера не задан!";
        //            _notificationService.ShowMessage(InfoMessage);
        //            return;
        //        }

        //        if (SessionService.Instance.ScannerModel == "Honeywell")
        //        {
        //            if (!CheckComPortExists(SessionService.Instance.ScannerPort))
        //            {
        //                InfoMessage = $"Сканер на порту {SessionService.Instance.ScannerPort} недоступен!";
        //                _notificationService.ShowMessage(InfoMessage);
        //                return;
        //            }
        //        }
        //        else
        //        {
        //            InfoMessage = $"Сканер модели '{SessionService.Instance.ScannerModel}' пока не поддерживается.";
        //            _notificationService.ShowMessage(InfoMessage);
        //            return;
        //        }
        //    }

        //    // Все проверки пройдены — переходим к агрегации
        //    _router.GoTo<AggregationViewModel>();
        //}
        //[RelayCommand]
        //public async Task GoAggregationAsync()
        //{
        //    // Загружаем все настройки перед проверкой
        //    await _sessionService.InitializeAsync(_databaseService);

        //    // Проверки устройств
        //    var cameraCheck = await CheckCameraAsync();
        //    if (!cameraCheck.Success)
        //    {
        //        _notificationService.ShowMessage(cameraCheck.Message);
        //        return;
        //    }

        //    var printerCheck = await CheckPrinterAsync();
        //    if (!printerCheck.Success)
        //    {
        //        _notificationService.ShowMessage(printerCheck.Message);
        //        return;
        //    }

        //    var controllerCheck = await CheckControllerAsync();
        //    if (!controllerCheck.Success)
        //    {
        //        _notificationService.ShowMessage(controllerCheck.Message);
        //        return;
        //    }

        //    var scannerCheck = await CheckScannerAsync();
        //    if (!scannerCheck.Success)
        //    {
        //        _notificationService.ShowMessage(scannerCheck.Message);
        //        return;
        //    }

        //    // Все проверки пройдены
        //    _router.GoTo<AggregationViewModel>();
        //}
        [RelayCommand]
        public async Task GoAggregationAsync()
        {
            // Загружаем все настройки перед проверкой
            await _sessionService.InitializeAsync(_databaseService);

            var errorMessages = new List<string>();

            var cameraCheck = await CheckCameraAsync();
            if (!cameraCheck.Success)
                errorMessages.Add(cameraCheck.Message);

            var printerCheck = await CheckPrinterAsync();
            if (!printerCheck.Success)
                errorMessages.Add(printerCheck.Message);

            var controllerCheck = await CheckControllerAsync();
            if (!controllerCheck.Success)
                errorMessages.Add(controllerCheck.Message);

            var scannerCheck = await CheckScannerAsync();
            if (!scannerCheck.Success)
                errorMessages.Add(scannerCheck.Message);

            if (errorMessages.Any())
            {
                foreach (var msg in errorMessages)
                {
                    _notificationService.ShowMessage(msg);
                }
                return;
            }

            // Все проверки пройдены
            _router.GoTo<AggregationViewModel>();
        }
        private async Task<(bool Success, string Message)> CheckCameraAsync()
        {
            if (!_sessionService.CheckCamera)
                return (true, null);

            if (string.IsNullOrWhiteSpace(_sessionService.CameraIP))
                return (false, "IP камеры не задан!");

            if (!await PingDeviceAsync(_sessionService.CameraIP))
                return (false, $"Камера {_sessionService.CameraIP} недоступна!");

            return (true, null);
        }

        private async Task<(bool Success, string Message)> CheckPrinterAsync()
        {
            if (!_sessionService.CheckPrinter)
                return (true, null);

            if (string.IsNullOrWhiteSpace(_sessionService.PrinterIP))
                return (false, "IP принтера не задан!");

            try
            {
                if (_sessionService.PrinterModel == "Zebra")
                {
                    // ... код проверки принтера
                    return (true, null);
                }
                return (false, $"Принтер модели '{_sessionService.PrinterModel}' не поддерживается.");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка принтера: {ex.Message}");
            }
        }

        private async Task<(bool Success, string Message)> CheckControllerAsync()
        {
            if (!_sessionService.CheckController)
                return (true, null);

            if (string.IsNullOrWhiteSpace(_sessionService.ControllerIP))
                return (false, "IP контроллера не задан!");

            if (!await PingDeviceAsync(_sessionService.ControllerIP))
                return (false, $"Контроллер {_sessionService.ControllerIP} недоступен!");

            return (true, null);
        }

        private async Task<(bool Success, string Message)> CheckScannerAsync()
        {
            if (!_sessionService.CheckScanner)
                return (true, null);

            if (string.IsNullOrWhiteSpace(_sessionService.ScannerPort))
                return (false, "Порт сканера не задан!");

            if (_sessionService.ScannerModel != "Honeywell")
                return (false, $"Сканер модели '{_sessionService.ScannerModel}' не поддерживается.");

            if (!CheckComPortExists(_sessionService.ScannerPort))
                return (false, $"Сканер на порту {_sessionService.ScannerPort} недоступен!");

            return (true, null);
        }
        private async Task<bool> PingDeviceAsync(string ip)
        {
            try
            {
                using var ping = new System.Net.NetworkInformation.Ping();
                var reply = await ping.SendPingAsync(ip, 300); // 300ms timeout
                return reply.Status == System.Net.NetworkInformation.IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckComPortExists(string portName)
        {
            return System.IO.Ports.SerialPort.GetPortNames().Contains(portName);
        }
    }
}
