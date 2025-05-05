using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DM_wraper_NS;
using FastReport;
using l2l_aggregator.Helpers.AggregationHelpers;
using l2l_aggregator.Models;
using l2l_aggregator.Services;
using l2l_aggregator.Services.AggregationService;
using l2l_aggregator.Services.Api;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.DmProcessing;
using l2l_aggregator.Services.Notification.Interface;
using l2l_aggregator.ViewModels.VisualElements;
using MD.Devices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace l2l_aggregator.ViewModels
{
    public partial class AggregationViewModel : ViewModelBase
    {
        private readonly SessionService _sessionService;
        private readonly DataApiService _dataApiService;
        private readonly ImageHelper _imageProcessingService;
        private readonly TemplateService _templateService;
        private readonly DmScanService _dmScanService;
        private readonly ScannerListenerService _scannerListener;
        private readonly DatabaseService _databaseService;
        private readonly ScannerInputService _scannerInputService;
        private ScannerWorker _scannerWorker;
        private readonly INotificationService _notificationService;
        private readonly HistoryRouter<ViewModelBase> _router;

        private ILogger? logger;

        public IBrush BorderColor => !IsValid ? Brushes.Red : Brushes.Green;
        partial void OnIsValidChanged(bool value) => OnPropertyChanged(nameof(BorderColor));

        [ObservableProperty] private bool isValid;


        [ObservableProperty] private Avalonia.Size imageSize;
        [ObservableProperty] private Avalonia.Size imageSizeCell;
        [ObservableProperty] private Avalonia.Size imageSizeGridCell;

        [ObservableProperty] private double imageWidth;
        [ObservableProperty] private double imageHeight;
        [ObservableProperty] private double imageCellWidth;
        [ObservableProperty] private double imageCellHeight;

        [ObservableProperty] private int currentStepIndex;
        [ObservableProperty] private Bitmap scannedImage;
        [ObservableProperty] private string infoMessage;
        [ObservableProperty] private bool isTemplateLoaded;

        [ObservableProperty] private ObservableCollection<DmCellViewModel> dMCells = new();
        [ObservableProperty] private bool isPopupOpen;
        [ObservableProperty] private DmCellViewModel selectedDmCell;
        [ObservableProperty] private Bitmap selectedSquareImage;
        [ObservableProperty] private ObservableCollection<string> layers = new();
        [ObservableProperty] private ObservableCollection<string> palletBoxes = new();
        [ObservableProperty] private ObservableCollection<AggregatedItem> aggregatedItems = new();


        [ObservableProperty] private int aggregatedLayers;
        [ObservableProperty] private int aggregatedBoxes;
        [ObservableProperty] private int aggregatedPallets;
        [ObservableProperty] private string scannedBarcode;

        public ObservableCollection<TemplateField> Fields { get; } = new();
        public ObservableCollection<ScannedItem> ScannedData { get; } = new();
        public ObservableCollection<object> BoxAggregationData { get; set; } = new();
        public ObservableCollection<object> PalletAggregationData { get; set; } = new();

        private double scaleX, scaleY, scaleXObrat, scaleYObrat;

        FastReport.Export.Zpl.ZplExport? exporter;

        public JobConfiguration configZPL;
        public IRelayCommand<SizeChangedEventArgs> ImageSizeChangedCommand { get; }
        public IRelayCommand<SizeChangedEventArgs> ImageSizeCellChangedCommand { get; }
        public IRelayCommand<SizeChangedEventArgs> ImageSizeGridCellChangedCommand { get; }

        ArmJobSsccResponse responseSscc;

        static DM_result_data dmrData;
        private void LoadTemplateFromSession()
        {
            Fields.Clear();
            var loadedFields = _templateService.LoadTemplateFromBase64(_sessionService.SelectedTaskInfo.UN_TEMPLATE);
            foreach (var f in loadedFields)
                Fields.Add(f);

            IsTemplateLoaded = Fields.Count > 0;

        }
        private void InitializeAggregationStructure()
        {
            AggregatedItems.Clear();

            int layersQty = _sessionService.SelectedTaskInfo.LAYERS_QTY;
            int boxesQty = _sessionService.SelectedTaskInfo.IN_PALLET_BOX_QTY;
            int palletsQty = _sessionService.SelectedTaskInfo.PALLET_QTY;

            for (int i = 1; i <= layersQty; i++)
                AggregatedItems.Add(new AggregatedItem
                {
                    Name = $"Слой {i} из {layersQty}",
                    Type = "Слой",
                    Index = i,
                    Total = layersQty,
                    IsCompleted = false
                });

            for (int i = 1; i <= boxesQty; i++)
                AggregatedItems.Add(new AggregatedItem
                {
                    Name = $"Коробка {i} из {boxesQty}",
                    Type = "Коробка",
                    Index = i,
                    Total = boxesQty,
                    IsCompleted = false
                });

            for (int i = 1; i <= palletsQty; i++)
                AggregatedItems.Add(new AggregatedItem
                {
                    Name = $"Паллета {i} из {palletsQty}",
                    Type = "Паллета",
                    Index = i,
                    Total = palletsQty,
                    IsCompleted = false
                });
        }
        private async void InitializeSsccAsync()
        {
            responseSscc = await _dataApiService.LoadSsccAsync(_sessionService.SelectedTaskInfo.DOCID);
        }

        public AggregationViewModel(
            DataApiService dataApiService,
            ImageHelper imageProcessingService,
            SessionService sessionService,
            TemplateService templateService,
            DmScanService dmScanService,
            ScannerListenerService scannerListener,
            DatabaseService databaseService,
            ScannerInputService scannerInputService,
            INotificationService notificationService,
            HistoryRouter<ViewModelBase> router
            )
        {
            Debug.WriteLine("[AGGREGATION] AggregationViewModel создан");
            _sessionService = sessionService;
            _dataApiService = dataApiService;
            _imageProcessingService = imageProcessingService;
            _templateService = templateService;
            _dmScanService = dmScanService;
            _scannerListener = scannerListener;
            _databaseService = databaseService;
            _scannerInputService = scannerInputService;
            _notificationService = notificationService;
            _router = router;

            ImageSizeChangedCommand = new RelayCommand<SizeChangedEventArgs>(OnImageSizeChanged);
            ImageSizeCellChangedCommand = new RelayCommand<SizeChangedEventArgs>(OnImageSizeCellChanged);
            ImageSizeGridCellChangedCommand = new RelayCommand<SizeChangedEventArgs>(OnImageSizeGridCellChanged);
            //var savedScanner = _databaseService.Config.LoadScannerDeviceAsync();
            //_scannerWorker = new ScannerWorker(savedScanner.Id);

            //_scannerWorker.BarcodeScanned += HandleScannedBarcode;
            //_scannerWorker.RunWorkerAsync();

            //LoadTemplateFromSession();
            //InitializeAggregationStructure();
            //InitializeSsccAsync();
            InitializeAsync();
        }
        private async void InitializeAsync()
        {
            var savedScanner = await _databaseService.Config.LoadScannerDeviceAsync();
            if (savedScanner?.Id is not null)
            {
                _scannerWorker = new ScannerWorker(savedScanner.Id);
                _scannerWorker.BarcodeScanned += HandleScannedBarcode;
                _scannerWorker.RunWorkerAsync();
            }

            LoadTemplateFromSession();
            InitializeAggregationStructure();
            InitializeSsccAsync();
        }
        ~AggregationViewModel()
        {
            _scannerWorker?.Dispose();
        }

        [RelayCommand]
        public async Task Scan()
        {
            int expectedPerLayer = _sessionService.SelectedTaskInfo.IN_BOX_QTY / _sessionService.SelectedTaskInfo.LAYERS_QTY;

            _dmScanService.StartScan(GenerateTemplate());
            dmrData = await _dmScanService.WaitForResultAsync();

            ScannedImage = _dmScanService.GetCroppedImage(dmrData);

            await Task.Delay(100);

            scaleX = imageSize.Width / ScannedImage.PixelSize.Width;
            scaleY = imageSize.Height / ScannedImage.PixelSize.Height;
            scaleXObrat = ScannedImage.PixelSize.Width / imageSize.Width;
            scaleYObrat = ScannedImage.PixelSize.Height / imageSize.Height;

            var responseSgtin = await _dataApiService.LoadSgtinAsync(_sessionService.SelectedTaskInfo.DOCID);

            DMCells = _dmScanService.BuildCellViewModels(
                dmrData,
                scaleX,
                scaleY,
                _sessionService,
                responseSgtin,
                this
            );
            int validCount = DMCells.Count(c => c.IsValid);
            if (validCount >= expectedPerLayer)
            {
                var currentLayer = AggregatedItems.FirstOrDefault(x => x.Type == "Слой" && !x.IsCompleted);
                if (currentLayer != null)
                {
                    currentLayer.IsCompleted = true;

                    // Автоматический переход, если это последний слой
                    if (AggregatedItems.Count(x => x.Type == "Слой" && x.IsCompleted) ==
                        _sessionService.SelectedTaskInfo.LAYERS_QTY)
                    {
                        GoToNextStep();
                    }
                }
            }
        }

        [RelayCommand]
        public async Task ScanBoxBarcode()
        {

        }

        [RelayCommand]
        public void ClearBox() => ScannedData.Clear();

        [RelayCommand]
        public void CompleteStep()
        {
            if (CurrentStepIndex == 0)
                BoxAggregationData = new ObservableCollection<object>(ScannedData);
            else if (CurrentStepIndex == 1)
                PalletAggregationData = new ObservableCollection<object>(BoxAggregationData);
        }

        [RelayCommand]
        public void GoToNextStep()
        {
            if (CurrentStepIndex < 2)
                CurrentStepIndex++;
        }

        [RelayCommand]
        public void ClearPallet() => PalletAggregationData.Clear();

        [RelayCommand]
        public void PrintLabel()
        {
            var reportXML = _templateService.LoadTemplateFromBase64(_sessionService.SelectedTaskInfo.BOX_TEMPLATE);

            Report report = new Report();
            byte[] frxBytes = Convert.FromBase64String(_sessionService.SelectedTaskInfo.BOX_TEMPLATE);
            //report.Load(Convert.FromBase64String(Convert.ToString(_sessionService.SelectedTaskInfo.BOX_TEMPLATE)));
            using (MemoryStream ms = new MemoryStream(frxBytes))
            {
                report.Load(ms);
            }
            // Получаем данные из responseSscc
            var displayBarcode = responseSscc?.RECORDSET?.FirstOrDefault()?.DISPLAY_BAR_CODE ?? "";

            var labelData = new
            {
                DISPLAY_BAR_CODE = _sessionService.SelectedTaskSscc.DISPLAY_BAR_CODE,
                IN_BOX_QTY = _sessionService.SelectedTaskInfo.IN_BOX_QTY,
                MNF_DATE = _sessionService.SelectedTaskInfo.MNF_DATE_VAL,
                EXPIRE_DATE = _sessionService.SelectedTaskInfo.EXPIREDATE,
                SERIES_NAME = _sessionService.SelectedTaskInfo.SERIESNAME,
                PRINT_NAME = _sessionService.SelectedTaskInfo.RESOURCE_NAME,
                LEVEL_QTY = _sessionService.SelectedTaskInfo.QTY ?? 0,
                CNT = _sessionService.SelectedTaskInfo.RES_BOXID
            };
            // Регистрируем данные в отчете
            report.RegisterData(new List<object> { labelData }, "LabelQry");
            report.GetDataSource("LabelQry").Enabled = true;
            // Подготавливаем отчет
            report.Prepare();
            //foreach (var name in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            //{
            //    Console.WriteLine(name);
            //}
            //report.RegisterData(new List<MyLabelData> { BuildLabelData() }, "LabelData");
            //report.GetDataSource("LabelData").Enabled = true;
            if (_sessionService.PrinterModel == "Zebra")
            {
                PrintReportToNetworkPrinter(report);
            }
            else
            {
                _notificationService.ShowMessage($"Модель принтера '{_sessionService.PrinterModel}' не поддерживается.");
                return;
            }
        }

        [RelayCommand]
        public void CompleteAggregation()
        { }

        ///// <summary>
        ///// Конфигурация принтера
        ///// </summary>
        //private IConfiguration config
        //{
        //    get
        //    {

        //        var configBuilder = new ConfigurationBuilder();
        //        var baseDir = AppContext.BaseDirectory;
        //        var basePath = Directory.GetParent(baseDir)!.Parent!.Parent!.Parent!.FullName;
        //        var relativePath = Path.Combine(basePath, "Config", "ConfigCameraTcp.json");
        //        configBuilder.AddJsonFile(relativePath);
        //        //configBuilder.AddJsonFile("D:/MedtechtdApp/MedtechtdApp/Config/ConfigCameraTcp.json");
        //        return configBuilder.Build();
        //    }
        //}
        public void PrintReportToNetworkPrinter(Report report)
        {
            exporter = new FastReport.Export.Zpl.ZplExport();

            report.Prepare();
            using var exportStream = new MemoryStream();
            exporter.Export(report, exportStream);

            // Получаем байты
            byte[] zplBytes = exportStream.ToArray();
            var config = PrinterConfigBuilder.Build(_sessionService.PrinterIP);
            // Создаем и настраиваем устройство
            var device = new PrinterTCP("TestCamera", logger);
            device.Configure(config);
            device.StartWork();
            _notificationService.ShowMessage("> Ожидание запуска...");

            DeviceHelper.WaitForState(device, DeviceStatusCode.Run, 10);
            _notificationService.ShowMessage("> Устройство запущено");

            // Отправляем экспортированный ZPL отчет на принтер
            device.Send(zplBytes);
            Thread.Sleep(1000);// подождать для завершения отправки
            _notificationService.ShowMessage($"> Состояние устройства: {device.Status}");

            device.StopWork();
            _notificationService.ShowMessage("> Ожидание остановки...");
            DeviceHelper.WaitForState(device, DeviceStatusCode.Ready, 10);
            _notificationService.ShowMessage("> Работа с устройством остановлена ");

            _notificationService.ShowMessage("> Ждем остановки worker 2 сек...");
            Thread.Sleep(2000);

            _notificationService.ShowMessage($"> Устройство в состоянии {device.Status}.");
            _notificationService.ShowMessage("> Тест завершен");
        }

        ///// <summary>
        ///// Ожидание изменения состояния устройства.
        ///// Если время ожидания истекло, то тест завершается с ошибкой̆
        ///// </summary>
        ///// <param name="device">Экземпляр устройства</param>
        ///// <param name="state">Желаемое состояние устройства</param>
        ///// <param name="timeout">Время ожидания состояния устройства (в секундах)</param>
        //private void WaitingDeviceStateChange(Device device, DeviceStatusCode state, int timeout)
        //{
        //    var startTime = DateTime.UtcNow;
        //    var timeSpan = TimeSpan.FromSeconds(timeout);

        //    while (device.Status != state)
        //    {
        //        if (DateTime.UtcNow - startTime > timeSpan)
        //        {
        //            _notificationService.ShowMessage("> Время ожидания изменения состояния устройства истекло.");
        //            Assert.True(false);
        //        }
        //        Thread.Sleep(100);
        //    }

        //}

        public string GenerateTemplate()
        {
            return _templateService.GenerateTemplateBase64(Fields.ToList());
        }

        private void OnImageSizeChanged(SizeChangedEventArgs e)
        {
            imageWidth = e.NewSize.Width;
            imageHeight = e.NewSize.Height;
        }

        private void OnImageSizeCellChanged(SizeChangedEventArgs e)
        {
            imageCellWidth = e.NewSize.Width;
            imageCellHeight = e.NewSize.Height;
        }

        private void OnImageSizeGridCellChanged(SizeChangedEventArgs e)
        {
            imageCellWidth = e.NewSize.Width;
            imageCellHeight = e.NewSize.Height;
        }
        public void HandleScannedBarcode(string barcode)
        {
            // Проверка, что мы находимся на шаге 2
            if (CurrentStepIndex != 1 && CurrentStepIndex != 2)
                return;

            if (responseSscc == null || responseSscc.RECORDSET == null)
                return;
            if (CurrentStepIndex == 1)
            {
                foreach (ArmJobSsccRecord resp in responseSscc.RECORDSET)
                {
                    if (resp.TYPEID == 0 && resp.DISPLAY_BAR_CODE == barcode)
                    {
                        // Совпадение найдено
                        InfoMessage = $"Короб с ШК {barcode} успешно найден!";
                        _notificationService.ShowMessage(InfoMessage);

                        var currentLayer = AggregatedItems.FirstOrDefault(x => x.Type == "Коробка" && !x.IsCompleted);
                        if (currentLayer != null)
                        {
                            currentLayer.IsCompleted = true;

                            //// Автоматический переход, если это последний слой

                            GoToNextStep();
                        }
                        return;
                    }
                }
            }
            if (CurrentStepIndex == 2)
            {
                foreach (ArmJobSsccRecord resp in responseSscc.RECORDSET)
                {
                    if (resp.TYPEID == 1 && resp.DISPLAY_BAR_CODE == barcode)
                    {
                        var currentLayer = AggregatedItems.FirstOrDefault(x => x.Type == "Паллета" && !x.IsCompleted);
                        if (currentLayer != null)
                        {
                            currentLayer.IsCompleted = true;

                            //// Автоматический переход, если это последний слой
                            _router.GoTo<TaskListViewModel>();
                        }
                        return;
                    }
                }
            }
            InfoMessage = $"ШК {barcode} не найден в списке!";
            _notificationService.ShowMessage(InfoMessage);
        }
        public async void OnCellClicked(DmCellViewModel cell)
        {
            int minX = dmrData.DMdataArr.Min(d => d.poseX);
            int minY = dmrData.DMdataArr.Min(d => d.poseY);
            int maxX = dmrData.DMdataArr.Max(d => d.poseX + d.width);
            int maxY = dmrData.DMdataArr.Max(d => d.poseY + d.height);

            SelectedSquareImage = _imageProcessingService.CropImage(
                ScannedImage,
                cell.X,
                cell.Y,
                cell.SizeWidth,
                cell.SizeHeight,
                scaleXObrat,
                scaleYObrat
            );

            await Task.Delay(100);

            var scaleXCell = ImageSizeCell.Width / SelectedSquareImage.PixelSize.Width;
            var scaleYCell = ImageSizeCell.Height / SelectedSquareImage.PixelSize.Height;
            var shiftX = (ImageSizeGridCell.Width - ImageSizeCell.Width) / 2;
            var shiftY = (ImageSizeGridCell.Height - ImageSizeCell.Height) / 2;

            var newOcrList = new ObservableCollection<SquareCellViewModel>();

            foreach (var ocr in cell.OcrCells)
            {
                double newX = (((ocr.X - minX) - (cell.X / scaleX)) * scaleXCell) + shiftX;
                double newY = (((ocr.Y - minY) - (cell.Y / scaleX)) * scaleYCell) + shiftY;

                newOcrList.Add(new SquareCellViewModel
                {
                    X = newX,
                    Y = newY,
                    SizeWidth = ocr.SizeWidth * scaleXCell,
                    SizeHeight = ocr.SizeHeight * scaleYCell,
                    IsValid = ocr.IsValid
                });
            }

            cell.OcrCellsInPopUp.Clear();
            foreach (var newOcr in newOcrList)
                cell.OcrCellsInPopUp.Add(newOcr);

            IsPopupOpen = true;
        }

    }
}
