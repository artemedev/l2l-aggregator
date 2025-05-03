using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l2l_aggregator.Models;
using l2l_aggregator.Services.Api;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Database.Interfaces;
using Refit;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace l2l_aggregator.ViewModels
{

    public partial class InitializationViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _serverUri = string.Empty;

        [ObservableProperty]
        private string _infoMessage = string.Empty;

        private readonly HistoryRouter<ViewModelBase> _router;
        private readonly DatabaseService _databaseService;
        private readonly DataApiService _dataApiService;

        public InitializationViewModel(DatabaseService databaseService, HistoryRouter<ViewModelBase> router, DataApiService dataApiService)
        {
            _databaseService = databaseService;
            _router = router;
            _dataApiService = dataApiService;

        }

        [RelayCommand]
        public async Task CheckServerAsync()
        {
            if (string.IsNullOrWhiteSpace(ServerUri))
            {
                InfoMessage = "Введите адрес сервера!";
                return;
            }

            try
            {
                //// Создаём динамический Refit-клиент с нужным BaseAddress
                //var client = RestService.For<IAuthApi>(new HttpClient()
                //{
                //    BaseAddress = new Uri(ServerUri)
                //});

                // Выполняем запрос (по условию, headers + body)
                var request = new ArmDeviceRegistrationRequest
                {
                    NAME = "test",
                    MAC_ADDRESS = "test",
                    SERIAL_NUMBER = "test",
                    NET_ADDRESS = "test",
                    KERNEL_VERSION = "test",
                    HADWARE_VERSION = "test",
                    SOFTWARE_VERSION = "test",
                    FIRMWARE_VERSION = "test",
                    DEVICE_TYPE = "test"
                };

                var httpClient = new HttpClient
                {
                    BaseAddress = new Uri(ServerUri)
                };
                httpClient.DefaultRequestHeaders.Add("MTDApikey", "e2fbe0f4fbe2e0fbf4ecf7f1ece5e8f020fbe2e0eff0eae5f020edeeede320fceee8ec343533343536333435212121de2cc1de"); // если нужно

                var authClient = RestService.For<IAuthApi>(httpClient);
                var response = await authClient.RegisterDevice(request);

                await _databaseService.RegistrationDevice.SaveRegistrationAsync(response);

                await _databaseService.Config.SetConfigValueAsync("ServerUri", ServerUri);

                InfoMessage = "Сервер проверен, переходим к авторизации...";
                _router.GoTo<AuthViewModel>();
            }
            catch (ApiException apiEx)
            {
                InfoMessage = $"Ошибка при запросе: {apiEx.StatusCode}";
            }
        }
    }

}