using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.SimpleRouter;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DM_wraper_NS;
using l2l_aggregator.Helpers.AggregationHelpers;
using l2l_aggregator.Models;
using l2l_aggregator.Models.AggregationModels;
using l2l_aggregator.Services;
using l2l_aggregator.Services.AggregationService;
using l2l_aggregator.Services.Api;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.DmProcessing;
using l2l_aggregator.Services.GS1ParserService;
using l2l_aggregator.Services.Notification.Interface;
using l2l_aggregator.Services.Printing;
using l2l_aggregator.ViewModels.VisualElements;
using l2l_aggregator.Views.Popup;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

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
        //Доступ к кнопоке печать этикетки коробки
        [ObservableProperty] private bool сanPrintBoxLabel = false;
        //Доступ к кнопоке печать этикетки паллеты
        [ObservableProperty] private bool сanPrintPalletLabel = false;
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

        private double scaleX, scaleY, scaleXObrat, scaleYObrat, scaleImagaeX, scaleImagaeY;

        FastReport.Export.Zpl.ZplExport? exporter;

        public JobConfiguration configZPL;
        public IRelayCommand<SizeChangedEventArgs> ImageSizeChangedCommand { get; }
        public IRelayCommand<SizeChangedEventArgs> ImageSizeCellChangedCommand { get; }
        public IRelayCommand<SizeChangedEventArgs> ImageSizeGridCellChangedCommand { get; }

        private string? _lastUsedTemplateJson;

        private int numberOfLayers;
        private int numberOfBoxes;
        private int numberOfPallets;
        private byte[] frxBoxBytes;
        private byte[] frxPalletBytes;
        ArmJobSsccResponse responseSscc;

        static result_data dmrData;


        [ObservableProperty] private string infoLayerText = "Выберите элементы шаблона для агрегации и нажмите кнопку сканировать!";
        [ObservableProperty] private string infoDMText = "Распознано 0 из 0";
        [ObservableProperty] private string infoHelperText;
        [ObservableProperty] private bool isHelperTextVisible;
        [ObservableProperty] private int currentLayer = 1;
        [ObservableProperty] private int currentBox = 1;
        [ObservableProperty] private int currentPallet = 1;


        [ObservableProperty]
        private bool recognizePack = true; // Добавление опции "распознавание коробки" в настройки распознавания

        public IEnumerable<RecognitionType> RecognitionTypes => Enum.GetValues(typeof(RecognitionType)).Cast<RecognitionType>();


        [ObservableProperty]
        private string aggregationSummaryText = "Результат агрегации пока не рассчитан.";


        //поле для запоминания предыдущего значения информации о агрегации для выхода из информации для клика по ячейке
        private string _previousAggregationSummaryText;


        //
        public class AggregationProgressModel
        {
            public int CurrentLayer { get; set; }
            public int CurrentBox { get; set; }
            public int CurrentPallet { get; set; }
            public List<AggregationCellData> DmCells { get; set; } = new();
        }

        public class AggregationCellData
        {
            public string DataMatrixData { get; set; }
            public bool IsValid { get; set; }
            public List<OcrCellData> Ocr { get; set; } = new();
        }

        public class OcrCellData
        {
            public string Name { get; set; }
            public string Text { get; set; }
            public bool IsValid { get; set; }
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
        private void InitializeFromSavedState()
        {
            if (_sessionService.HasUnfinishedAggregation)
            {
                try
                {
                    var progress = JsonSerializer.Deserialize<AggregationProgressModel>(_sessionService.LoadedProgressJson!);
                    if (progress != null)
                    {
                        if(_sessionService.LoadedTemplateJson != null)
                            _lastUsedTemplateJson = _sessionService.LoadedTemplateJson;

                        CurrentLayer = progress.CurrentLayer;
                        CurrentBox = progress.CurrentBox;
                        CurrentPallet = progress.CurrentPallet;
                        DMCells.Clear();
                        foreach (var cell in progress.DmCells)
                        {
                            var dmCell = new DmCellViewModel(this)
                            {
                                IsValid = cell.IsValid,
                                DmCell = new DmSquareViewModel
                                {
                                    Data = cell.DataMatrixData,
                                    IsValid = cell.IsValid
                                    // можно также X, Y, SizeWidth, SizeHeight, Angle — если есть
                                }
                            };

                            foreach (var ocr in cell.Ocr)
                            {
                                dmCell.OcrCells.Add(new SquareCellViewModel
                                {
                                    OcrName = ocr.Name,
                                    OcrText = ocr.Text,
                                    IsValid = ocr.IsValid
                                });
                            }

                            DMCells.Add(dmCell);
                        }
                        int validCountDMCells = DMCells.Count(c => c.IsValid);
                        AggregationSummaryText = $@"
Агрегируемая серия: {_sessionService.SelectedTaskInfo.RESOURCEID}
Количество собранных коробов: {CurrentBox - 1}
Номер собираемого короба: {CurrentBox}
Номер слоя: {CurrentLayer}
Количество слоев в коробе: {_sessionService.SelectedTaskInfo.LAYERS_QTY}
Количество СИ, распознанное в слое: {validCountDMCells}
Количество СИ, считанное в слое: {DMCells.Count}
Количество СИ, ожидаемое в слое: {numberOfLayers}";
                        //AggregationSummaryText = $"Загружено сохранённое состояние. Короб {CurrentBox}, Слой {CurrentLayer}, Паллет {CurrentPallet}.";
                    }
                }
                catch (Exception ex)
                {
                    _notificationService.ShowMessage($"Ошибка восстановления прогресса: {ex.Message}", NotificationType.Warn);
                }
            }
        }

        private async void InitializeAsync()
        {
            await _sessionService.InitializeAsync(_databaseService);
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
            InitializeSsccAsync();
            if (_sessionService.SelectedTaskInfo != null)
            {
                numberOfLayers = _sessionService.SelectedTaskInfo.IN_BOX_QTY / _sessionService.SelectedTaskInfo.LAYERS_QTY;
            }
            else
            {
                InfoMessage = "Ошибка: отсутствует информация о задании.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }
            //шаблон коробки
            frxBoxBytes = Convert.FromBase64String(_sessionService.SelectedTaskInfo.BOX_TEMPLATE);
            //шаблон паллеты
            frxPalletBytes = Convert.FromBase64String(_sessionService.SelectedTaskInfo.PALLETE_TEMPLATE);


            LoadTemplateFromSession(); //заполнение из шаблона в модальное окно для выбора элементов для сканирования

            InitializeFromSavedState();
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
            if (_sessionService.SelectedTaskInfo == null)
            {
                InfoMessage = "Не выбрано задание. Невозможно загрузить SSCC.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }
            responseSscc = await _dataApiService.LoadSsccAsync(_sessionService.SelectedTaskInfo.DOCID);
            if (responseSscc == null)
            {
                InfoMessage = "Ошибка загрузки SSCC данных.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }
            _sessionService.SelectedTaskSscc = responseSscc.RECORDSET.FirstOrDefault();
        }
        ~AggregationViewModel()
        {
            _scannerWorker?.Dispose();
        }

        [RelayCommand]
        public async Task Scan()
        {
            //bool StartScan = false;
            if (_sessionService.SelectedTaskInfo == null)
            {
                InfoMessage = "Ошибка: отсутствует информация о задании.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }
            //при сканировании это всегда перый шаг сканирования
            CurrentStepIndex = 0;
            // Генерация шаблона
            var currentTemplate = _templateService.GenerateTemplate(TemplateFields.ToList());

            // Сравнение текущего шаблона с последним использованным
            if (_lastUsedTemplateJson != currentTemplate)
            {
                // Определение, есть ли выбранные OCR или DM элементы
                bool hasOcr = TemplateFields.Any(f =>
                    f.IsSelected && (
                        f.Element.Name.LocalName == "TfrxMemoView" ||
                        f.Element.Name.LocalName == "TfrxTemplateMemoView"
                    )
                );

                bool hasDm = TemplateFields.Any(f =>
                    f.IsSelected && (
                        f.Element.Name.LocalName == "TfrxBarcode2DView" ||
                        f.Element.Name.LocalName == "TfrxTemplateBarcode2DView"
                    )
                );
                // Настройки параметров камеры для библиотеки распознавания
                var recognParams = new recogn_params
                {
                    countOfDM = numberOfLayers,
                    CamInterfaces = "GigEVision2",
                    cameraName = _sessionService.CameraIP,
                    _Preset = new camera_preset(_sessionService.CameraModel),
                    softwareTrigger = true,
                    hardwareTrigger = false,
                    OCRRecogn = hasOcr,
                    packRecogn = RecognizePack,
                    DMRecogn = hasDm
                };

                _dmScanService.ConfigureParams(recognParams);
                try
                {
                    //отправка шаблона в библиотеку распознавания
                    _dmScanService.StartScan(currentTemplate);
                    _lastUsedTemplateJson = currentTemplate;
                    //StartScan = true;
                    ////TODO WRAPTER swStartOk;
                    //Thread.Sleep(4000);
                    ////Thread.Sleep(1000);
                    ////старт распознавания
                    //_dmScanService.startShot();
                }
                catch (Exception ex)
                {
                    InfoMessage = $"Ошбика отправки шаблона {ex.Message}.";
                    _notificationService.ShowMessage(InfoMessage);
                }
            }
            if (_lastUsedTemplateJson != null)
            {
                try
                {
                    //старт распознавания
                    await _dmScanService.WaitForStartOkAsync();
                    _dmScanService.startShot();
                    //_dmScanService.startShot();
                    //ожидание результата распознавания
                    dmrData = await _dmScanService.WaitForResultAsync();
                    Console.WriteLine($"[INFO] Получено {dmrData.BOXs.Count} BOX элементов");

                    int i = 0;
                    foreach (var box in dmrData.BOXs)
                    {
                        Console.WriteLine($"--- BOX #{i} ---");
                        Console.WriteLine($"Position: ({box.poseX}, {box.poseY}), Size: {box.width}x{box.height}, Angle: {box.alpha}");
                        Console.WriteLine($"PackType: {box.packType}, Error: {box.isError}");

                        if (box.DM.data != null)
                        {
                            Console.WriteLine($"  DM Data: {box.DM.data}, Error: {box.DM.isError}");
                        }
                        else
                        {
                            Console.WriteLine($"  DM Data: null");
                        }
                        if (box.DM.poseX > 0)
                        {
                            Console.WriteLine($"  DM poseX: {box.DM.poseX}");
                        }
                        else
                        {
                            Console.WriteLine($"  DM poseX: 0");
                        }
                        if (box.DM.poseY > 0)
                        {
                            Console.WriteLine($"  DM poseY: {box.DM.poseY}");
                        }
                        else
                        {
                            Console.WriteLine($"  DM poseY: 0");
                        }
                        if (box.DM.width > 0)
                        {
                            Console.WriteLine($"  DM width: {box.DM.width}");
                        }
                        else
                        {
                            Console.WriteLine($"  DM width: 0");
                        }
                        if (box.DM.height > 0)
                        {
                            Console.WriteLine($"  DM height: {box.DM.height}");
                        }
                        else
                        {
                            Console.WriteLine($"  DM height: 0");
                        }
                        if (box.OCR != null && box.OCR.Count > 0)
                        {
                            foreach (var ocr in box.OCR)
                            {
                                if (ocr.Name != null)
                                {
                                    Console.WriteLine($"  OCR Name: {ocr.Name}, Text: {ocr.Text}, Pos: ({ocr.poseX}, {ocr.poseY}), Size: {ocr.width}x{ocr.height}, Angle: {ocr.alpha}, Error: {ocr.isError}");
                                }
                                else
                                {
                                    Console.WriteLine($"  OCR Name: null");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine($"  box.OCR: null");
                        }


                        i++;
                    }
                    Console.WriteLine($"----------------------------------");

                }
                catch (Exception ex)
                {
                    InfoMessage = $"Ошбика распознавания {ex.Message}.";
                    _notificationService.ShowMessage(InfoMessage);
                }
                //Сохраняем при сканировании в бд
                if (dmrData.rawImage != null)
                {
                    string taskInfoJson = JsonSerializer.Serialize(_sessionService.SelectedTaskInfo);
                    await _databaseService.AggregationState.SaveStateAsync(new AggregationState
                    {
                        Username = _sessionService.User.USER_NAME,
                        TaskId = _sessionService.SelectedTaskInfo.DOCID,
                        TemplateJson = _lastUsedTemplateJson,
                        TaskInfoJson = JsonSerializer.Serialize(_sessionService.SelectedTaskInfo),
                        ProgressJson = JsonSerializer.Serialize(new
                        {
                            CurrentLayer,
                            CurrentBox,
                            CurrentPallet,
                            DmCells = DMCells.Select(c => new
                            {
                                c.DmCell.Data,
                                c.IsValid,
                                Ocr = c.OcrCells.Select(o => new { o.OcrName, o.OcrText, o.IsValid })
                            })
                        })
                    });

                    double boxRadius = Math.Sqrt(dmrData.BOXs[0].height * dmrData.BOXs[0].height +
                              dmrData.BOXs[0].width * dmrData.BOXs[0].width) / 2;
                    int minX = (int)dmrData.BOXs.Min(d => d.poseX - boxRadius);
                    int minY = (int)dmrData.BOXs.Min(d => d.poseY - boxRadius);
                    int maxX = (int)dmrData.BOXs.Max(d => d.poseX + boxRadius);
                    int maxY = (int)dmrData.BOXs.Max(d => d.poseY + boxRadius);


                    minX = Math.Max(0, minX);
                    minY = Math.Max(0, minY);
                    maxX = Math.Min(dmrData.rawImage.Width, maxX);
                    maxY = Math.Min(dmrData.rawImage.Height, maxY);

                    // Освобождаем старое изображение перед новым
                    ScannedImage?.Dispose();
                    //кроп изображения
                    ScannedImage = await _dmScanService.GetCroppedImage(dmrData, minX, minY, maxX, maxY);


                    await Task.Delay(100); //исправить

                    scaleX = imageSize.Width / ScannedImage.PixelSize.Width;
                    scaleY = imageSize.Height / ScannedImage.PixelSize.Height;
                    scaleXObrat = ScannedImage.PixelSize.Width / imageSize.Width;
                    scaleYObrat = ScannedImage.PixelSize.Height / imageSize.Height;
                    // изображение из распознавания
                    //dmrData.rawImage = null;

                    var responseSgtin = await _dataApiService.LoadSgtinAsync(_sessionService.SelectedTaskInfo.DOCID);
                    if (responseSgtin == null)
                    {
                        InfoMessage = "Ошибка загрузки данных SGTIN.";
                        _notificationService.ShowMessage(InfoMessage);
                        return;
                    }
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        DMCells.Clear();
                        foreach (DmCellViewModel cell in _dmScanService.BuildCellViewModels(
                                                                    dmrData,
                                                                    scaleX,
                                                                    scaleY,
                                                                    _sessionService,
                                                                    TemplateFields,
                                                                    responseSgtin,
                                                                    this,
                                                                    minX,
                                                                    minY))
                        {
                            DMCells.Add(cell);
                        }
                    });

                    int validCountDMCells = DMCells.Count(c => c.IsValid);
                    // Обновление информационных текстов
                    UpdateLayerInfo(validCountDMCells, numberOfLayers);
                    AggregationSummaryText = $@"
Агрегируемая серия: {_sessionService.SelectedTaskInfo.RESOURCEID}
Количество собранных коробов: {CurrentBox - 1}
Номер собираемого короба: {CurrentBox}
Номер слоя: {CurrentLayer}
Количество слоев в коробе: {_sessionService.SelectedTaskInfo.LAYERS_QTY}
Количество СИ, распознанное в слое: {validCountDMCells}
Количество СИ, считанное в слое: {DMCells.Count}
Количество СИ, ожидаемое в слое: {numberOfLayers}";
                    canScan = true;
                    canOpenTemplateSettings = true;

                    if (CurrentLayer == _sessionService.SelectedTaskInfo.LAYERS_QTY)
                    {
                        if (validCountDMCells == DMCells.Count)
                        {
                            СanPrintBoxLabel = true;
                            if (validCountDMCells == numberOfLayers)
                            {

                                canScan = false;
                                canOpenTemplateSettings = false;
                                сanPrintBoxLabel = true;//сделать 
                                CurrentStepIndex = 1;
                                if (CurrentBox == _sessionService.SelectedTaskInfo.IN_PALLET_BOX_QTY)
                                {

                                    if (CurrentPallet == _sessionService.SelectedTaskInfo.PALLET_QTY)
                                    {
                                        //нужно сохранить 
                                        //!!!!!!!!!!!!!!!!!!!!!!!!!
                                    }
                                }
                            }
                            else
                            {
                                // CurrentLayer++;
                            }
                        }
                    }
                    else
                    {
                        if (validCountDMCells == numberOfLayers)
                        {

                        }
                    }
                }
                else
                {
                    InfoMessage = $"Изображение из распознавания не получено";
                    _notificationService.ShowMessage(InfoMessage);
                }


            }
            else
            {
                InfoMessage = $"Ошибка сканирования";
                _notificationService.ShowMessage(InfoMessage);
            }
        }

        private async Task SaveAggregationProgressAsync()
        {
            var progress = new AggregationProgressModel
            {
                CurrentBox = CurrentBox,
                CurrentLayer = CurrentLayer,
                CurrentPallet = CurrentPallet,
                DmCells = DMCells.Select(c => new AggregationCellData
                {
                    DataMatrixData = c.DmCell?.Data,
                    IsValid = c.IsValid,
                    Ocr = c.OcrCells.Select(o => new OcrCellData
                    {
                        Name = o.OcrName,
                        Text = o.OcrText,
                        IsValid = o.IsValid
                    }).ToList()
                }).ToList()
            };

            await _databaseService.AggregationState.SaveStateAsync(new AggregationState
            {
                Username = _sessionService.User?.USER_NAME ?? "unknown",
                TaskId = _sessionService.SelectedTaskInfo?.DOCID ?? 0,
                TemplateJson = _lastUsedTemplateJson ?? "",
                ProgressJson = JsonSerializer.Serialize(progress),
                LastUpdated = DateTime.Now
            });
        }

        private void UpdateLayerInfo(int validCount, int expectedPerLayer)
        {
            InfoLayerText = $"Слой {CurrentLayer} из {_sessionService.SelectedTaskInfo.LAYERS_QTY}. Распознано {validCount} из {expectedPerLayer}";
        }

        //Печать этикетки коробки
        [RelayCommand]
        public void PrintBoxLabel()
        {
            if (frxBoxBytes == null || frxBoxBytes.Length == 0)
            {
                InfoMessage = "Шаблон коробки не загружен.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }
            var boxRecord = responseSscc.RECORDSET
                           .Where(r => r.TYPEID == 0)
                           .ElementAtOrDefault(CurrentBox - 1);

            if (boxRecord != null)
            {
                _sessionService.SelectedTaskSscc = boxRecord;
                _printingService.PrintReport(frxBoxBytes, true);
            }
            else
            {
                InfoMessage = $"Не удалось найти запись коробки с индексом {CurrentBox - 1}.";
                _notificationService.ShowMessage(InfoMessage);
            }
        }
        //Печать этикетки паллеты
        [RelayCommand]
        public void PrintPalletLabel()
        {
            var boxRecord = responseSscc.RECORDSET
               .Where(r => r.TYPEID == 1)
               .ElementAtOrDefault(CurrentPallet - 1);

            if (boxRecord != null)
            {
                _sessionService.SelectedTaskSscc = boxRecord;
                _printingService.PrintReport(frxPalletBytes, false);
            }
            else
            {
                InfoMessage = $"Не удалось найти запись паллет с индексом {CurrentPallet - 1}.";
                _notificationService.ShowMessage(InfoMessage);
            }
        }

        //Очистить короб
        [RelayCommand]
        public void ClearBox()
        {
            CurrentStepIndex = 3;
        }

        //Очистить паллету
        [RelayCommand]
        public void ClearPallet()
        {
            CurrentStepIndex = 4;
        }

        //Завершить агрегацию
        [RelayCommand]
        public async Task CompleteAggregation()
        {
            await _databaseService.AggregationState.ClearStateAsync(_sessionService.User.USER_NAME);
            _notificationService.ShowMessage("Агрегация завершена.");
            _router.GoTo<TaskListViewModel>();
        }

        //отменить агрегацию
        [RelayCommand]
        public async Task CancelAggregation()
        {
            await _databaseService.AggregationState.ClearStateAsync(_sessionService.User.USER_NAME);
            _notificationService.ShowMessage("Агрегация завершена.");
            _router.GoTo<TaskListViewModel>();
        }

        //сканирование кода этикетки
        public void HandleScannedBarcode(string barcode)
        {

            // Проверка, что мы находимся на шаге 2
            if (CurrentStepIndex != 1 && CurrentStepIndex != 2)
                return;

            if (responseSscc?.RECORDSET == null || responseSscc.RECORDSET.Count == 0)
            {
                InfoMessage = "Данные SSCC отсутствуют.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            if (_sessionService.SelectedTaskSscc == null)
            {
                InfoMessage = "Данные SSCC отсутствуют.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }
            if (CurrentStepIndex == 1)
            {
                foreach (ArmJobSsccRecord resp in responseSscc.RECORDSET)
                {
                    //resp.TYPEID == 0 это тип коробки
                    if (resp.TYPEID == 0 && resp.DISPLAY_BAR_CODE == barcode)
                    {
                        //добавить сохранение
                        //!!!!!!!!!!!!!!!!!!!!!!!!!

                        //изменение состояния после сканирования
                        if (CurrentBox == _sessionService.SelectedTaskInfo.IN_PALLET_BOX_QTY)
                        {
                            сanPrintBoxLabel = false;
                            сanPrintPalletLabel = true;
                        }
                        CurrentBox++;
                        CurrentLayer = 1;

                        // Совпадение найдено
                        InfoMessage = $"Короб с ШК {barcode} успешно найден!";
                        _notificationService.ShowMessage(InfoMessage);
                        return;
                    }
                    else
                    {
                        InfoMessage = $"ШК {barcode} не найден в списке!";
                        _notificationService.ShowMessage(InfoMessage);
                    }
                }
            }
            if (CurrentStepIndex == 2)
            {
                foreach (ArmJobSsccRecord resp in responseSscc.RECORDSET)
                {
                    //resp.TYPEID == 1 это тип паллеты
                    if (resp.TYPEID == 1 && resp.DISPLAY_BAR_CODE == barcode)
                    {
                        //добавить сохранение
                        //!!!!!!!!!!!!!!!!!!!!!!!!!

                        //изменение состояния после сканирования
                        CurrentPallet++;
                        CurrentBox = 1;
                        CurrentLayer = 1;

                        CurrentStepIndex = 1;
                        // Совпадение найдено
                        InfoMessage = $"Короб с ШК {barcode} успешно найден!";
                        _notificationService.ShowMessage(InfoMessage);
                    }
                    else
                    {
                        InfoMessage = $"ШК {barcode} не найден в списке!";
                        _notificationService.ShowMessage(InfoMessage);
                    }
                }
            }

        }
        public async void OnCellClicked(DmCellViewModel cell)
        {
            _previousAggregationSummaryText = AggregationSummaryText; // Сохраняем старый текст

            double boxRadius = Math.Sqrt(dmrData.BOXs[0].height * dmrData.BOXs[0].height +
                         dmrData.BOXs[0].width * dmrData.BOXs[0].width) / 2;
            int minX = (int)dmrData.BOXs.Min(d => d.poseX - boxRadius);
            int minY = (int)dmrData.BOXs.Min(d => d.poseY - boxRadius);
            int maxX = (int)dmrData.BOXs.Max(d => d.poseX + boxRadius);
            int maxY = (int)dmrData.BOXs.Max(d => d.poseY + boxRadius);

            SelectedSquareImage = _imageProcessingService.CropImage(
                ScannedImage,
                cell.X,
                cell.Y,
                cell.SizeWidth,
                cell.SizeHeight,
                scaleXObrat,
                scaleYObrat,
                (float)cell.Angle
            );

            await Task.Delay(100);

            var scaleXCell = ImageSizeCell.Width / SelectedSquareImage.PixelSize.Width;
            var scaleYCell = ImageSizeCell.Height / SelectedSquareImage.PixelSize.Height;
            var shiftX = (ImageSizeGridCell.Width - ImageSizeCell.Width) / 2;
            var shiftY = (ImageSizeGridCell.Height - ImageSizeCell.Height) / 2;

            var newOcrList = new ObservableCollection<SquareCellViewModel>();

            foreach (var ocr in cell.OcrCells)
            {
                double newX = ocr.X;
                double newY = ocr.Y;

                newOcrList.Add(new SquareCellViewModel
                {
                    X = newX,
                    Y = newY,
                    SizeWidth = ocr.SizeWidth * scaleXCell,
                    SizeHeight = ocr.SizeHeight * scaleYCell,
                    IsValid = ocr.IsValid,
                    Angle = ocr.Angle
                });
            }

            cell.OcrCellsInPopUp.Clear();
            foreach (var newOcr in newOcrList)
                cell.OcrCellsInPopUp.Add(newOcr);

            //foreach(var ocr in cell.)
            IsPopupOpen = true;
            var gS1Parser = new GS1Parser();
            GS1_data newGS = gS1Parser.ParseGTIN(cell.DmCell?.Data);
            var GS1 = newGS.GS1isCorrect;
            var GTIN = newGS.GTIN;
            
            // Обновление текста
            AggregationSummaryText = $"""
GS1-код: {(GS1 ? "нет данных" : GS1)}
GTIN-код: {(string.IsNullOrWhiteSpace(GTIN) ? "нет данных" : GTIN)}
Валидность: {(cell.DmCell?.IsValid == true ? "Да" : "Нет")}
Координаты: {(cell.DmCell is { } dm1 ? $"({dm1.X:0.##}, {dm1.Y:0.##})" : "нет данных")}
Размер: {(cell.DmCell is { } dm ? $"({dm.SizeWidth:0.##} x {dm.SizeHeight:0.##})" : "нет данных")}
Угол: {(cell.DmCell?.Angle is double a ? $"{a:0.##}°" : "нет данных")}
OCR:
{(cell.OcrCells.Count > 0
? string.Join('\n', cell.OcrCells.Select(o =>
    $"- {(string.IsNullOrWhiteSpace(o.OcrName) ? "нет данных" : o.OcrName)}: {(string.IsNullOrWhiteSpace(o.OcrText) ? "нет данных" : o.OcrText)} ({(o.IsValid ? "валид" : "не валид")})"))
: "- нет данных")}
""";

            Console.WriteLine($"SelectedDmCell == : {SelectedDmCell}");
            Console.WriteLine($"OcrCellsInPopUp.Count: {cell.OcrCellsInPopUp.Count}");
            foreach (var ocr in cell.OcrCellsInPopUp)
            {
                Console.WriteLine($"OCR: X={ocr.X}, Y={ocr.Y}, W={ocr.SizeWidth}, H={ocr.SizeHeight}, Angle={ocr.Angle}");
            }
        }
        partial void OnIsPopupOpenChanged(bool value)
        {
            if (!value)
            {
                AggregationSummaryText = _previousAggregationSummaryText;
            }
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
