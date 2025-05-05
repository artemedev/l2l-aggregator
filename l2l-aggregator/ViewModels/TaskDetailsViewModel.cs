using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastReport.Utils;
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
        [RelayCommand]
        public async Task GoAggregationAsync()
        {
            //var s = _sessionService;

            // Проверка камеры
            if (_sessionService.CheckCamera)
            {
                if (string.IsNullOrWhiteSpace(_sessionService.CameraIP))
                {
                    InfoMessage = "IP камеры не задан!";
                    _notificationService.ShowMessage(InfoMessage);
                    return;
                }

                bool cameraReachable = await PingDeviceAsync(_sessionService.CameraIP);
                if (!cameraReachable)
                {
                    InfoMessage = $"Камера {_sessionService.CameraIP} недоступна!";
                    _notificationService.ShowMessage(InfoMessage);
                    return;
                }
            }

            // Проверка принтера
            if (_sessionService.CheckPrinter)
            {
                if (string.IsNullOrWhiteSpace(_sessionService.PrinterIP))
                {
                    InfoMessage = "IP принтера не задан!";
                    _notificationService.ShowMessage(InfoMessage);
                    return;
                }

                try
                {
                    if (_sessionService.PrinterModel == "Zebra")
                    {
                        var config = PrinterConfigBuilder.Build(_sessionService.PrinterIP);
                        var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("PrinterCheck");
                        var device = new PrinterTCP("PreAggPrinter", logger);
                        device.Configure(config);

                        device.StartWork();
                        DeviceHelper.WaitForState(device, DeviceStatusCode.Run, 10);
                        device.StopWork();
                        DeviceHelper.WaitForState(device, DeviceStatusCode.Ready, 10);
                    }
                    else
                    {
                        InfoMessage = $"Принтер модели '{_sessionService.PrinterModel}' не поддерживается.";
                        _notificationService.ShowMessage(InfoMessage);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    InfoMessage = $"Ошибка соединения с принтером: {ex.Message}";
                    _notificationService.ShowMessage(InfoMessage);
                    return;
                }
            }

            // Проверка контроллера
            if (_sessionService.CheckController)
            {
                if (string.IsNullOrWhiteSpace(_sessionService.ControllerIP))
                {
                    InfoMessage = "IP контроллера не задан!";
                    _notificationService.ShowMessage(InfoMessage);
                    return;
                }

                bool controllerReachable = await PingDeviceAsync(_sessionService.ControllerIP);
                if (!controllerReachable)
                {
                    InfoMessage = $"Контроллер {_sessionService.ControllerIP} недоступен!";
                    _notificationService.ShowMessage(InfoMessage);
                    return;
                }
            }

            // Проверка сканера
            if (_sessionService.CheckScanner)
            {
                if (string.IsNullOrWhiteSpace(_sessionService.ScannerPort))
                {
                    InfoMessage = "Порт сканера не задан!";
                    _notificationService.ShowMessage(InfoMessage);
                    return;
                }

                bool scannerAvailable = CheckComPortExists(_sessionService.ScannerPort);
                if (!scannerAvailable)
                {
                    InfoMessage = $"Сканер на порту {_sessionService.ScannerPort} недоступен!";
                    _notificationService.ShowMessage(InfoMessage);
                    return;
                }
            }

            // Все проверки пройдены — переходим к агрегации
            _router.GoTo<AggregationViewModel>();
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
