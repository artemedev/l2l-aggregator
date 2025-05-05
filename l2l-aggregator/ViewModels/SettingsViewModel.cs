using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l2l_aggregator.Models;
using l2l_aggregator.Services.Database.Interfaces;
using Refit;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.IO.Ports;
using l2l_aggregator.Services.Database;
using l2l_aggregator.ViewModels.VisualElements;
using l2l_aggregator.Services.Notification.Interface;
using l2l_aggregator.Services;
using l2l_aggregator.Services.Notification;

namespace l2l_aggregator.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {

        [ObservableProperty]
        private string _serverUri;

        [ObservableProperty]
        private string _cameraIP;
        [ObservableProperty]
        private string _serverIP;
        [ObservableProperty]
        private string _printerIP;
        [ObservableProperty]
        private string _scannerCOMPort;
        [ObservableProperty]
        private string _controllerIP;

        //[ObservableProperty]
        //private string _selectedScannerModel;



        [ObservableProperty]
        private string _licenseNumber = "XXXX-XXXX-XXXX-XXXX";

        [ObservableProperty]
        private bool _checkForUpdates;


        [ObservableProperty]
        private string _infoMessage;


        [ObservableProperty]
        private bool _disableVirtualKeyboard;



        [ObservableProperty]
        private string _selectedCameraModel;

        [ObservableProperty]
        private ObservableCollection<string> _printerModels = new() { "Model X", "Model Y", "Model Z" };

        [ObservableProperty] private string _selectedPrinterModel;

        [ObservableProperty]
        private ObservableCollection<CameraViewModel> _cameras = new();

        [ObservableProperty]
        private ObservableCollection<string> _cameraModels = new() { "Model A", "Model B", "Model C" };

        [ObservableProperty]
        private bool isConnected;


        [ObservableProperty] private ObservableCollection<ScannerDevice> _availableScanners = new();

        [ObservableProperty] private ScannerDevice _selectedScanner;

        private readonly HistoryRouter<ViewModelBase> _router;
        private readonly INotificationService _notificationService;
        private readonly DatabaseService _databaseService;
        public SettingsViewModel(DatabaseService DatabaseService, HistoryRouter<ViewModelBase> router, INotificationService notificationService)
        {
            _notificationService = notificationService;
            _databaseService = DatabaseService;
            _router = router;
            _ = LoadSettingsAsync();
            LoadCameras();
            LoadAvailableScanners();

            if (Cameras.Count == 0)
            {
                AddCamera();
            }
        }
        [RelayCommand]
        private async Task ToggleDisableVirtualKeyboardAsync()
        {
            await _databaseService.Config.SetConfigValueAsync("DisableVirtualKeyboard", DisableVirtualKeyboard.ToString());
            SessionService.Instance.DisableVirtualKeyboard = DisableVirtualKeyboard;
            InfoMessage = "Настройка клавиатуры сохранена.";
            _notificationService.ShowMessage(InfoMessage);
        }

        private async Task LoadSettingsAsync()
        {
            ServerUri = await _databaseService.Config.GetConfigValueAsync("ServerUri");
            // аналогично можно загрузить другие значения, если потребуется
            DisableVirtualKeyboard = bool.TryParse(await _databaseService.Config.GetConfigValueAsync("DisableVirtualKeyboard"), out var parsed) && parsed;
            SessionService.Instance.DisableVirtualKeyboard = DisableVirtualKeyboard;
        }

        public void LoadAvailableScanners()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                ScannerDevice device1 = new ScannerDevice();
                device1.Id = port;
                AvailableScanners.Add(device1);
            }
        }

        [RelayCommand]
        private void OpenCameraSettings()
        {
            // Передаём CameraIP или другие параметры
            _router.GoTo<CameraSettingsViewModel>();
        }
        private void LoadCameras()
        {

        }

        [RelayCommand]
        public void AddCamera()
        {
            Cameras.Add(new CameraViewModel());
        }

        [RelayCommand]
        public void RemoveCamera(CameraViewModel camera)
        {
            if (Cameras.Count > 1) // Ensure at least one camera remains
            {
                Cameras.Remove(camera);
            }
            else
            {
                InfoMessage = "Как минимум одна камера должна быть настроена";
                _notificationService.ShowMessage(InfoMessage);
            }
        }

        [RelayCommand]
        public async Task TestCameraConnectionAsync(CameraViewModel camera)
        {
            if (string.IsNullOrWhiteSpace(camera.CameraIP))
            {
                InfoMessage = "Введите IP адрес камеры!";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            try
            {
                // Implement your camera connection test here
                // This is just a placeholder
                await Task.Delay(500); // Simulate network operation

                camera.IsConnected = true;
                InfoMessage = $"Соединение с камерой {camera.CameraIP} установлено!";
                _notificationService.ShowMessage(InfoMessage);

            }
            catch (Exception ex)
            {
                camera.IsConnected = false;
                InfoMessage = $"Ошибка соединения с камерой: {ex.Message}";
                _notificationService.ShowMessage(InfoMessage);

            }
        }

        [RelayCommand]
        public async Task CheckAndSaveUriAsync()
        {
            if (!string.IsNullOrWhiteSpace(ServerUri))
            {
                // InfoMessage = "Введите адрес сервера!";
                try
                {
                    var client = RestService.For<IAuthApi>(new HttpClient
                    {
                        BaseAddress = new Uri(ServerUri)
                    });

                    _databaseService.Config.SetConfigValueAsync("ServerUri", ServerUri);

                    InfoMessage = "URI успешно сохранён!";
                    _notificationService.ShowMessage(InfoMessage);

                    //_router.GoTo<AuthViewModel>();
                }
                catch (Exception ex)
                {
                    InfoMessage = $"Ошибка: {ex.Message}";
                    _notificationService.ShowMessage($"Ошибка: {ex.Message}");

                }
            }


        }

        //[RelayCommand]
        //private void TestCameraConnection() { /* ... */ }

        [RelayCommand]
        private void TestServerConnection() { /* ... */ }

        [RelayCommand]
        private void TestPrinterConnection() { /* ... */ }

        [RelayCommand]
        private void TestScannerConnection()
        {
            if (SelectedScanner == null)
            {
                InfoMessage = "Сканер не выбран!";
                _notificationService.ShowMessage(InfoMessage);

                return;
            }

            try
            {
                // Сохраняем сканер в конфиг
                 _databaseService.Config.SaveScannerDeviceAsync(SelectedScanner);

                InfoMessage = $"Сканер '{SelectedScanner.Id}' успешно сохранён!";
                _notificationService.ShowMessage(InfoMessage);

            }
            catch (Exception ex)
            {

                InfoMessage = $"Ошибка подключения: {ex.Message}";
                _notificationService.ShowMessage(InfoMessage);

            }
        }


        [RelayCommand]
        private void TestControllerConnection() { /* ... */ }

        [RelayCommand]
        private async void SaveSettings()
        {
            // Save all camera settings to DatabaseService
            // For each camera in Cameras collection
            foreach (var camera in Cameras)
            {
                // _databaseService.SaveCameraSettings(camera.Id, camera.CameraIP, camera.SelectedCameraModel);
            }
            await _databaseService.Config.SetConfigValueAsync("DisableVirtualKeyboard", DisableVirtualKeyboard.ToString());

            InfoMessage = "Настройки успешно сохранены!";
            _notificationService.ShowMessage(InfoMessage);

        }


        [RelayCommand]
        private void GetArchive() { /* ... */ }
    }
}
