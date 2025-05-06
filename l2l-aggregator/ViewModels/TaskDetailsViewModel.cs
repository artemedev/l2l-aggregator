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
        private readonly DeviceCheckService _deviceCheckService;
        private readonly ConfigurationLoaderService _configLoader;

        public TaskDetailsViewModel(HistoryRouter<ViewModelBase> router, 
            DatabaseService databaseService, 
            SessionService sessionService, 
            DataApiService dataApiService, 
            INotificationService notificationService, 
            DeviceCheckService deviceCheckService,
            ConfigurationLoaderService configLoader)
        {
            _configLoader = configLoader;
            _dataApiService = dataApiService;
            _databaseService = databaseService;
            _router = router;
            _sessionService = sessionService;
            _notificationService = notificationService;
            _deviceCheckService = deviceCheckService;
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
            await _configLoader.LoadSettingsToSessionAsync();
            await _sessionService.InitializeAsync(_databaseService);
            var session = SessionService.Instance;

            var results = new List<(bool Success, string Message)>
            {
                await _deviceCheckService.CheckCameraAsync(session),
                await _deviceCheckService.CheckPrinterAsync(session),
                await _deviceCheckService.CheckControllerAsync(session),
                await _deviceCheckService.CheckScannerAsync(session)
            };

            var errors = results.Where(r => !r.Success).Select(r => r.Message).ToList();
            if (errors.Any())
            {
                foreach (var msg in errors)
                    _notificationService.ShowMessage(msg);
                return;
            }

            _router.GoTo<AggregationViewModel>();
        }
    }
}
