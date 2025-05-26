using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l2l_aggregator.Services;
using l2l_aggregator.Services.AggregationService;
using l2l_aggregator.Services.Api;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Notification.Interface;
using Refit;
using System;
using System.IO.Ports;
using System.Linq;
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
        private readonly DataApiService _dataApiService;
        private readonly SessionService _sessionService;
        private readonly INotificationService _notificationService;
        private ScannerWorker _scannerWorker;
        public AuthViewModel(DatabaseService databaseService, 
                            HistoryRouter<ViewModelBase> router, 
                            INotificationService notificationService, 
                            DataApiService dataApiService, 
                            SessionService sessionService)
        {
            _databaseService = databaseService;
            _router = router;
            _notificationService = notificationService;
            _dataApiService = dataApiService;
            _sessionService = sessionService;
            // Тестовые/заготовленные значения
            _login = "TESTINNO1";
            _password = "4QrcOUm6Wau+VuBX8g+IPg==";
            InitializeScanner();
        }


        private async void InitializeScanner()
        {
            try
            {
                var savedScannerPort = await _databaseService.Config.GetConfigValueAsync("ScannerCOMPort");
                var savedScannerModel = await _databaseService.Config.GetConfigValueAsync("ScannerModel");

                if (string.IsNullOrWhiteSpace(savedScannerPort) || string.IsNullOrWhiteSpace(savedScannerModel))
                    return;

                var availablePorts = SerialPort.GetPortNames();

                if (!availablePorts.Contains(savedScannerPort))
                {
                    _notificationService.ShowMessage($"Сканер на порту '{savedScannerPort}' не найден. Удаление из настроек...");

                    // Очистка в SessionService
                    _sessionService.ScannerPort = null;
                    _sessionService.ScannerModel = null;

                    // Очистка в конфигурации
                    await _databaseService.Config.SetConfigValueAsync("ScannerCOMPort", null);
                    await _databaseService.Config.SetConfigValueAsync("ScannerModel", null);

                    return;
                }

                if (savedScannerModel == "Honeywell")
                {
                    _scannerWorker = new ScannerWorker(savedScannerPort);
                    _scannerWorker.BarcodeScanned += HandleScannedBarcode;
                    _scannerWorker.RunWorkerAsync();

                    _sessionService.ScannerPort = savedScannerPort;
                    _sessionService.ScannerModel = savedScannerModel;
                }
                else
                {
                    _notificationService.ShowMessage($"Модель сканера '{savedScannerModel}' не поддерживается.");
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка инициализации сканера: {ex.Message}");
            }
        }
        // Обработка считанного штрихкода
        private async void HandleScannedBarcode(string barcode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(barcode)) return;
                var parts = barcode.Trim().Split(';');
                if (parts.Length == 2)
                {
                    Login = parts[0];
                    Password = parts[1];
                    await LoginAsync();
                }
                else
                {
                    _notificationService.ShowMessage("Некорректный формат штрих-кода. Ожидается 'Логин;Пароль'.", NotificationType.Warn);
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка обработки штрих-кода: {ex.Message}", NotificationType.Error);
            }
        }
        [RelayCommand]
        public async Task LoginAsync()
        {
            try
            {
                // Локальный админ вход
                if (await _databaseService.UserAuth.ValidateAdminUserAsync(Login, Password))
                {
                    _sessionService.User = new Models.UserAuthResponse { USER_NAME = Login };
                    _notificationService.ShowMessage("Админ вход. Переходим к настройкам...");
                    _router.GoTo<SettingsViewModel>();
                    return;
                }

                // Получаем адрес сервера из настроек, запрос на USER_AUTH
                var serverUri = await _databaseService.Config.GetConfigValueAsync("ServerUri");
                if (string.IsNullOrWhiteSpace(serverUri))
                {
                    //InfoMessage = "Сервер не настроен!";
                    _notificationService.ShowMessage("Сервер не настроен!");
                    return;
                }
                // Попытка входа через API
                try
                {
                    var response = await _dataApiService.LoginAsync(Login, Password);
                    if (response != null)
                    {
                        if (response.AUTH_OK == "1")
                        {
                            _sessionService.User = response;
                            // Сохраняем в локальную базу
                            await _databaseService.UserAuth.SaveUserAuthAsync(response);
                            // Успешная авторизация
                            _notificationService.ShowMessage("Авторизация прошла успешно!");

                            // Загружаем сохранённое состояние (если есть)
                            await _sessionService.LoadAggregationStateAsync(_databaseService);


                            if (_sessionService.HasUnfinishedAggregation)
                            {
                                _notificationService.ShowMessage("Обнаружена незавершённая агрегация. Продолжаем...");
                                _router.GoTo<AggregationViewModel>();
                            }
                            else
                            {
                                _router.GoTo<TaskListViewModel>();
                            }
                        }
                        else
                        {
                            _notificationService.ShowMessage($"Ошибка авторизации: {response.ERROR_TEXT}", NotificationType.Warn);
                        }
                    }
                    else
                    {
                        _notificationService.ShowMessage("Ошибка: пустой ответ от сервера.", NotificationType.Error);
                    }

                }
                catch (Exception ex)
                {

                    _notificationService.ShowMessage($"Ошибка входа: {ex.Message}", NotificationType.Error);
                }

            }
            catch (ApiException apiEx)
            {
                _notificationService.ShowMessage($"API ошибка: {apiEx.Message}", NotificationType.Error);
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка: {ex.Message}", NotificationType.Error);
            }
        }
    }
}
