using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l2l_aggregator.Models;
using l2l_aggregator.Services;
using l2l_aggregator.Services.Api;
using l2l_aggregator.Services.Database;
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

        public TaskDetailsViewModel(HistoryRouter<ViewModelBase> router, DatabaseService databaseService, SessionService sessionService, DataApiService dataApiService)
        {
            _dataApiService = dataApiService;
            _databaseService = databaseService;
            _router = router;
            _sessionService = sessionService;
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
            }
        }

        [RelayCommand]
        public void GoBack()
        {
            // Переход на страницу назад
            _router.GoTo<TaskListViewModel>();
        }

        [RelayCommand]
        public void GoAggregation()
        {
            Debug.WriteLine("[APP] Переход на AggregationViewModel...");
            // Переход на страницу агрегации
            _router.GoTo<AggregationViewModel>();
        }
    }
}
