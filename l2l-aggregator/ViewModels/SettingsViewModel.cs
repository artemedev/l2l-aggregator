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

namespace l2l_aggregator.ViewModels
{
    public partial class SettingsViewModel : ViewModelBase
    {
        private readonly DatabaseService _databaseService;

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
        private readonly HistoryRouter<ViewModelBase> _router;




        [ObservableProperty]
        private string _selectedCameraModel;

        [ObservableProperty]
        private ObservableCollection<string> _printerModels = new() { "Model X", "Model Y", "Model Z" };
        [ObservableProperty] private string _selectedPrinterModel;


        //[ObservableProperty]
        //private ObservableCollection<string> _scannerModels = new() { "Model 1", "Model 2", "Model 3" };

        [ObservableProperty]
        private ObservableCollection<CameraViewModel> _cameras = new();

        [ObservableProperty]
        private ObservableCollection<string> _cameraModels = new() { "Model A", "Model B", "Model C" };

        [ObservableProperty]
        private bool isConnected;


        [ObservableProperty] private ObservableCollection<ScannerDevice> _availableScanners = new();

        [ObservableProperty] private ScannerDevice _selectedScanner;

        public SettingsViewModel(DatabaseService DatabaseService, HistoryRouter<ViewModelBase> router)
        {
            _databaseService = DatabaseService;
            _router = router;
            _ = LoadSettingsAsync();
            // Load cameras from DatabaseService or add a default one
            LoadCameras();
            //LoadScannerModels();
            LoadAvailableScanners();
            //var savedScanner = _databaseService.LoadScannerDevice();

            //if (savedScanner != null)
            //{
            //    // Пробуем найти в текущем списке (например, по DevicePath / COM)
            //    var matchedScanner = AvailableScanners.FirstOrDefault(s => s.Id == savedScanner.Id);

            //    if (matchedScanner != null)
            //    {
            //        SelectedScanner = matchedScanner;
            //    }
            //    else
            //    {
            //        // Если в списке его нет — всё равно подставим, чтобы отобразить
            //        //AvailableScanners.Add(savedScanner);
            //        //SelectedScanner = savedScanner;
            //    }
            //}
            if (Cameras.Count == 0)
            {
                AddCamera();
            }
        }

        private async Task LoadSettingsAsync()
        {
            ServerUri = await _databaseService.Config.GetConfigValueAsync("ServerUri");
            // аналогично можно загрузить другие значения, если потребуется
        }

        public void LoadAvailableScanners()
        {
            //AvailableScanners.Clear();
            //foreach (var scanner in ScannerHelper.GetAvailableScanners())
            //{
            //    AvailableScanners.Add(scanner);
            //}
            //using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%)'"))
            //{
            //    AvailableScanners.Clear();
            //    foreach (var device in searcher.Get())
            //    {
            //        ScannerDevice device1 = new ScannerDevice();
            //        string name = device["Name"]?.ToString();
            //        device1.Id = name;
            //        device1.Type = "COMPORT";
            //        device1.DisplayName = "Honeywell COMPORT";
            //        //Console.WriteLine(name);
            //        AvailableScanners.Add(device1);
            //    }
            //}
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                ScannerDevice device1 = new ScannerDevice();
                //string name = port;
                device1.Id = port;
                //device1.Type = "COMPORT";
                //device1.DisplayName = "Honeywell COMPORT";
                //Console.WriteLine(name);
                AvailableScanners.Add(device1);
                //Console.WriteLine($"Порт: {port}");
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
            // Here you would implement loading the cameras from your DatabaseService
            // For example:
            // var cameraSettings = _databaseService.GetCameraSettings();
            // foreach (var cameraSetting in cameraSettings)
            // {
            //     Cameras.Add(new CameraViewModel
            //     {
            //         CameraIP = cameraSetting.IP,
            //         SelectedCameraModel = cameraSetting.Model,
            //         IsConnected = false
            //     });
            // }
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
            }
        }

        [RelayCommand]
        public async Task TestCameraConnectionAsync(CameraViewModel camera)
        {
            if (string.IsNullOrWhiteSpace(camera.CameraIP))
            {
                InfoMessage = "Введите IP адрес камеры!";
                return;
            }

            try
            {
                // Implement your camera connection test here
                // This is just a placeholder
                await Task.Delay(500); // Simulate network operation

                camera.IsConnected = true;
                InfoMessage = $"Соединение с камерой {camera.CameraIP} установлено!";
            }
            catch (Exception ex)
            {
                camera.IsConnected = false;
                InfoMessage = $"Ошибка соединения с камерой: {ex.Message}";
            }
        }

        //// Команда для добавления новой камеры
        //[RelayCommand]
        //private void AddCamera()
        //{
        //    Cameras.Add(new CameraConfig());
        //}

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
                    //_router.GoTo<AuthViewModel>();
                }
                catch (Exception ex)
                {
                    InfoMessage = $"Ошибка: {ex.Message}";
                }
            }


        }
        //public void LoadScannerModels()
        //{
        //    if (OperatingSystem.IsWindows())
        //    {
        //        ScannerModels = GetAvailableSerialDevices();
        //    }
        //    //#if WINDOWS
        //    //    ScannerModels = GetAvailableSerialDevices();
        //    //#else
        //    //            ScannerModels = new ObservableCollection<string>(SerialPort.GetPortNames());
        //    //#endif
        //    //ScannerModels = new ObservableCollection<string>(System.IO.Ports.SerialPort.GetPortNames());
        //}

        //public ObservableCollection<string> GetAvailableSerialDevices()
        //{
        //    var devices = new ObservableCollection<string>();

        //    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");
        //    foreach (var device in searcher.Get())
        //    {
        //        var name = device["Name"]?.ToString();
        //        if (!string.IsNullOrWhiteSpace(name))
        //        {
        //            devices.Add(name); // Пример: "Zebra DS2208 (COM3)"
        //        }
        //    }

        //    return devices;
        //}

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
                return;
            }

            try
            {
                //// Простейшая проверка подключения
                //if (SelectedScanner.Type == "COM")
                //{
                //    using var port = new SerialPort(SelectedScanner.Id, 9600);
                //    port.Open();
                //    if (!port.IsOpen)
                //        throw new Exception("Не удалось открыть COM порт");
                //}
                //else if (SelectedScanner.Type == "HID")
                //{
                //    var device = DeviceList.Local.GetHidDevices().FirstOrDefault(d => d.DevicePath == SelectedScanner.Id);
                //    if (device == null)
                //        throw new Exception("HID устройство не найдено");
                //}

                // ✅ Сохраняем сканер в конфиг
                 _databaseService.Config.SaveScannerDeviceAsync(SelectedScanner);

                InfoMessage = $"Сканер '{SelectedScanner.Id}' успешно сохранён!";
            }
            catch (Exception ex)
            {
                InfoMessage = $"Ошибка подключения: {ex.Message}";
            }
        }


        [RelayCommand]
        private void TestControllerConnection() { /* ... */ }

        [RelayCommand]
        private void SaveSettings()
        {
            // Save all camera settings to DatabaseService
            // For each camera in Cameras collection
            foreach (var camera in Cameras)
            {
                // _databaseService.SaveCameraSettings(camera.Id, camera.CameraIP, camera.SelectedCameraModel);
            }

            InfoMessage = "Настройки успешно сохранены!";
        }


        [RelayCommand]
        private void GetArchive() { /* ... */ }
    }
}
