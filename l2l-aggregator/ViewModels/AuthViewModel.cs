using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l2l_aggregator.Models;
using l2l_aggregator.Services.Api;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Database.Interfaces;
using l2l_aggregator.Services.Notification.Interface;
using Refit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace l2l_aggregator.ViewModels
{
    public partial class AuthViewModel : ViewModelBase
    {

        [ObservableProperty]
        private string _login;

        [ObservableProperty]
        private string _password;

        [ObservableProperty]
        private string _infoMessage;

        private readonly HistoryRouter<ViewModelBase> _router;
        private readonly DatabaseService _databaseService;
        //private readonly IApiClientFactory _apiClientFactory;
        private readonly DataApiService _dataApiService;

        private readonly INotificationService _notificationService;
        public AuthViewModel(DatabaseService databaseService, HistoryRouter<ViewModelBase> router, INotificationService notificationService, DataApiService dataApiService)
        {
            _databaseService = databaseService;
            _router = router;
            _notificationService = notificationService;
            _dataApiService = dataApiService;
            _login = "TESTINNO1";
            _password = "4QrcOUm6Wau+VuBX8g+IPg==";
        }

        [RelayCommand]
        public async Task LoginAsync()
        {
            try
            {
                if (await _databaseService.UserAuth.ValidateAdminUserAsync(Login, Password))
                {
                    //InfoMessage = "Админ вход. Переходим к настройкам...";
                    _notificationService.ShowMessage("Админ вход. Переходим к настройкам...");
                    _router.GoTo<SettingsViewModel>();
                    return;
                }

                // Если не admin, то делаем запрос на USER_AUTH
                var serverUri = await _databaseService.Config.GetConfigValueAsync("ServerUri");
                if (string.IsNullOrWhiteSpace(serverUri))
                {
                    //InfoMessage = "Сервер не настроен!";
                    _notificationService.ShowMessage("Сервер не настроен!");
                    return;
                }
                //HttpClientHandler httpClientHandler = new HttpClientHandler();
                //httpClientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                //var client = RestService.For<IAuthApi>(new HttpClient(httpClientHandler)
                //{
                //    BaseAddress = new Uri(serverUri)
                //});
                //var client = await _dataApiService.CreateClientAsync<IAuthApi>();
                ////var response = await client.UserAuth(request);

                //var request = new UserAuthRequest
                //{
                //    wid = Login,
                //    spd = Password  // Заглушка
                //};
                try
                {
                    var response = await _dataApiService.LoginAsync(Login, Password);
                    if (response.AUTH_OK == "1")
                    {
                        await _databaseService.UserAuth.SaveUserAuthAsync(response);
                        // Успешная авторизация
                        //InfoMessage = "Авторизация прошла успешно!";
                        _notificationService.ShowMessage("Авторизация прошла успешно!");
                        // Переходим к списку задач
                        //_router.GoTo<TaskListViewModel>();
                    }
                    else
                    {
                        //InfoMessage = $"Ошибка авторизации: {response.ERROR_TEXT}";
                        _notificationService.ShowMessage($"Ошибка авторизации: {response.ERROR_TEXT}");
                    }
                }
                catch (Exception ex)
                {

                    _notificationService.ShowMessage(ex.Message);
                }

            }
            catch (ApiException apiEx)
            {
                _notificationService.ShowMessage($"API ошибка: {apiEx.Message}");

                //$"Ошибка авторизации: {response.ERROR_TEXT}"
                //InfoMessage = $"API ошибка: {apiEx.Message}";
            }
        }
    }
}
