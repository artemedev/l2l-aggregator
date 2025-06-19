using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l2l_aggregator.Models;
using l2l_aggregator.Services;
using l2l_aggregator.Services.Api;
using l2l_aggregator.Services.ControllerService;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Notification.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly DatabaseDataService _databaseDataService;


        public TaskDetailsViewModel(HistoryRouter<ViewModelBase> router, 
            DatabaseService databaseService,
            DatabaseDataService databaseDataService,
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
            _databaseDataService = databaseDataService;

            LoadTaskAsync(_sessionService.SelectedTask.DOCID);
        }
        private async Task LoadTaskAsync(long docId)
        {
            try
            {
                var serverUri = _sessionService.DatabaseUri;
                if (string.IsNullOrWhiteSpace(serverUri))
                {
                    InfoMessage = "Сервер не настроен!";
                    _notificationService.ShowMessage(InfoMessage);

                    return;
                }
                var request = new ArmJobInfoRequest { docid = docId };
                //var jobInfo = await _dataApiService.GetJobDetailsAsync(docId);
                var jobInfo = await _databaseDataService.GetJobDetailsAsync(docId);
                Task = jobInfo;
                _sessionService.SelectedTaskInfo = jobInfo;
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


        [RelayCommand]
        public async Task GoAggregationAsync()
        {
            ////проверка контроллера
            //var modbusService = new ModbusPositioningService(_sessionService.ControllerIP, 1);
            //var controllerAvailable = await modbusService.CheckMutualConnectionAsync();

            //if (!controllerAvailable)
            //{
            //    _notificationService.ShowMessage("Контроллер не отвечает. Невозможно начать агрегацию.");
            //    return;
            //}
            //await _configLoader.LoadSettingsToSessionAsync();
            //await _sessionService.InitializeAsync(_databaseService);
            //var session = SessionService.Instance;

            var results = new List<(bool Success, string Message)>
            {
                await _deviceCheckService.CheckCameraAsync(_sessionService),
                await _deviceCheckService.CheckPrinterAsync(_sessionService),
                await _deviceCheckService.CheckControllerAsync(_sessionService),
                await _deviceCheckService.CheckScannerAsync(_sessionService)
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
