using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastReport.Utils;
using l2l_aggregator.Helpers.AggregationHelpers;
using l2l_aggregator.Models;
using l2l_aggregator.Services;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Database.Interfaces;
using l2l_aggregator.Services.Notification.Interface;
using l2l_aggregator.ViewModels.VisualElements;
using MD.Devices;
using Microsoft.Extensions.Logging;
using Refit;
using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace l2l_aggregator.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {

        [ObservableProperty] private string _serverUri;

        [ObservableProperty] private string _cameraIP;
        [ObservableProperty]
        private string _serverIP;
        [ObservableProperty]
        private string _printerIP;

        [ObservableProperty]
        private string _controllerIP;

        //[ObservableProperty]
        //private string _selectedScannerModel;



        [ObservableProperty] private string _licenseNumber = "XXXX-XXXX-XXXX-XXXX";

        [ObservableProperty]
        private bool _checkForUpdates;


        [ObservableProperty]
        private string _infoMessage;


        [ObservableProperty]
        private bool _disableVirtualKeyboard;



        [ObservableProperty]
        private string _selectedCameraModel;

        [ObservableProperty]
        private ObservableCollection<string> _printerModels = new() { "Zebra" };

        [ObservableProperty] private string _selectedPrinterModel;



        [ObservableProperty]
        private ObservableCollection<string> _cameraModels = new() { "Basler" };

        [ObservableProperty] private bool isConnectedCamera;
        [ObservableProperty] private ObservableCollection<CameraViewModel> _cameras = new();

        [ObservableProperty] private ObservableCollection<ScannerDevice> _availableScanners = new();
        [ObservableProperty] private ScannerDevice _selectedScanner;
        [ObservableProperty] private string _scannerCOMPort;

        [ObservableProperty] private bool _checkCameraBeforeAggregation = true;
        [ObservableProperty] private bool _checkPrinterBeforeAggregation = true;
        [ObservableProperty] private bool _checkControllerBeforeAggregation = true;
        [ObservableProperty] private bool _checkScannerBeforeAggregation = true;

        private readonly HistoryRouter<ViewModelBase> _router;
        private readonly INotificationService _notificationService;
        private readonly DatabaseService _databaseService;
        private readonly SessionService _sessionService;

        public SettingsViewModel(DatabaseService DatabaseService, HistoryRouter<ViewModelBase> router, INotificationService notificationService, SessionService sessionService)
        {
            _notificationService = notificationService;
            _databaseService = DatabaseService;
            _router = router;
            _sessionService = sessionService;

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
            _sessionService.DisableVirtualKeyboard = DisableVirtualKeyboard;
            InfoMessage = "Настройка клавиатуры сохранена.";
            _notificationService.ShowMessage(InfoMessage);
        }
        partial void OnCheckControllerBeforeAggregationChanged(bool value)
        {
            _ = _databaseService.Config.SetConfigValueAsync("CheckController", value.ToString());
            _sessionService.CheckController = value;
        }
        partial void OnCheckCameraBeforeAggregationChanged(bool value)
        {
            _ = _databaseService.Config.SetConfigValueAsync("CheckCamera", value.ToString());
            _sessionService.CheckCamera = value;
        }
        partial void OnCheckPrinterBeforeAggregationChanged(bool value)
        {
            _ = _databaseService.Config.SetConfigValueAsync("CheckPrinter", value.ToString());
            _sessionService.CheckPrinter = value;
        }
        partial void OnCheckScannerBeforeAggregationChanged(bool value)
        {
            _ = _databaseService.Config.SetConfigValueAsync("CheckScanner", value.ToString());
            _sessionService.CheckScanner = value;
        }
        private async Task LoadSettingsAsync()
        {
            ServerUri = await _databaseService.Config.GetConfigValueAsync("ServerUri");
            PrinterIP = await _databaseService.Config.GetConfigValueAsync("PrinterIP");
            SelectedPrinterModel = await _databaseService.Config.GetConfigValueAsync("PrinterModel");
            ControllerIP = await _databaseService.Config.GetConfigValueAsync("ControllerIP");
            CameraIP = await _databaseService.Config.GetConfigValueAsync("CameraIP");
            SelectedCameraModel = await _databaseService.Config.GetConfigValueAsync("CameraModel");
            ScannerCOMPort = await _databaseService.Config.GetConfigValueAsync("ScannerCOMPort");
            DisableVirtualKeyboard = bool.TryParse(await _databaseService.Config.GetConfigValueAsync("DisableVirtualKeyboard"), out var parsed) && parsed;

            CheckCameraBeforeAggregation = await _databaseService.Config.GetConfigValueAsync("CheckCamera") == "True";
            CheckPrinterBeforeAggregation = await _databaseService.Config.GetConfigValueAsync("CheckPrinter") == "True";
            CheckControllerBeforeAggregation = await _databaseService.Config.GetConfigValueAsync("CheckController") == "True";
            CheckScannerBeforeAggregation = await _databaseService.Config.GetConfigValueAsync("CheckScanner") == "True";

            _sessionService.PrinterIP = PrinterIP;
            _sessionService.PrinterModel = SelectedPrinterModel;
            _sessionService.ControllerIP = ControllerIP;
            _sessionService.CameraIP = CameraIP;
            _sessionService.CameraModel = SelectedCameraModel;
            _sessionService.ScannerPort = ScannerCOMPort;
            // аналогично можно загрузить другие значения, если потребуется
            _sessionService.DisableVirtualKeyboard = DisableVirtualKeyboard;

            _sessionService.CheckCamera = CheckCameraBeforeAggregation;
            _sessionService.CheckPrinter = CheckPrinterBeforeAggregation;
            _sessionService.CheckController = CheckControllerBeforeAggregation;
            _sessionService.CheckScanner = CheckScannerBeforeAggregation;
        }

        public void LoadAvailableScanners()
        {
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                ScannerDevice scanerDevice = new ScannerDevice();
                scanerDevice.Id = port;
                AvailableScanners.Add(scanerDevice);
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



        [RelayCommand]
        private void TestServerConnection() { /* ... */ }







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

        //Контроллер - проверка соединения
        [RelayCommand]
        public async Task TestControllerConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(ControllerIP))
            {
                InfoMessage = "Введите IP контроллера!";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            try
            {
                await Task.Delay(300);
                await _databaseService.Config.SetConfigValueAsync("ControllerIP", ControllerIP);
                await _databaseService.Config.SetConfigValueAsync("CheckController", CheckControllerBeforeAggregation.ToString());

                _sessionService.ControllerIP = ControllerIP;
                _sessionService.CheckController = CheckControllerBeforeAggregation;

                InfoMessage = "Контроллер успешно сохранён!";
                _notificationService.ShowMessage(InfoMessage);
            }
            catch (Exception ex)
            {
                InfoMessage = $"Ошибка: {ex.Message}";
                _notificationService.ShowMessage(InfoMessage);
            }
        }

        //Камера - проверка соединения
        [RelayCommand]
        public async Task TestCameraConnectionAsync(CameraViewModel camera)
        {
            if (string.IsNullOrWhiteSpace(camera.CameraIP))
            {
                InfoMessage = "Введите IP камеры!";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            try
            {
                await Task.Delay(300); // simulate

                camera.IsConnected = true;

                await _databaseService.Config.SetConfigValueAsync("CameraIP", camera.CameraIP);
                await _databaseService.Config.SetConfigValueAsync("CameraModel", camera.SelectedCameraModel);
                await _databaseService.Config.SetConfigValueAsync("CheckCamera", CheckCameraBeforeAggregation.ToString());

                _sessionService.CameraIP = camera.CameraIP;
                _sessionService.CameraModel = camera.SelectedCameraModel;
                _sessionService.CheckCamera = CheckCameraBeforeAggregation;

                InfoMessage = $"Камера {camera.CameraIP} сохранена!";
                _notificationService.ShowMessage(InfoMessage);
            }
            catch (Exception ex)
            {
                camera.IsConnected = false;
                InfoMessage = $"Ошибка: {ex.Message}";
                _notificationService.ShowMessage(InfoMessage);
            }
        }

        //Принтер - проверка соединения
        [RelayCommand]
        public async Task TestPrinterConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(PrinterIP))
            {
                InfoMessage = "Введите IP принтера!";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            try
            {
                if (SelectedPrinterModel == "Zebra")
                {
                    var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger("PrinterTest");
                    var config = PrinterConfigBuilder.Build(PrinterIP);

                    var device = new PrinterTCP("SettingsPrinter", logger);
                    device.Configure(config); // использует IP из SessionService

                    device.StartWork();
                    _notificationService.ShowMessage("> Ожидание запуска...");

                    WaitingDeviceStateChange(device, DeviceStatusCode.Run, 10);
                    _notificationService.ShowMessage("> Принтер успешно запущен");

                    device.StopWork();
                    _notificationService.ShowMessage("> Ожидание остановки...");
                    WaitingDeviceStateChange(device, DeviceStatusCode.Ready, 10);

                    // сохраняем в БД
                    await _databaseService.Config.SetConfigValueAsync("PrinterIP", PrinterIP);
                    await _databaseService.Config.SetConfigValueAsync("PrinterModel", SelectedPrinterModel);
                    await _databaseService.Config.SetConfigValueAsync("CheckPrinter", CheckPrinterBeforeAggregation.ToString());

                    _sessionService.PrinterIP = PrinterIP;
                    _sessionService.PrinterModel = SelectedPrinterModel;
                    _sessionService.CheckPrinter = CheckPrinterBeforeAggregation;

                    InfoMessage = "Принтер успешно сохранён и проверен!";
                    _notificationService.ShowMessage(InfoMessage);
                }
                else
                {
                    InfoMessage = $"Поддержка модели принтера '{SelectedPrinterModel}' пока не реализована.";
                    _notificationService.ShowMessage(InfoMessage);
                    return;
                }
            }
            catch (Exception ex)
            {
                InfoMessage = $"Ошибка проверки принтера: {ex.Message}";
                _notificationService.ShowMessage(InfoMessage);
            }
        }

        private void WaitingDeviceStateChange(Device device, DeviceStatusCode state, int timeoutSeconds)
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);

            while (device.Status != state)
            {
                if (DateTime.UtcNow - startTime > timeout)
                {
                    throw new TimeoutException($"Ожидание состояния {state} превысило {timeoutSeconds} секунд.");
                }

                Thread.Sleep(100);
            }
        }

        //Сканер - проверка соединения
        [RelayCommand]
        public async Task TestScannerConnectionAsync()
        {
            if (SelectedScanner == null)
            {
                InfoMessage = "Сканер не выбран!";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            try
            {
                await _databaseService.Config.SaveScannerDeviceAsync(SelectedScanner);
                await _databaseService.Config.SetConfigValueAsync("ScannerCOMPort", SelectedScanner.Id);
                await _databaseService.Config.SetConfigValueAsync("CheckScanner", CheckScannerBeforeAggregation.ToString());

                _sessionService.ScannerPort = SelectedScanner.Id;
                _sessionService.CheckScanner = CheckScannerBeforeAggregation;

                InfoMessage = $"Сканер '{SelectedScanner.Id}' сохранён!";
                _notificationService.ShowMessage(InfoMessage);
            }
            catch (Exception ex)
            {
                InfoMessage = $"Ошибка: {ex.Message}";
                _notificationService.ShowMessage(InfoMessage);
            }
        }




        //не нужное в данном проекте, возможно пригодится
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
    }
}
