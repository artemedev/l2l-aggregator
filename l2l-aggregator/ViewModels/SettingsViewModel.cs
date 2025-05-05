using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastReport.Utils;
using l2l_aggregator.Helpers;
using l2l_aggregator.Helpers.AggregationHelpers;
using l2l_aggregator.Models;
using l2l_aggregator.Services;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Database.Interfaces;
using l2l_aggregator.Services.Notification.Interface;
using l2l_aggregator.Services.ScannerService.Interfaces;
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

        [ObservableProperty]
        private ObservableCollection<string> _scannerModels = new() { "Honeywell" };

        [ObservableProperty]
        private string _selectedScannerModel;

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
        private readonly IScannerPortResolver _scannerResolver;
        public SettingsViewModel(DatabaseService databaseService, HistoryRouter<ViewModelBase> router, INotificationService notificationService, SessionService sessionService, IScannerPortResolver scannerResolver)
        {
            _notificationService = notificationService;
            _databaseService = databaseService;
            _router = router;
            _sessionService = sessionService;
            _scannerResolver = scannerResolver;

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
        partial void OnCheckControllerBeforeAggregationChanged(bool value)
        {
            _ = _databaseService.Config.SetConfigValueAsync("CheckController", value.ToString());
            SessionService.Instance.CheckController = value;
        }
        partial void OnCheckCameraBeforeAggregationChanged(bool value)
        {
            _ = _databaseService.Config.SetConfigValueAsync("CheckCamera", value.ToString());
            SessionService.Instance.CheckCamera = value;
        }
        partial void OnCheckPrinterBeforeAggregationChanged(bool value)
        {
            _ = _databaseService.Config.SetConfigValueAsync("CheckPrinter", value.ToString());
            SessionService.Instance.CheckPrinter = value;
        }
        partial void OnCheckScannerBeforeAggregationChanged(bool value)
        {
            _ = _databaseService.Config.SetConfigValueAsync("CheckScanner", value.ToString());
            SessionService.Instance.CheckScanner = value;
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
            SelectedScannerModel = await _databaseService.Config.GetConfigValueAsync("ScannerModel");

            //var session = SessionService.Instance;
            SessionService.Instance.PrinterIP = PrinterIP;
            SessionService.Instance.PrinterModel = SelectedPrinterModel;
            SessionService.Instance.ControllerIP = ControllerIP;
            SessionService.Instance.CameraIP = CameraIP;
            SessionService.Instance.CameraModel = SelectedCameraModel;
            SessionService.Instance.ScannerPort = ScannerCOMPort;
            // аналогично можно загрузить другие значения, если потребуется
            SessionService.Instance.DisableVirtualKeyboard = DisableVirtualKeyboard;

            SessionService.Instance.CheckCamera = CheckCameraBeforeAggregation;
            SessionService.Instance.CheckPrinter = CheckPrinterBeforeAggregation;
            SessionService.Instance.CheckController = CheckControllerBeforeAggregation;
            SessionService.Instance.CheckScanner = CheckScannerBeforeAggregation;
        }

        //public void LoadAvailableScanners()
        //{
        //    string[] ports = SerialPort.GetPortNames();
        //    foreach (string port in ports)
        //    {
        //        ScannerDevice scanerDevice = new ScannerDevice();
        //        scanerDevice.Id = port;
        //        AvailableScanners.Add(scanerDevice);
        //    }
        //}
        public void LoadAvailableScanners()
        {
            AvailableScanners.Clear();
            foreach (var port in _scannerResolver.GetHoneywellScannerPorts())
            {
                AvailableScanners.Add(new ScannerDevice { Id = port });
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

                SessionService.Instance.ControllerIP = ControllerIP;
                SessionService.Instance.CheckController = CheckControllerBeforeAggregation;

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

                SessionService.Instance.CameraIP = camera.CameraIP;
                SessionService.Instance.CameraModel = camera.SelectedCameraModel;
                SessionService.Instance.CheckCamera = CheckCameraBeforeAggregation;

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

                    DeviceHelper.WaitForState(device, DeviceStatusCode.Run, 10);
                    _notificationService.ShowMessage("> Принтер успешно запущен");

                    device.StopWork();
                    _notificationService.ShowMessage("> Ожидание остановки...");
                    DeviceHelper.WaitForState(device, DeviceStatusCode.Ready, 10);

                    // сохраняем в БД
                    await _databaseService.Config.SetConfigValueAsync("PrinterIP", PrinterIP);
                    await _databaseService.Config.SetConfigValueAsync("PrinterModel", SelectedPrinterModel);
                    await _databaseService.Config.SetConfigValueAsync("CheckPrinter", CheckPrinterBeforeAggregation.ToString());

                    SessionService.Instance.PrinterIP = PrinterIP;
                    SessionService.Instance.PrinterModel = SelectedPrinterModel;
                    SessionService.Instance.CheckPrinter = CheckPrinterBeforeAggregation;

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

        //Сканер - проверка соединения
        [RelayCommand]
        public async Task TestScannerConnectionAsync()
        {
            //if (SelectedScanner == null)
            //{
            //    InfoMessage = "Сканер не выбран!";
            //    _notificationService.ShowMessage(InfoMessage);
            //    return;
            //}

            //try
            //{
            //    await _databaseService.Config.SaveScannerDeviceAsync(SelectedScanner);
            //    await _databaseService.Config.SetConfigValueAsync("ScannerCOMPort", SelectedScanner.Id);
            //    await _databaseService.Config.SetConfigValueAsync("CheckScanner", CheckScannerBeforeAggregation.ToString());

            //    SessionService.Instance.ScannerPort = SelectedScanner.Id;
            //    SessionService.Instance.CheckScanner = CheckScannerBeforeAggregation;

            //    InfoMessage = $"Сканер '{SelectedScanner.Id}' сохранён!";
            //    _notificationService.ShowMessage(InfoMessage);
            //}
            //catch (Exception ex)
            //{
            //    InfoMessage = $"Ошибка: {ex.Message}";
            //    _notificationService.ShowMessage(InfoMessage);
            //}
            if (SelectedScanner == null)
            {
                InfoMessage = "Сканер не выбран!";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            try
            {
                if (SelectedScannerModel == "Honeywell")
                {
                    await _databaseService.Config.SaveScannerDeviceAsync(SelectedScanner);
                    await _databaseService.Config.SetConfigValueAsync("ScannerCOMPort", SelectedScanner.Id);
                    await _databaseService.Config.SetConfigValueAsync("ScannerModel", SelectedScannerModel);
                    await _databaseService.Config.SetConfigValueAsync("CheckScanner", CheckScannerBeforeAggregation.ToString());

                    SessionService.Instance.ScannerPort = SelectedScanner.Id;
                    SessionService.Instance.CheckScanner = CheckScannerBeforeAggregation;
                    SessionService.Instance.ScannerModel = SelectedScannerModel;

                    InfoMessage = $"Сканер '{SelectedScanner.Id}' сохранён!";
                    _notificationService.ShowMessage(InfoMessage);
                }
                else
                {
                    InfoMessage = $"Модель сканера '{SelectedScannerModel}' пока не поддерживается.";
                    _notificationService.ShowMessage(InfoMessage);
                }
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
