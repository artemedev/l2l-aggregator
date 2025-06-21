using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l2l_aggregator.Models;
using l2l_aggregator.Services;
using l2l_aggregator.Services.Api;
using l2l_aggregator.Services.Api.Interfaces;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Notification.Interface;
using Refit;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace l2l_aggregator.ViewModels
{
    public partial class InitializationViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _infoMessage = "Проверка подключения к базе данных...";

        [ObservableProperty]
        private string _nameDevice = Environment.MachineName; // Используем имя компьютера по умолчанию

        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private string _databaseInfo = "База данных: 172.16.3.237:3050";

        private readonly HistoryRouter<ViewModelBase> _router;
        private readonly DatabaseService _databaseService;
        private readonly DataApiService _dataApiService;
        private readonly INotificationService _notificationService;
        private readonly SessionService _sessionService;
        private readonly DatabaseDataService _databaseDataService;

        public InitializationViewModel(
            DatabaseService databaseService,
            HistoryRouter<ViewModelBase> router,
            DataApiService dataApiService,
            DatabaseDataService databaseDataService,
            INotificationService notificationService,
            SessionService sessionService)
        {
            _notificationService = notificationService;
            _databaseService = databaseService;
            _router = router;
            _dataApiService = dataApiService;
            _sessionService = sessionService;
            _databaseDataService = databaseDataService;

            // Автоматически запускаем проверку при создании ViewModel
            _ = Task.Run(AutoInitializeAsync);
        }

        // Автоматическая инициализация при загрузке
        private async Task AutoInitializeAsync()
        {
            await Task.Delay(500); // Небольшая задержка для отображения UI
            await CheckDatabaseConnectionAsync();
        }

        [RelayCommand]
        public async Task CheckDatabaseConnectionAsync()
        {
            if (IsLoading) return;

            IsLoading = true;
            InfoMessage = "Проверка подключения к базе данных...";

            try
            {
                // Проверяем подключение к удаленной БД
                bool isConnected = await _databaseDataService.TestConnectionAsync();

                if (isConnected)
                {
                    InfoMessage = "Подключение установлено. Регистрация устройства...";

                    // Получаем MAC-адрес устройства
                    string macAddress = GetMacAddress();

                    // Тестируем регистрацию устройства
                    var request = new ArmDeviceRegistrationRequest
                    {
                        NAME = NameDevice,
                        MAC_ADDRESS = macAddress,
                        SERIAL_NUMBER = NameDevice, //должен вводить пользователь
                        NET_ADDRESS = GetLocalIPAddress(),
                        KERNEL_VERSION = Environment.OSVersion.Version.ToString(),
                        HADWARE_VERSION = Environment.OSVersion.Platform.ToString(),
                        SOFTWARE_VERSION = "1.0.0", // Версия вашего приложения
                        FIRMWARE_VERSION = "1.0.0",
                        DEVICE_TYPE = "ARM"
                    };

                    var deviceRegistered = await _databaseDataService.RegisterDeviceAsync(request);

                    if (deviceRegistered?.DEVICEID != null)
                    {
                        // Сохраняем информацию об устройстве в сессии
                        await _sessionService.SaveDeviceInfoAsync(
                            deviceRegistered.DEVICEID,
                            deviceRegistered.DEVICE_NAME ?? NameDevice);

                        InfoMessage = "Устройство зарегистрировано. Переход к авторизации...";
                        _notificationService.ShowMessage("Инициализация завершена успешно", NotificationType.Success);

                        await Task.Delay(1000); // Показываем сообщение об успехе
                        _router.GoTo<AuthViewModel>();
                    }
                    else
                    {
                        InfoMessage = "Ошибка регистрации устройства в базе данных";
                        _notificationService.ShowMessage(InfoMessage, NotificationType.Error);
                    }
                }
                else
                {
                    InfoMessage = "Не удалось подключиться к базе данных (172.16.3.237:3050)";
                    _notificationService.ShowMessage(InfoMessage, NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                InfoMessage = $"Ошибка при инициализации: {ex.Message}";
                _notificationService.ShowMessage(InfoMessage, NotificationType.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        public async Task RetryConnectionAsync()
        {
            // Сбрасываем состояние подключения
            _databaseDataService.ResetConnection();
            await CheckDatabaseConnectionAsync();
        }

        [RelayCommand]
        public void SkipToAuth()
        {
            // Аварийный переход к авторизации (для отладки)
            _notificationService.ShowMessage("Переход к авторизации без проверки подключения", NotificationType.Warn);
            _router.GoTo<AuthViewModel>();
        }

        // Получение MAC-адреса
        private string GetMacAddress()
        {
            try
            {
                var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                foreach (var network in networkInterfaces)
                {
                    if (network.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                        network.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    {
                        return network.GetPhysicalAddress().ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка получения MAC-адреса: {ex.Message}", NotificationType.Warn);
            }

            return "00:00:00:00:00:00"; // Fallback
        }

        // Получение локального IP-адреса
        private string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка получения IP-адреса: {ex.Message}", NotificationType.Warn);
            }

            return "127.0.0.1"; // Fallback
        }

        // Свойства для отображения в UI
        public string ApplicationVersion => "1.0.0";
        public string DatabaseConnection => "172.16.3.237:3050";
    }
}