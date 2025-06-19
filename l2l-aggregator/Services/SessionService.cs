using l2l_aggregator.Models;
using l2l_aggregator.Models.AggregationModels;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Database.Repositories.Interfaces;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace l2l_aggregator.Services
{
    public class SessionService
    {
        private static SessionService? _instance;
        public static SessionService Instance => _instance ??= new SessionService();

        private static IConfigRepository? _configRepo;

        public void SetConfigRepository(IConfigRepository config)
        {
            _configRepo = config;
        }

        private async void SaveSettingToDb(string key, string? value)
        {
            if (_configRepo != null && value != null)
            {
                await _configRepo.SetConfigValueAsync(key, value);
            }
        }

        private T SetAndSave<T>(ref T field, T value, string key)
        {
            field = value;
            SaveSettingToDb(key, value?.ToString());
            return field;
        }

        // ---------------- Device Settings ----------------
        private bool _disableVirtualKeyboard;
        public bool DisableVirtualKeyboard
        {
            get => _disableVirtualKeyboard;
            set => SetAndSave(ref _disableVirtualKeyboard, value, "DisableVirtualKeyboard");
        }

        private string? _scannerPort;
        public string? ScannerPort
        {
            get => _scannerPort;
            set => SetAndSave(ref _scannerPort, value, "ScannerCOMPort");
        }

        private string? _cameraIP;
        public string? CameraIP
        {
            get => _cameraIP;
            set => SetAndSave(ref _cameraIP, value, "CameraIP");
        }

        private string? _cameraModel;
        public string? CameraModel
        {
            get => _cameraModel;
            set => SetAndSave(ref _cameraModel, value, "CameraModel");
        }

        private string? _printerIP;
        public string? PrinterIP
        {
            get => _printerIP;
            set => SetAndSave(ref _printerIP, value, "PrinterIP");
        }

        private string? _printerModel;
        public string? PrinterModel
        {
            get => _printerModel;
            set => SetAndSave(ref _printerModel, value, "PrinterModel");
        }

        private string? _controllerIP;
        public string? ControllerIP
        {
            get => _controllerIP;
            set => SetAndSave(ref _controllerIP, value, "ControllerIP");
        }

        private string? _scannerModel;
        public string? ScannerModel
        {
            get => _scannerModel;
            set => SetAndSave(ref _scannerModel, value, "ScannerModel");
        }

        // ---------------- Device Check Flags ----------------
        private bool _checkCamera;
        public bool CheckCamera
        {
            get => _checkCamera;
            set => SetAndSave(ref _checkCamera, value, "CheckCamera");
        }

        private bool _checkPrinter;
        public bool CheckPrinter
        {
            get => _checkPrinter;
            set => SetAndSave(ref _checkPrinter, value, "CheckPrinter");
        }

        private bool _checkController;
        public bool CheckController
        {
            get => _checkController;
            set => SetAndSave(ref _checkController, value, "CheckController");
        }

        private bool _checkScanner;
        public bool CheckScanner
        {
            get => _checkScanner;
            set => SetAndSave(ref _checkScanner, value, "CheckScanner");
        }

        // ---------------- Database Connection (Read-only) ----------------
        /// <summary>
        /// Статичный адрес базы данных (только для чтения)
        /// </summary>
        public string DatabaseUri => "Server=172.16.3.237;Port=3050;Database=/DATA_SSD/fb/arm_mtd_test.fdb;User=SYSDBA;Password=masterkey;";

        /// <summary>
        /// Отображаемая информация о базе данных
        /// </summary>
        public string DatabaseDisplayInfo => "172.16.3.237:3050";

        // ---------------- Device Registration Info ----------------
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }

        // ---------------- User Session Data ----------------
        public UserAuthResponse? User { get; set; }
        public bool IsAdmin { get; set; }

        // ---------------- Current Task Data ----------------
        public ArmJobRecord? SelectedTask { get; set; }
        public ArmJobInfoRecord? SelectedTaskInfo { get; set; }
        public ArmJobSsccRecord? SelectedTaskSscc { get; set; }
        public ArmJobSgtinRecord? SelectedTaskSgtin { get; set; }

        // ---------------- Aggregation State ----------------
        public AggregationState? AggregationState { get; set; }

        /// <summary>
        /// Проверяет, есть ли незавершенная агрегация
        /// </summary>
        public bool HasUnfinishedAggregation => SelectedTaskInfo != null &&
            AggregationState != null &&
            !string.IsNullOrWhiteSpace(AggregationState.TemplateJson) &&
            !string.IsNullOrWhiteSpace(AggregationState.ProgressJson);

        // ---------------- Session Management ----------------
        public long? CurrentSessionId { get; set; }

        /// <summary>
        /// Инициализация настроек сессии
        /// </summary>
        public async Task InitializeAsync(DatabaseService db)
        {
            SetConfigRepository(db.Config);
            await InitializeCheckFlagsAsync(db);

            var config = db.Config;

            // Загружаем настройки устройств
            PrinterIP = await config.GetConfigValueAsync("PrinterIP");
            PrinterModel = await config.GetConfigValueAsync("PrinterModel");
            ControllerIP = await config.GetConfigValueAsync("ControllerIP");
            CameraIP = await config.GetConfigValueAsync("CameraIP");
            CameraModel = await config.GetConfigValueAsync("CameraModel");
            ScannerPort = await config.GetConfigValueAsync("ScannerCOMPort");
            ScannerModel = await config.GetConfigValueAsync("ScannerModel");
            DisableVirtualKeyboard = bool.TryParse(await config.GetConfigValueAsync("DisableVirtualKeyboard"), out var vkParsed) && vkParsed;

            // Загружаем информацию об устройстве
            DeviceId = await config.GetConfigValueAsync("DeviceId");
            DeviceName = await config.GetConfigValueAsync("DeviceName");

            // Загружаем последнего авторизованного пользователя
            var users = await db.UserAuth.GetUserAuthAsync();
            if (users != null && users.Count > 0)
            {
                User = users.FirstOrDefault();

                // Если есть пользователь, загружаем его состояние агрегации
                if (User != null)
                {
                    await LoadAggregationStateAsync(db);
                }
            }
        }

        /// <summary>
        /// Инициализация флагов проверки устройств
        /// </summary>
        private async Task InitializeCheckFlagsAsync(DatabaseService db)
        {
            var config = db.Config;

            CheckCamera = await LoadOrInitBool(config, "CheckCamera", true);
            CheckPrinter = await LoadOrInitBool(config, "CheckPrinter", true);
            CheckController = await LoadOrInitBool(config, "CheckController", true);
            CheckScanner = await LoadOrInitBool(config, "CheckScanner", true);
        }

        /// <summary>
        /// Загружает или инициализирует булево значение в конфигурации
        /// </summary>
        private async Task<bool> LoadOrInitBool(IConfigRepository config, string key, bool defaultValue)
        {
            var value = await config.GetConfigValueAsync(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                await config.SetConfigValueAsync(key, defaultValue.ToString());
                return defaultValue;
            }

            return bool.TryParse(value, out var parsed) && parsed;
        }

        /// <summary>
        /// Загружает состояние агрегации для текущего пользователя
        /// </summary>
        public async Task LoadAggregationStateAsync(DatabaseService db)
        {
            if (User == null || string.IsNullOrWhiteSpace(User.USER_NAME))
                return;

            try
            {
                var state = await db.AggregationState.LoadStateAsync(User.USER_NAME);
                if (state != null)
                {
                    SelectedTaskInfo = JsonSerializer.Deserialize<ArmJobInfoRecord>(state.TaskInfoJson);
                    AggregationState = state;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при загрузке состояния агрегации", ex);
            }
        }

        /// <summary>
        /// Сохраняет информацию об устройстве в настройках
        /// </summary>
        public async Task SaveDeviceInfoAsync(string deviceId, string deviceName)
        {
            DeviceId = deviceId;
            DeviceName = deviceName;

            if (_configRepo != null)
            {
                await _configRepo.SetConfigValueAsync("DeviceId", deviceId);
                await _configRepo.SetConfigValueAsync("DeviceName", deviceName);
            }
        }

        /// <summary>
        /// Очищает данные пользователя при выходе
        /// </summary>
        public async Task ClearUserDataAsync()
        {
            User = null;
            IsAdmin = false;
            CurrentSessionId = null;
            SelectedTask = null;
            SelectedTaskInfo = null;
            SelectedTaskSscc = null;
            SelectedTaskSgtin = null;
            AggregationState = null;

            // Очищаем сохраненную информацию о пользователе в локальной БД
            if (_configRepo != null)
            {
                // Можно добавить очистку конкретных пользовательских настроек если нужно
            }
        }

        /// <summary>
        /// Получает полную информацию о конфигурации для отладки
        /// </summary>
        public string GetConfigurationSummary()
        {
            return $@"
=== Конфигурация сессии ===
База данных: {DatabaseDisplayInfo}
Устройство: {DeviceName} (ID: {DeviceId})
Пользователь: {User?.USER_NAME ?? "Не авторизован"}
Админ: {IsAdmin}
Сессия: {CurrentSessionId?.ToString() ?? "Нет"}
Незавершенная агрегация: {HasUnfinishedAggregation}

=== Настройки устройств ===
Сканер: {ScannerModel} на {ScannerPort} (проверка: {CheckScanner})
Камера: {CameraModel} на {CameraIP} (проверка: {CheckCamera})
Принтер: {PrinterModel} на {PrinterIP} (проверка: {CheckPrinter})
Контроллер: {ControllerIP} (проверка: {CheckController})
Виртуальная клавиатура отключена: {DisableVirtualKeyboard}
";
        }
    }
}