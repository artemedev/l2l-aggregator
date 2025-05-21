using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l2l_aggregator.Models;
using l2l_aggregator.Services;
using l2l_aggregator.Services.Api;
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
                var jobInfo = await _dataApiService.GetJobDetailsAsync(docId);
                //await client.GetJob(request);
                Task = jobInfo;
                //_sessionService.SelectedTaskInfo = response.RECORDSET.FirstOrDefault();
                _sessionService.SelectedTaskInfo = jobInfo;
                //_sessionService.SelectedTaskSscc = ssccInfo;
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
