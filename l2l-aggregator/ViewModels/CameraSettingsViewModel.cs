using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DM_wraper_NS;
using l2l_aggregator.Services;
using l2l_aggregator.Services.ControllerService;
using l2l_aggregator.Services.DmProcessing;
using l2l_aggregator.Services.Notification;
using l2l_aggregator.Services.Notification.Interface;
using NModbus;
using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static l2l_aggregator.Services.ControllerService.PcPlcConnectionService;

namespace l2l_aggregator.ViewModels
{
    public partial class CameraSettingsViewModel : ViewModelBase
    {
        public string Title { get; set; } = "Настройка нулевой точки";

        private readonly DmScanService _dmScanService;
        private readonly HistoryRouter<ViewModelBase> _router;

        [ObservableProperty] private Bitmap scannedImage;
        public IRelayCommand<SizeChangedEventArgs> ImageSizeChangedCommand { get; }

        [ObservableProperty] private double imageWidth;
        [ObservableProperty] private double imageHeight;

        static result_data dmrData;

        //сервис работы с сессией
        private readonly SessionService _sessionService;

        //сервис нотификаций
        private readonly INotificationService _notificationService;

        [ObservableProperty] private int offsetFromZeroPosition;
        [ObservableProperty] private int positioningTimeout;
        [ObservableProperty] private int zeroToHomeDistance;
        [ObservableProperty] private bool arrowDirectionReversed;
        [ObservableProperty] private int movementSpeed;
        [ObservableProperty] private int minCameraToTableDistance;

        [ObservableProperty] private int lightIntensity;
        [ObservableProperty] private int lightDelay;
        [ObservableProperty] private int lightExposure;
        [ObservableProperty] private int triggerDelay;
        [ObservableProperty] private int lightMode;
        [ObservableProperty] private int pcTimeout;

        private readonly PcPlcConnectionService _plcConnection;
        public CameraSettingsViewModel(HistoryRouter<ViewModelBase> router, DmScanService dmScanService, SessionService sessionService, INotificationService notificationService, PcPlcConnectionService plcConnection)
        {
            _sessionService = sessionService;
            _notificationService = notificationService;
            _plcConnection = plcConnection;
            _router = router;
            _dmScanService = dmScanService;
            ImageSizeChangedCommand = new RelayCommand<SizeChangedEventArgs>(OnImageSizeChanged);
            _ = InitializeAsync();
        }
        [RelayCommand]
        public async Task Scan()
        {
            //старое
            //_dmScanService.getScan();
            //новое
            //await _dmScanService.WaitForStartOkAsync();
            //_dmScanService.startShot();
            dmrData = await _dmScanService.WaitForResultAsync();
            using (var ms = new MemoryStream())
            {
                dmrData.rawImage.SaveAsBmp(ms);
                ms.Seek(0, SeekOrigin.Begin);
                ScannedImage = new Bitmap(ms);
            }
        }
        private void OnImageSizeChanged(SizeChangedEventArgs e)
        {
            imageWidth = e.NewSize.Width;
            imageHeight = e.NewSize.Height;
        }
        [RelayCommand]
        public void GoBack()
        {
            _ = _plcConnection.StopAsync();
            // Переход на страницу назад
            _router.GoTo<SettingsViewModel>();
        }

        private async Task InitializeAsync()
        {
            await _plcConnection.StartAsync();

            if (!_plcConnection.IsConnected)
            {
                _notificationService?.ShowMessage("ПЛК недоступен. Проверьте подключение.");
                return;
            }

            await LoadFromPlcAsync();
        }

        [RelayCommand]
        public async Task LoadFromPlcAsync()
        {
            var settings = await _plcConnection.ReadCameraSettingsAsync();
            if (settings == null)
            {
                _notificationService?.ShowMessage("Ошибка чтения уставок из ПЛК.");
                return;
            }

            OffsetFromZeroPosition = settings.OffsetFromZeroPosition;
            PositioningTimeout = settings.PositioningTimeout;
            ZeroToHomeDistance = settings.ZeroToHomeDistance;
            ArrowDirectionReversed = settings.ArrowDirectionReversed;
            MovementSpeed = settings.MovementSpeed;
            MinCameraToTableDistance = settings.MinCameraToTableDistance;

            LightIntensity = settings.LightIntensity;
            LightDelay = settings.LightDelay;
            LightExposure = settings.LightExposure;
            TriggerDelay = settings.TriggerDelay;
            LightMode = settings.LightMode;
            PcTimeout = settings.PcTimeout;

            _notificationService?.ShowMessage("Уставки загружены из ПЛК.");
        }

        [RelayCommand]
        public async Task SaveToPlcAsync()
        {
            var settings = new CameraPlcSettings
            {
                OffsetFromZeroPosition = OffsetFromZeroPosition,
                PositioningTimeout = PositioningTimeout,
                ZeroToHomeDistance = ZeroToHomeDistance,
                ArrowDirectionReversed = ArrowDirectionReversed,
                MovementSpeed = MovementSpeed,
                MinCameraToTableDistance = MinCameraToTableDistance,
                LightIntensity = LightIntensity,
                LightDelay = LightDelay,
                LightExposure = LightExposure,
                TriggerDelay = TriggerDelay,
                LightMode = LightMode,
                PcTimeout = PcTimeout
            };

            bool success = await _plcConnection.WriteCameraSettingsAsync(settings);
            if (success)
                _notificationService?.ShowMessage("Уставки успешно сохранены в ПЛК.");
            else
                _notificationService?.ShowMessage("Ошибка записи уставок в ПЛК.");
        }

    }
}
