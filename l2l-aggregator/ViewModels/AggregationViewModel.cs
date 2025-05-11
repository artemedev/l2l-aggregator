using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DM_wraper_NS;
using FastReport;
using l2l_aggregator.Helpers;
using l2l_aggregator.Helpers.AggregationHelpers;
using l2l_aggregator.Models;
using l2l_aggregator.Services;
using l2l_aggregator.Services.AggregationService;
using l2l_aggregator.Services.Api;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.DmProcessing;
using l2l_aggregator.Services.Notification.Interface;
using l2l_aggregator.Services.Printing;
using l2l_aggregator.ViewModels.VisualElements;
using l2l_aggregator.Views.Popup;
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
using static FastReport.Export.Dbf.Record;

namespace l2l_aggregator.ViewModels
{
    public class TabItemModel
    {
        public string Header { get; set; }
        public string Content { get; set; }
        public string Content2 { get; set; }
    }
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
        private readonly PrintingService _printingService;

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
        //[ObservableProperty] private ObservableCollection<AggregatedItem> aggregatedItems = new();
        [ObservableProperty] private int selectedTabIndex;

        [ObservableProperty] private int aggregatedLayers;
        [ObservableProperty] private int aggregatedBoxes;
        [ObservableProperty] private int aggregatedPallets;
        [ObservableProperty] private string scannedBarcode;

        //состояние кнопок
        //Кнопока сканировать
        [ObservableProperty] private bool canScan = true;
        //Кнопока настройки шаблона
        [ObservableProperty] private bool canOpenTemplateSettings = true;
        //Кнопока печать этикетки
        [ObservableProperty] private bool canPrintLabel = false;
        //Очистить короб
        [ObservableProperty] private bool canClearBox = false;
        //Очистить паллету
        [ObservableProperty] private bool canClearPallet = false;
        //Завершить агрегацию
        [ObservableProperty] private bool canCompleteAggregation = false;
        //Fields это 
        public ObservableCollection<TemplateField> TemplateFields { get; } = new();
        public ObservableCollection<ScannedItem> ScannedData { get; } = new();
        public ObservableCollection<object> BoxAggregationData { get; set; } = new();
        public ObservableCollection<object> PalletAggregationData { get; set; } = new();

        private double scaleX, scaleY, scaleXObrat, scaleYObrat;

        FastReport.Export.Zpl.ZplExport? exporter;

        public JobConfiguration configZPL;
        public IRelayCommand<SizeChangedEventArgs> ImageSizeChangedCommand { get; }
        public IRelayCommand<SizeChangedEventArgs> ImageSizeCellChangedCommand { get; }
        public IRelayCommand<SizeChangedEventArgs> ImageSizeGridCellChangedCommand { get; }

        private string? _lastUsedTemplateJson;
        private int numberOfLayers;
        private int numberOfBoxes;
        private int numberOfPallets;

        ArmJobSsccResponse responseSscc;

        static result_data dmrData;
        public ObservableCollection<TabItemModel> Tabs { get; }
        //public int SelectedTabIndex { get; set; }
        [ObservableProperty] private string infoLayerText = "Выберите элементы шаблона для агрегации и нажмите кнопку сканировать!";
        [ObservableProperty] private string infoDMText = "Распознано 0 из 0";
        [ObservableProperty] private string infoHelperText;
        [ObservableProperty] private bool isHelperTextVisible;
        [ObservableProperty] private int currentLayer = 1;
        [ObservableProperty] private int currentBox = 1;
        [ObservableProperty] private int currentPallet = 1;
        //public TabItemModel SelectedTab => Tabs.ElementAtOrDefault(SelectedTabIndex);

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
            HistoryRouter<ViewModelBase> router,
            PrintingService printingService
            )
        {
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
            _printingService = printingService;

            ImageSizeChangedCommand = new RelayCommand<SizeChangedEventArgs>(OnImageSizeChanged);
            ImageSizeCellChangedCommand = new RelayCommand<SizeChangedEventArgs>(OnImageSizeCellChanged);
            ImageSizeGridCellChangedCommand = new RelayCommand<SizeChangedEventArgs>(OnImageSizeGridCellChanged);
            InitializeAsync();
        }
        private async void InitializeAsync()
        {
            //переделать на сервис
            var savedScanner = await _databaseService.Config.GetConfigValueAsync("ScannerCOMPort");
            if (savedScanner is not null)
            {
                var modelScanner = await _databaseService.Config.GetConfigValueAsync("ScannerModel");

                if (modelScanner == "Honeywell")
                {
                    _scannerWorker = new ScannerWorker(savedScanner);
                    _scannerWorker.BarcodeScanned += HandleScannedBarcode;
                    _scannerWorker.RunWorkerAsync();
                }
                else
                {
                    InfoMessage = $"Модель сканера '{modelScanner}' пока не поддерживается.";
                    _notificationService.ShowMessage(InfoMessage);
                }
            }
            numberOfLayers = _sessionService.SelectedTaskInfo.IN_BOX_QTY / _sessionService.SelectedTaskInfo.LAYERS_QTY;
            LoadTemplateFromSession(); //заполнение из шаблона в модальное окно для выбора элементов для сканирования
            InitializeSsccAsync();
        }
        private void LoadTemplateFromSession()
        {

            TemplateFields.Clear();
            var loadedFields = _templateService.LoadTemplate(_sessionService.SelectedTaskInfo.UN_TEMPLATE_FR);
            foreach (var f in loadedFields)
                TemplateFields.Add(f);

            IsTemplateLoaded = TemplateFields.Count > 0;

        }
       
        private async void InitializeSsccAsync()
        {
            responseSscc = await _dataApiService.LoadSsccAsync(_sessionService.SelectedTaskInfo.DOCID);
        }
        ~AggregationViewModel()
        {
            _scannerWorker?.Dispose();
        }

        [RelayCommand]
        public async Task Scan()
        {
            // Генерация шаблона
            var currentTemplate = _templateService.GenerateTemplate(TemplateFields.ToList());

            // Сравнение текущего шаблона с последним использованным
            if (_lastUsedTemplateJson != currentTemplate)
            {
                //отправка шаблона в библиотеку распознавания
                _dmScanService.StartScan(currentTemplate);
                _lastUsedTemplateJson = currentTemplate;
            }

            //старт распознавания
            _dmScanService.getScan();
            //ожидание результата распознавания
            dmrData = await _dmScanService.WaitForResultAsync();

            int minX = dmrData.BOXs.Min(d => d.poseX - (d.width / 2));
            int minY = dmrData.BOXs.Min(d => d.poseY - (d.height / 2));
            int maxX = dmrData.BOXs.Max(d => d.poseX + (d.width / 2));
            int maxY = dmrData.BOXs.Max(d => d.poseY + (d.height / 2));

            //кроп изображения
            ScannedImage = await _dmScanService.GetCroppedImage(dmrData, minX, minY, maxX, maxY);

            await Task.Delay(100); //исправить

            scaleX = imageSize.Width / ScannedImage.PixelSize.Width;
            scaleY = imageSize.Height / ScannedImage.PixelSize.Height;
            scaleXObrat = ScannedImage.PixelSize.Width / imageSize.Width;
            scaleYObrat = ScannedImage.PixelSize.Height / imageSize.Height;
             
            var responseSgtin = await _dataApiService.LoadSgtinAsync(_sessionService.SelectedTaskInfo.DOCID);
            //создание ячеек
            DMCells = _dmScanService.BuildCellViewModels(
                dmrData,
                scaleX,
                scaleY,
                _sessionService,
                TemplateFields,
                responseSgtin,
                this,
                minX, 
                minY
            );
            
            int validCountDMCells = DMCells.Count(c => c.IsValid);
            // Обновление информационных текстов
            UpdateLayerInfo(validCountDMCells, numberOfLayers);

            if (validCountDMCells == numberOfLayers)
            {
                CurrentLayer++;
                if (CurrentLayer == _sessionService.SelectedTaskInfo.LAYERS_QTY)
                {
                    CurrentLayer = 1;
                }
                //var currentLayer = AggregatedItems.FirstOrDefault(x => x.Type == "Слой" && !x.IsCompleted);
                //if (currentLayer != null)
                //{
                //    currentLayer.IsCompleted = true;

                //    // Автоматический переход, если это последний слой
                //    //if (AggregatedItems.Count(x => x.Type == "Слой" && x.IsCompleted) ==
                //    //    _sessionService.SelectedTaskInfo.LAYERS_QTY)
                //    //{
                //    //    GoToNextStep();
                //    //}
                //}
            }
        }

        private void UpdateLayerInfo(int validCount, int expectedPerLayer)
        {
            InfoLayerText = $"Слой {CurrentLayer} из {_sessionService.SelectedTaskInfo.LAYERS_QTY}. Распознано {validCount} из {expectedPerLayer}";
            InfoDMText = $"Распознано {validCount} из {expectedPerLayer}";

            IsHelperTextVisible = validCount == expectedPerLayer &&
                                CurrentLayer == _sessionService.SelectedTaskInfo.LAYERS_QTY;

            InfoHelperText = IsHelperTextVisible
                ? "Короб агрегирован. Запечатайте, наклейте этикетку и считайте сканером."
                : string.Empty;
        }
        //[RelayCommand]
        //public async Task ScanBoxBarcode()
        //{

        //}



        ////[RelayCommand]
        //public void CompleteStep()
        //{
        //    //if (CurrentStepIndex == 0)
        //    //    BoxAggregationData = new ObservableCollection<object>(ScannedData);
        //    //else if (CurrentStepIndex == 1)
        //    //    PalletAggregationData = new ObservableCollection<object>(BoxAggregationData);
        //}

        ////[RelayCommand]
        //public void GoToNextStep()
        //{
        //    //if (CurrentStepIndex < 2)
        //    //    CurrentStepIndex++;
        //    //if (SelectedTabIndex < 2)
        //    //    SelectedTabIndex++;
        //}


        //Печать этикетки
        [RelayCommand]
        public void PrintLabel()
        {
            //var reportXML = _templateService.LoadTemplate(_sessionService.SelectedTaskInfo.BOX_TEMPLATE);

            //
            byte[] frxBytes = Convert.FromBase64String(_sessionService.SelectedTaskInfo.BOX_TEMPLATE);
            _printingService.PrintReport(frxBytes);
        }
        //Очистить короб
        [RelayCommand]
        public void ClearBox()
        {
            //ScannedData.Clear();
        }
        //Очистить паллету
        [RelayCommand]
        public void ClearPallet()
        {
            //PalletAggregationData.Clear();
        }
        //Завершить агрегацию
        [RelayCommand]
        public void CompleteAggregation()
        { 

        }
        //отменить агрегацию
        [RelayCommand]
        public void CancelAggregation()
        {

        }


        //сканирование кода этикетки
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

                        //var currentLayer = AggregatedItems.FirstOrDefault(x => x.Type == "Коробка" && !x.IsCompleted);
                        //if (currentLayer != null)
                        //{
                        //    currentLayer.IsCompleted = true;

                        //    //// Автоматический переход, если это последний слой

                        //    GoToNextStep();
                        //}
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
                        //var currentLayer = AggregatedItems.FirstOrDefault(x => x.Type == "Паллета" && !x.IsCompleted);
                        //if (currentLayer != null)
                        //{
                        //    currentLayer.IsCompleted = true;

                        //    //// Автоматический переход, если это последний слой
                        //    _router.GoTo<TaskListViewModel>();
                        //}
                        //return;
                    }
                }
            }
            InfoMessage = $"ШК {barcode} не найден в списке!";
            _notificationService.ShowMessage(InfoMessage);
        }
        public async void OnCellClicked(DmCellViewModel cell)
        {
            int minX = dmrData.BOXs.Min(d => d.poseX - (d.width / 2));
            int minY = dmrData.BOXs.Min(d => d.poseY - (d.height / 2));
            int maxX = dmrData.BOXs.Max(d => d.poseX + (d.width / 2));
            int maxY = dmrData.BOXs.Max(d => d.poseY + (d.height / 2));

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
        [RelayCommand]
        public void OpenTemplateSettings()
        {
            var window = new TemplateSettingsWindow
            {
                DataContext = this
            };

            if (App.Current.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                window.ShowDialog(desktop.MainWindow);
            }
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
    }
}
