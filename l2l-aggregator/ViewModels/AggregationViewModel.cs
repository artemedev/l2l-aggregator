﻿using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
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
using l2l_aggregator.Services.ControllerService;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.DmProcessing;
using l2l_aggregator.Services.GS1ParserService;
using l2l_aggregator.Services.Notification.Interface;
using l2l_aggregator.Services.Printing;
using l2l_aggregator.ViewModels.VisualElements;
using l2l_aggregator.Views.Popup;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace l2l_aggregator.ViewModels
{
    public partial class AggregationViewModel : ViewModelBase
    {
        //сервис работы с сессией
        private readonly SessionService _sessionService;

        //сервис работы с api
        private readonly DataApiService _dataApiService;

        //сервис кропа изображения выбранной ячейки пользователем
        private readonly ImageHelper _imageProcessingService;

        //сервис обработки шаблона, после выбора пользователя элементов в ui. Для дальнейшей отправки в библиотеку распознавания
        private readonly TemplateService _templateService;

        //сервис обработки и работы с библиотекой распознавания, нужно сделать только чтобы была работа с распознавание, перенести обработку!!!!!!!!!!!!!!!!!!!!!!!!!
        private readonly DmScanService _dmScanService;

        //сервис работы с бд
        private readonly DatabaseService _databaseService;

        //сервис сканера через comport
        private ScannerWorker _scannerWorker;

        //сервис нотификаций
        private readonly INotificationService _notificationService;

        //сервис роутинга
        private readonly HistoryRouter<ViewModelBase> _router;

        //сервис принтера
        private readonly PrintingService _printingService;

        //для обновления размеров ячейки, UI
        public IRelayCommand<SizeChangedEventArgs> ImageSizeChangedCommand { get; }
        public IRelayCommand<SizeChangedEventArgs> ImageSizeCellChangedCommand { get; }
        [ObservableProperty] private Avalonia.Size imageSize;
        [ObservableProperty] private Avalonia.Size imageSizeCell;
        [ObservableProperty] private double imageWidth;
        [ObservableProperty] private double imageHeight;
        [ObservableProperty] private double imageCellWidth;
        [ObservableProperty] private double imageCellHeight;

        //валидация ячейки будет она красная или зеленая, UI
        [ObservableProperty] private bool isValid;

        //изображение слоя, UI
        [ObservableProperty] private Bitmap scannedImage;

        //Данные ячеек, UI
        [ObservableProperty] private ObservableCollection<DmCellViewModel> dMCells = new();

        //переменная для открытия всплывающего окна с изображением выбранной ячейки
        [ObservableProperty] private bool isPopupOpen;

        //перемення для отображения элементов в выбранной ячейке
        [ObservableProperty] private DmCellViewModel selectedDmCell;
        //изображение выбранной ячейки
        [ObservableProperty] private Bitmap selectedSquareImage;

        //состояние кнопок
        //Кнопока сканировать
        //для отслеживания состояния загрузки шаблона
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

        //переменная на каком мы шаге находимся, 1-агрегация пачек, 2 агрегация короба, 3 агрегация паллеты, 4 сканирование короба, 5 сканирование паллеты
        private int CurrentStepIndex;

        //переменная для сообщений нотификации
        private string InfoMessage;

        //элементы шаболона в список всплывающего окна 
        public ObservableCollection<TemplateField> TemplateFields { get; } = new();

        //переменные для высчитывания разницы между кропнутым изображением и изображением из интерфейса
        private double scaleX, scaleY, scaleXObrat, scaleYObrat;

        //переменная для сохранение шаблона при показе информацию из ячейки
        private string? _lastUsedTemplateJson;

        //переменная для колличества слоёв всего
        private int numberOfLayers;

        //переменная для колличества слоёв всего, не используются но нужны!!!!!
        private int numberOfBoxes;
        private int numberOfPallets;

        //переменная для шаблона коробки, для печати
        private byte[] frxBoxBytes;

        //переменная для шаблона паллеты, для печати
        private byte[] frxPalletBytes;

        //данные из api о sscc
        ArmJobSsccResponse responseSscc;

        //данные распознавания
        static result_data dmrData;


        //текущий слой
        [ObservableProperty] private int currentLayer = 1;
        //текущая коробка
        [ObservableProperty] private int currentBox = 1;
        //текущая паллета
        [ObservableProperty] private int currentPallet = 1;

        // Добавление опции "распознавание коробки" в настройки распознавания
        [ObservableProperty] private bool recognizePack = true;

        //информационное текстовое поле выше изображения 
        [ObservableProperty] private string infoLayerText = "Выберите элементы шаблона для агрегации и нажмите кнопку сканировать!";

        //информационное текстовое поле справа изображения
        [ObservableProperty] private string aggregationSummaryText = "Результат агрегации пока не рассчитан.";

        //поле для запоминания предыдущего значения информации о агрегации для выхода из информации для клика по ячейке
        private string _previousAggregationSummaryText;

        private int minX;
        private int minY;
        private int maxX;
        private int maxY;
        //модель для сохранения агрегационного состояния
        public class AggregationProgressModel
        {
            public int CurrentLayer { get; set; }
            public int CurrentBox { get; set; }
            public int CurrentPallet { get; set; }
            public List<DmCellViewModel> CurrentDmCells { get; set; } = new();
        }

        //-----------------------------------------------


        [ObservableProperty]
        private bool isControllerAvailable = true; // по умолчанию доступен

        private Image<Rgba32> _croppedImageRaw;

        private PcPlcConnectionService _plcConnection;
        private Timer _plcPingTimer;
        private readonly ILogger<PcPlcConnectionService> _logger;

        //состояние кнопок
        //Кнопка "Начать задание"
        [ObservableProperty] private bool canStartTask = true;

        //переменная для отслеживания состояния шаблона
        private bool templateOk = false;

        //Кнопка сканировать (hardware trigger)
        [ObservableProperty] private bool canScanHardware = false;

        //переменная для отслеживания состояния конфигурации камеры
        private bool cameraConfigured = false;

        private readonly DatabaseDataService _databaseDataService;

        public AggregationViewModel(
            DataApiService dataApiService,
            ImageHelper imageProcessingService,
            SessionService sessionService,
            TemplateService templateService,
            DmScanService dmScanService,
            ScannerListenerService scannerListener,
            DatabaseService databaseService,
            DatabaseDataService databaseDataService,
            ScannerInputService scannerInputService,
            INotificationService notificationService,
            HistoryRouter<ViewModelBase> router,
            PrintingService printingService,
            ILogger<PcPlcConnectionService> logger
            )
        {
            _sessionService = sessionService;
            _dataApiService = dataApiService;
            _imageProcessingService = imageProcessingService;
            _templateService = templateService;
            _dmScanService = dmScanService;
            _databaseService = databaseService;
            _notificationService = notificationService;
            _router = router;
            _printingService = printingService;
            _logger = logger;
            _databaseDataService = databaseDataService;


            ImageSizeChangedCommand = new RelayCommand<SizeChangedEventArgs>(OnImageSizeChanged);
            ImageSizeCellChangedCommand = new RelayCommand<SizeChangedEventArgs>(OnImageSizeCellChanged);


            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            ////инициализация предыдущего состояния если оно есть
            //InitializeFromSavedState();

            //инициализация сканера
            InitializeScanner();

            //инициализация в переменны
            InitializeNumberOfLayers();
            //
            InitializeSsccAsync();

            //заполнение из шаблона в модальное окно для выбора элементов для сканирования
            InitializeTemplate();

            //Периодическая проверка контроллера раз в 10 секунд в
            InitializeControllerPing();
        }

        private void InitializeFromSavedState()
        {
            if (_sessionService.HasUnfinishedAggregation)
            {
                try
                {
                    var progress = JsonSerializer.Deserialize<AggregationProgressModel>(_sessionService.AggregationState.ProgressJson!);
                    if (progress != null)
                    {
                        DMCells.Clear();
                        CurrentLayer = progress.CurrentLayer;
                        CurrentBox = progress.CurrentBox;
                        CurrentPallet = progress.CurrentPallet;


                        foreach (var c in progress.CurrentDmCells)
                        {
                            var cell = new DmCellViewModel(this)
                            {
                                X = c.X,
                                Y = c.Y,
                                SizeHeight = c.SizeHeight,
                                SizeWidth = c.SizeWidth,
                                Angle = c.Angle,
                                IsValid = c.IsValid,
                                Dm_data = c.Dm_data
                            };

                            foreach (var ocr in c.OcrCells)
                            {
                                cell.OcrCells.Add(ocr); // или создайте копии если нужно
                            }

                            DMCells.Add(cell);
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

        private void InitializeScanner()
        {
            var savedScanner = _sessionService.ScannerPort;

            if (savedScanner is not null)
            {
                var modelScanner = _sessionService.ScannerModel;

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
        }

        private void InitializeNumberOfLayers()
        {
            if (_sessionService.SelectedTaskInfo != null)
            {
                // Безопасное преобразование nullable int в int
                var inBoxQty = _sessionService.SelectedTaskInfo.IN_BOX_QTY ?? 0;
                var layersQty = _sessionService.SelectedTaskInfo.LAYERS_QTY ?? 1; // избегаем деления на ноль

                if (layersQty > 0)
                {
                    numberOfLayers = inBoxQty / layersQty;
                }
                else
                {
                    numberOfLayers = 0;
                    InfoMessage = "Ошибка: некорректное количество слоев (LAYERS_QTY).";
                    _notificationService.ShowMessage(InfoMessage);
                }


                // Проверяем, что BOX_TEMPLATE не null и не пустой
                if (_sessionService.SelectedTaskInfo.BOX_TEMPLATE != null)
                {
                    // Если BOX_TEMPLATE это byte[], то используем его напрямую
                    frxBoxBytes = _sessionService.SelectedTaskInfo.BOX_TEMPLATE;
                }
                else
                {
                    InfoMessage = "Ошибка: шаблон коробки отсутствует.";
                    _notificationService.ShowMessage(InfoMessage);
                }
            }
            else
            {
                InfoMessage = "Ошибка: отсутствует информация о задании.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }
            //var inBoxQty = _sessionService.SelectedTaskInfo.IN_BOX_QTY ?? 0;
            //var layersQty = _sessionService.SelectedTaskInfo.LAYERS_QTY ?? 1; // избегаем деления на ноль

            //if (layersQty > 0)
            //{
            //    numberOfLayers = inBoxQty / layersQty;
            //}
            //if (_sessionService.SelectedTaskInfo != null)
            //{
            //    numberOfLayers = _sessionService.SelectedTaskInfo.IN_BOX_QTY / _sessionService.SelectedTaskInfo.LAYERS_QTY;
            //    //шаблон коробки
            //    frxBoxBytes = _sessionService.SelectedTaskInfo.BOX_TEMPLATE;
            //    ////шаблон паллеты
            //    //frxPalletBytes = Convert.FromBase64String(_sessionService.SelectedTaskInfo.PALLETE_TEMPLATE);
            //}
            //else
            //{
            //    InfoMessage = "Ошибка: отсутствует информация о задании.";
            //    _notificationService.ShowMessage(InfoMessage);
            //    return;
            //}
        }

        private async void InitializeSsccAsync()
        {
            if (_sessionService.SelectedTaskInfo == null)
            {
                InfoMessage = "Не выбрано задание. Невозможно загрузить SSCC.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            // Безопасное преобразование nullable long в long
            var docId = _sessionService.SelectedTaskInfo.DOCID ?? 0;
            if (docId == 0)
            {
                InfoMessage = "Ошибка: некорректный ID документа.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            //responseSscc = await _dataApiService.LoadSsccAsync(docId);
            responseSscc = await _databaseDataService.GetSsccAsync(docId);
            if (responseSscc == null)
            {
                InfoMessage = "Ошибка загрузки SSCC данных.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }
            _sessionService.SelectedTaskSscc = responseSscc.RECORDSET.FirstOrDefault();
            //if (_sessionService.SelectedTaskInfo == null)
            //{
            //    InfoMessage = "Не выбрано задание. Невозможно загрузить SSCC.";
            //    _notificationService.ShowMessage(InfoMessage);
            //    return;
            //}
            //responseSscc = await _dataApiService.LoadSsccAsync(_sessionService.SelectedTaskInfo.DOCID);
            //if (responseSscc == null)
            //{
            //    InfoMessage = "Ошибка загрузки SSCC данных.";
            //    _notificationService.ShowMessage(InfoMessage);
            //    return;
            //}
            //_sessionService.SelectedTaskSscc = responseSscc.RECORDSET.FirstOrDefault();
        }

        private void InitializeTemplate()
        {
            TemplateFields.Clear();

            // Проверяем, что UN_TEMPLATE_FR не null
            if (_sessionService.SelectedTaskInfo?.UN_TEMPLATE_FR != null)
            {
                var loadedFields = _templateService.LoadTemplate(_sessionService.SelectedTaskInfo.UN_TEMPLATE_FR);
                foreach (var f in loadedFields)
                    TemplateFields.Add(f);
            }
            else
            {
                InfoMessage = "Ошибка: шаблон распознавания отсутствует.";
                _notificationService.ShowMessage(InfoMessage);
            }

            UpdateScanAvailability();
            //TemplateFields.Clear();
            //var loadedFields = _templateService.LoadTemplate(_sessionService.SelectedTaskInfo.UN_TEMPLATE_FR);
            //foreach (var f in loadedFields)
            //    TemplateFields.Add(f);

            ////CanScan = TemplateFields.Count > 0;
            //UpdateScanAvailability();

        }

        private async void InitializeControllerPing()
        {
            if (!_sessionService.CheckController || string.IsNullOrWhiteSpace(_sessionService.ControllerIP))
                return;

            try
            {
                _plcConnection = new PcPlcConnectionService(_logger);
                bool connected = await _plcConnection.ConnectAsync(_sessionService.ControllerIP);

                if (connected)
                {
                    // Запуск непрерывного мониторинга ping-pong
                    _plcConnection.StartPingPong(10000); // Каждые 10 секунд

                    // Подписка на изменения статуса соединения
                    _plcConnection.ConnectionStatusChanged += OnPlcConnectionStatusChanged;
                    _plcConnection.ErrorsReceived += OnPlcErrorsReceived;

                    IsControllerAvailable = true;
                    _notificationService.ShowMessage("Контроллер подключен и мониторинг активен");
                }
                else
                {
                    IsControllerAvailable = false;
                    _notificationService.ShowMessage("Не удалось подключиться к контроллеру", NotificationType.Error);
                }
            }
            catch (Exception ex)
            {
                IsControllerAvailable = false;
                _notificationService.ShowMessage($"Ошибка инициализации контроллера: {ex.Message}", NotificationType.Error);
            }
        }

        ~AggregationViewModel()
        {
            _scannerWorker?.Dispose();
        }

        partial void OnIsControllerAvailableChanged(bool value)
        {
            UpdateScanAvailability();
        }
        private void UpdateScanAvailability()
        {
            CanScan = IsControllerAvailable && TemplateFields.Count > 0 && templateOk;
            CanScanHardware = IsControllerAvailable && TemplateFields.Count > 0 && templateOk;
        }
        [RelayCommand]
        public async Task StartTask()
        {
            if (_sessionService.SelectedTaskInfo == null)
            {
                InfoMessage = "Ошибка: отсутствует информация о задании.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            CanStartTask = false; // Отключаем кнопку во время выполнения

            try
            {
                //отправляет шаблон распознавания в библиотеку
                templateOk = await SendTemplateToRecognizerAsync();

                if (templateOk)
                {
                    InfoLayerText = "Шаблон успешно отправлен. Теперь можно начинать сканирование!";
                    _notificationService.ShowMessage("Шаблон распознавания успешно настроен");
                }
                else
                {
                    InfoLayerText = "Ошибка отправки шаблона. Проверьте настройки и попробуйте снова.";
                    _notificationService.ShowMessage("Ошибка настройки шаблона распознавания", NotificationType.Error);
                    CanStartTask = true; // Включаем кнопку обратно при ошибке
                }
            }
            catch (Exception ex)
            {
                InfoMessage = $"Ошибка инициализации задания: {ex.Message}";
                _notificationService.ShowMessage(InfoMessage, NotificationType.Error);
                CanStartTask = true; // Включаем кнопку обратно при ошибке
                templateOk = false;
            }
            finally
            {
                UpdateScanAvailability();
            }
        }

        [RelayCommand]
        public async Task Scan()
        {
            if (_sessionService.SelectedTaskInfo == null)
            {
                InfoMessage = "Ошибка: отсутствует информация о задании.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }
            if (!templateOk)
            {
                InfoMessage = "Шаблон не инициализирован. Сначала нажмите 'Начать задание'.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            if (!await MoveCameraToCurrentLayerAsync())
                return; // Остановить, если позиционирование не удалось


            ////при сканировании это всегда перый шаг сканирования
            //CurrentStepIndex = 1;
            //_cellsReadyTcs = new TaskCompletionSource<bool>();

            ////отправляет шаблон распознавания в библиотеку
            //var templateOk = await SendTemplateToRecognizerAsync();
            //if (!templateOk)
            //    return;

            //выполняет процесс получения данных от распознавания и отображение результата в UI
            await StartScanningSoftwareAsync();

        }
        // Команда для hardware trigger сканирования
        [RelayCommand]
        public async Task ScanHardware()
        {
            if (_sessionService.SelectedTaskInfo == null)
            {
                InfoMessage = "Ошибка: отсутствует информация о задании.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            if (!templateOk)
            {
                InfoMessage = "Задание не инициализировано. Сначала нажмите 'Начать задание'.";
                _notificationService.ShowMessage(InfoMessage);
                return;
            }

            if (!await MoveCameraToCurrentLayerAsync())
                return; // Остановить, если позиционирование не удалось



            // Отправляем шаблон для hardware trigger режима
            if (!await SendTemplateToRecognizerAsync())
                return;

            // Запуск захвата фото через контроллер
            try
            {
                await _plcConnection.TriggerPhotoAsync();
                //выполняет процесс получения данных от распознавания и отображение результата в UI
                await StartScanningHardwareAsync();
            }
            catch (Exception ex)
            {
                InfoMessage = $"Ошибка hardware trigger: {ex.Message}";
                _notificationService.ShowMessage(InfoMessage);
            }
        }

        private void OnPlcConnectionStatusChanged(bool isConnected)
        {
            IsControllerAvailable = isConnected;

            if (!isConnected)
            {
                _notificationService.ShowMessage("Потеряно соединение с контроллером!", NotificationType.Error);
            }
            else
            {
                _notificationService.ShowMessage("Соединение с контроллером восстановлено");
            }
        }

        private void OnPlcErrorsReceived(PlcErrors errors)
        {
            string errorMessage = errors.GetErrorDescription();
            _notificationService.ShowMessage($"Ошибки контроллера: {errorMessage}", NotificationType.Error);
        }

        //работа с контроллером, выставление высоты
        private async Task<bool> MoveCameraToCurrentLayerAsync()
        {
            // Если отключена проверка контроллера — позиционирование не требуется
            if (!_sessionService.CheckController)
                return true;

            if (!IsControllerAvailable)
            {
                _notificationService.ShowMessage("Контроллер недоступен!");
                return false;
            }

            if (_sessionService.SelectedTaskInfo == null)
            {
                _notificationService.ShowMessage("Информация о задании отсутствует.");
                return false;
            }

            var packHeight = _sessionService.SelectedTaskInfo.PACK_HEIGHT ?? 0;
            var layersQty = _sessionService.SelectedTaskInfo.LAYERS_QTY ?? 0;

            if (packHeight == null || packHeight == 0)
            {
                _notificationService.ShowMessage("Ошибка: не задана высота слоя (PACK_HEIGHT).");
                return false;
            }
            if (layersQty == null || layersQty == 0)
            {
                _notificationService.ShowMessage("Ошибка: не задана колличество слёв (LAYERS_QTY).");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_sessionService.ControllerIP))
            {
                _notificationService.ShowMessage("IP контроллера не задан.");
                return false;
            }

            try
            {
                // Установка рабочих настроек бокса для текущей операции
                var boxSettings = new BoxWorkSettings
                {
                    CamBoxDistance = (ushort)(450 - ((CurrentLayer - 1) * packHeight)), // Настройка для текущего слоя
                    BoxHeight = (ushort)packHeight,
                    LayersQtty = (ushort)layersQty,
                    CamBoxMinDistance = 500
                };

                await _plcConnection.SetBoxWorkSettingsAsync(boxSettings);

                // Запуск шага цикла для текущего слоя
                await _plcConnection.StartCycleStepAsync((ushort)CurrentLayer);

                //// Запуск захвата фото
                //await _plcConnection.TriggerPhotoAsync();
                return true;
            }
            catch (Exception ex)
            {
                _notificationService.ShowMessage($"Ошибка позиционирования: {ex.Message}");
                return false;
            }
        }

        // Метод для подтверждения обработки фото в ПЛК
        //!!!!УЗНАТЬ ЧТО ОН ДЕЛАЕТ, ЕГО НУЖНО ОТПРАВЛЯТЬ ПОСЛЕ ПОЛКУЧЕНИЯ ИЗОБРАЖЕНИЯ, ИЛИ КОГДА ДАННЫЕ С КАДРА ХОРОШО СЧИТАЛИСЬ!!!!
        private async Task ConfirmPhotoToPlcAsync()
        {
            if (_plcConnection?.IsConnected == true)
            {
                try
                {
                    await _plcConnection.ConfirmPhotoProcessedAsync();
                }
                catch (Exception ex)
                {
                    _notificationService.ShowMessage($"Ошибка подтверждения фото: {ex.Message}");
                }
            }
        }


        //отправляет шаблон распознавания в библиотеку
        public async Task<bool> SendTemplateToRecognizerAsync()
        {
            // Генерация шаблона из ui
            var currentTemplate = _templateService.GenerateTemplate(TemplateFields.ToList());
            // Сравнение текущего шаблона с последним использованным
            if (_lastUsedTemplateJson != currentTemplate)
            {
                // Определение, есть ли выбранные OCR или DM элементы
                bool hasOcr = TemplateFields.Any(f =>
                    f.IsSelected && (
                        f.Element.Name.LocalName == "TfrxMemoView" ||
                        f.Element.Name.LocalName == "TfrxTemplateMemoView"
                    ));

                bool hasDm = TemplateFields.Any(f =>
                    f.IsSelected && (
                        f.Element.Name.LocalName == "TfrxBarcode2DView" ||
                        f.Element.Name.LocalName == "TfrxTemplateBarcode2DView"
                    ));
                // Настройки параметров камеры для библиотеки распознавания
                var recognParams = new recogn_params
                {
                    countOfDM = numberOfLayers,
                    CamInterfaces = "GigEVision2",
                    cameraName = _sessionService.CameraIP,
                    _Preset = new camera_preset(_sessionService.CameraModel),
                    softwareTrigger = true, //поменять на false
                    hardwareTrigger = true, //поменять на true
                    OCRRecogn = hasOcr,
                    packRecogn = RecognizePack,
                    DMRecogn = hasDm
                };
                _dmScanService.StopScan();
                //отправка настроек камеры в библиотеку распознавания
                _dmScanService.ConfigureParams(recognParams);

                try
                {

                    //отправка шаблона в библиотеку распознавания
                    _dmScanService.StartScan(currentTemplate);
                    _lastUsedTemplateJson = currentTemplate;
                    return true;
                }
                catch (Exception ex)
                {
                    InfoMessage = $"Ошбика отправки шаблона {ex.Message}.";
                    _notificationService.ShowMessage(InfoMessage);
                    return false;
                }
            }

            return true; // шаблон не изменился, можно использовать старый
        }

        //выполняет процесс получения данных от распознавания и отображение результата в UI.Software
        public async Task StartScanningSoftwareAsync()
        {
            if (_lastUsedTemplateJson == null)
            {
                _notificationService.ShowMessage("Шаблон не отправлен. Сначала выполните отправку шаблона.");
                return;
            }

            if (!await TryReceiveScanDataSoftwareAsync())
                return;

            if (!await TryCropImageAsync())
                return;

            if (!await TryBuildCellsAsync())
                return;

            UpdateInfoAndUI();
            //// Метод подтверждения обработки фотографий в ПЛК
            //await ConfirmPhotoToPlcAsync();

            // Сохранение агрегационного состояния
            // await SaveAggregationProgressAsync();
        }
        //выполняет процесс получения данных от распознавания и отображение результата в UI.Hardware
        public async Task StartScanningHardwareAsync()
        {
            if (_lastUsedTemplateJson == null)
            {
                _notificationService.ShowMessage("Шаблон не отправлен. Сначала выполните отправку шаблона.");
                return;
            }

            if (!await TryReceiveScanDataHardwareAsync())
                return;

            if (!await TryCropImageAsync())
                return;

            if (!await TryBuildCellsAsync())
                return;

            UpdateInfoAndUI();
            //// Метод подтверждения обработки фотографий в ПЛК
            //await ConfirmPhotoToPlcAsync();

            // Сохранение агрегационного состояния
            // await SaveAggregationProgressAsync();
        }

        // Получение данных распознавания. Software
        private async Task<bool> TryReceiveScanDataSoftwareAsync()
        {
            try
            {
                //старое
                //_dmScanService.getScan();
                //старт распознавания
                //await _dmScanService.WaitForStartOkAsync();
                _dmScanService.startShot();
                dmrData = await _dmScanService.WaitForResultAsync();
                return true;
            }
            catch (Exception ex)
            {
                InfoMessage = $"Ошибка распознавания: {ex.Message}";
                _notificationService.ShowMessage(InfoMessage);
                return false;
            }
        }
        // Получение данных распознавания. Hardware
        private async Task<bool> TryReceiveScanDataHardwareAsync()
        {
            try
            {
                //старое
                //_dmScanService.getScan();
                //старт распознавания
                //await _dmScanService.WaitForStartOkAsync();
                //_dmScanService.startShot();
                dmrData = await _dmScanService.WaitForResultAsync();
                return true;
            }
            catch (Exception ex)
            {
                InfoMessage = $"Ошибка распознавания: {ex.Message}";
                _notificationService.ShowMessage(InfoMessage);
                return false;
            }
        }

        // Кроп изображения
        private async Task<bool> TryCropImageAsync()
        {
            if (dmrData.rawImage == null)
            {
                InfoMessage = "Изображение из распознавания не получено.";
                _notificationService.ShowMessage(InfoMessage);
                return false;
            }

            double boxRadius = Math.Sqrt(dmrData.BOXs[0].height * dmrData.BOXs[0].height +
                                         dmrData.BOXs[0].width * dmrData.BOXs[0].width) / 2;

            minX = Math.Max(0, (int)dmrData.BOXs.Min(d => d.poseX - boxRadius));
            minY = Math.Max(0, (int)dmrData.BOXs.Min(d => d.poseY - boxRadius));
            maxX = Math.Min(dmrData.rawImage.Width, (int)dmrData.BOXs.Max(d => d.poseX + boxRadius));
            maxY = Math.Min(dmrData.rawImage.Height, (int)dmrData.BOXs.Max(d => d.poseY + boxRadius));

            //кроп изображения
            //ScannedImage = await _dmScanService.GetCroppedImage(dmrData, minX, minY, maxX, maxY);
            _croppedImageRaw = _imageProcessingService.GetCroppedImage(dmrData, minX, minY, maxX, maxY);

            // Освобождаем старое изображение перед новым
            ScannedImage?.Dispose();
            ScannedImage = _imageProcessingService.ConvertToAvaloniaBitmap(_croppedImageRaw);

            await Task.Delay(100); //исправить
            scaleX = imageSize.Width / ScannedImage.PixelSize.Width;
            scaleY = imageSize.Height / ScannedImage.PixelSize.Height;

            scaleXObrat = ScannedImage.PixelSize.Width / imageSize.Width;
            scaleYObrat = ScannedImage.PixelSize.Height / imageSize.Height;

            return true;
        }

        // Построение ячеек
        private async Task<bool> TryBuildCellsAsync()
        {
            var docId = _sessionService.SelectedTaskInfo?.DOCID ?? 0;
            if (docId == 0)
            {
                InfoMessage = "Ошибка: некорректный ID документа.";
                _notificationService.ShowMessage(InfoMessage);
                return false;
            }
            //var responseSgtin = await _dataApiService.LoadSgtinAsync(docId);
            var responseSgtin = await _databaseDataService.GetSgtinAsync(docId);
            if (responseSgtin == null)
            {
                InfoMessage = "Ошибка загрузки данных SGTIN.";
                _notificationService.ShowMessage(InfoMessage);
                return false;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                DMCells.Clear();
                foreach (var cell in _imageProcessingService.BuildCellViewModels(
                             dmrData,
                             scaleX,
                             scaleY,
                             _sessionService,
                             TemplateFields,
                             responseSgtin,
                             this,
                             minX, minY))
                {
                    DMCells.Add(cell);
                }
            });

            return true;
        }
        //Обновление UI и состояния
        private async void UpdateInfoAndUI()
        {
            int validCountDMCells = DMCells.Count(c => c.IsValid);
            // Обновление информационного текста выше изображения
            InfoLayerText = $"Слой {CurrentLayer} из {_sessionService.SelectedTaskInfo.LAYERS_QTY}. Распознано {validCountDMCells} из {numberOfLayers}";
            // Обновление информационного текста справа изображения
            AggregationSummaryText = $"""
Агрегируемая серия: {_sessionService.SelectedTaskInfo.RESOURCEID}
Количество собранных коробов: {CurrentBox - 1}
Номер собираемого короба: {CurrentBox}
Номер слоя: {CurrentLayer}
Количество слоев в коробе: {_sessionService.SelectedTaskInfo.LAYERS_QTY}
Количество СИ, распознанное в слое: {validCountDMCells}
Количество СИ, считанное в слое: {DMCells.Count}
Количество СИ, ожидаемое в слое: {numberOfLayers}
""";

            CanScan = true;
            CanOpenTemplateSettings = true;

            if (CurrentLayer == _sessionService.SelectedTaskInfo.LAYERS_QTY &&
                validCountDMCells == numberOfLayers)
            {
                CanScan = false;
                CanOpenTemplateSettings = false;
                СanPrintBoxLabel = true;
                // Метод подтверждения обработки фотографий в ПЛК
                await ConfirmPhotoToPlcAsync();
            }
        }
        //Сохранение состояния агрегации
        private void SaveAggregationProgress()
        {
            var progress = new AggregationProgressModel
            {
                CurrentBox = CurrentBox,
                CurrentLayer = CurrentLayer,
                CurrentPallet = CurrentPallet,
                CurrentDmCells = DMCells.Select(c => new DmCellViewModel(this)
                {
                    X = c.X,
                    Y = c.Y,
                    SizeHeight = c.SizeHeight,
                    SizeWidth = c.SizeWidth,
                    Angle = c.Angle,
                    IsValid = c.IsValid,
                    Dm_data = c.Dm_data,
                    OcrCells = c.OcrCells
                }).ToList()
            };

            _sessionService.AggregationState = new AggregationState
            {
                Username = _sessionService.User?.USER_NAME ?? "unknown",
                TaskId = _sessionService.SelectedTaskInfo?.DOCID ?? 0,
                TemplateJson = _lastUsedTemplateJson ?? "",
                ProgressJson = JsonSerializer.Serialize(progress),
                LastUpdated = DateTime.Now
            };
            //await _databaseService.AggregationState.SaveStateAsync(new AggregationState
            //{
            //    Username = _sessionService.User?.USER_NAME ?? "unknown",
            //    TaskId = _sessionService.SelectedTaskInfo?.DOCID ?? 0,
            //    TemplateJson = _lastUsedTemplateJson ?? "",
            //    ProgressJson = JsonSerializer.Serialize(progress),
            //    LastUpdated = DateTime.Now
            //});
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
        //[RelayCommand]
        //public void PrintPalletLabel()
        //{
        //    var boxRecord = responseSscc.RECORDSET
        //       .Where(r => r.TYPEID == 1)
        //       .ElementAtOrDefault(CurrentPallet - 1);

        //    if (boxRecord != null)
        //    {
        //        _sessionService.SelectedTaskSscc = boxRecord;
        //        _printingService.PrintReport(frxPalletBytes, false);
        //    }
        //    else
        //    {
        //        InfoMessage = $"Не удалось найти запись паллет с индексом {CurrentPallet - 1}.";
        //        _notificationService.ShowMessage(InfoMessage);
        //    }
        //}

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
            _dmScanService.StopScan();
            _notificationService.ShowMessage("Агрегация завершена.");
            _router.GoTo<TaskListViewModel>();
        }

        //отменить агрегацию
        [RelayCommand]
        public async Task CancelAggregation()
        {
            await _databaseService.AggregationState.ClearStateAsync(_sessionService.User.USER_NAME);
            _dmScanService.StopScan();
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
                            //сanPrintPalletLabel = true;
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

            var cropped = _imageProcessingService.CropImage(
                _croppedImageRaw,
                cell.X,
                cell.Y,
                cell.SizeWidth,
                cell.SizeHeight,
                scaleXObrat,
                scaleYObrat,
                (float)cell.Angle
            );
            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                SelectedSquareImage = _imageProcessingService.ConvertToAvaloniaBitmap(cropped);
                await Task.Delay(100);
            });

            var scaleXCell = ImageSizeCell.Width / SelectedSquareImage.PixelSize.Width;
            var scaleYCell = ImageSizeCell.Height / SelectedSquareImage.PixelSize.Height;


            var newOcrList = new ObservableCollection<SquareCellViewModel>();

            foreach (var ocr in cell.OcrCells)
            {
                double newX = ocr.X;
                double newY = ocr.Y;

                newOcrList.Add(new SquareCellViewModel
                {
                    X = ocr.X * scaleXCell,
                    Y = ocr.Y * scaleYCell,
                    SizeWidth = ocr.SizeWidth * scaleXCell,
                    SizeHeight = ocr.SizeHeight * scaleYCell,
                    IsValid = ocr.IsValid,
                    Angle = ocr.Angle,
                    OcrName = ocr.OcrName,
                    OcrText = ocr.OcrText
                });
            }
            // Добавим DM элемент (если есть)
            if (cell.Dm_data.Data != null)
            {
                newOcrList.Add(new SquareCellViewModel
                {
                    X = cell.Dm_data.X * scaleXCell,
                    Y = cell.Dm_data.Y * scaleYCell,
                    SizeWidth = cell.Dm_data.SizeWidth * scaleYCell,
                    SizeHeight = cell.Dm_data.SizeHeight * scaleYCell,
                    IsValid = cell.Dm_data.IsValid,
                    Angle = cell.Dm_data.Angle,
                    OcrName = "DM",
                    OcrText = cell.Dm_data.Data ?? "пусто"
                });
            }

            cell.OcrCellsInPopUp.Clear();
            foreach (var newOcr in newOcrList)
                cell.OcrCellsInPopUp.Add(newOcr);
            //cell.Angle = 87;
            //foreach(var ocr in cell.)
            IsPopupOpen = true;
            var GS1 = false;
            var GTIN = "";
            var DMData = "";
            var SerialNumber = "";
            if (cell.Dm_data?.Data != null)
            {
                var gS1Parser = new GS1Parser();
                GS1_data newGS = gS1Parser.ParseGTIN(cell.Dm_data?.Data);
                GS1 = newGS.GS1isCorrect;
                GTIN = newGS.GTIN;
                DMData = newGS.DMData;
                SerialNumber = newGS.SerialNumber;
            }
            bool isDuplicate = false;
            if (!string.IsNullOrWhiteSpace(cell.Dm_data?.Data))
            {
                var thisData = cell.Dm_data.Data;
                isDuplicate = DMCells
                    .Where(c => c != cell)
                    .Any(c => string.Equals(c.Dm_data?.Data, thisData, StringComparison.OrdinalIgnoreCase));
            }
            // Обновление текста
            AggregationSummaryText = $"""
GTIN-код: {(string.IsNullOrWhiteSpace(GTIN) ? "нет данных" : GTIN)}
SerialNumber-код: {(string.IsNullOrWhiteSpace(SerialNumber) ? "нет данных" : SerialNumber)}
Валидность: {(cell.Dm_data?.IsValid == true ? "Да" : "Нет")}
Дубликат: {(isDuplicate ? "Да" : "Нет")}
Координаты: {(cell.Dm_data is { } dm1 ? $"({dm1.X:0.##}, {dm1.Y:0.##})" : "нет данных")}
Размер: {(cell.Dm_data is { } dm ? $"({dm.SizeWidth:0.##} x {dm.SizeHeight:0.##})" : "нет данных")}
Угол: {(cell.Dm_data?.Angle is double a ? $"{a:0.##}°" : "нет данных")}
OCR:
{(cell.OcrCells.Count > 0
? string.Join('\n', cell.OcrCells.Select(o =>
    $"- {(string.IsNullOrWhiteSpace(o.OcrName) ? "нет данных" : o.OcrName)}: {(string.IsNullOrWhiteSpace(o.OcrText) ? "нет данных" : o.OcrText)} ({(o.IsValid ? "валид" : "не валид")})"))
: "- нет данных")}
""";
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _plcConnection?.StopPingPong();
                _plcConnection?.Disconnect();
                _plcConnection?.Dispose();
                _scannerWorker?.Dispose();
            }
            base.Dispose(disposing);
        }

    }

}
