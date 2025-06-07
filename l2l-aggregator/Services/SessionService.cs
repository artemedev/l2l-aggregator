//using FastReport.Utils;
//using l2l_aggregator.Models;
//using l2l_aggregator.Models.AggregationModels;
//using l2l_aggregator.Services.Database;
//using l2l_aggregator.Services.Database.Repositories.Interfaces;
//using System;
//using System.Linq;
//using System.Text.Json;
//using System.Threading.Tasks;

//namespace l2l_aggregator.Services
//{
//    public class SessionService
//    {
//        private static SessionService? _instance;
//        public static SessionService Instance => _instance ??= new SessionService();

//        // Настройки устройств
//        public bool DisableVirtualKeyboard { get; set; }
//        public string? ScannerPort { get; set; }
//        public string? CameraIP { get; set; }
//        public string? CameraModel { get; set; }
//        public string? PrinterIP { get; set; }
//        public string? PrinterModel { get; set; }
//        public string? ControllerIP { get; set; }

//        public bool CheckCamera { get; set; }
//        public bool CheckPrinter { get; set; }
//        public bool CheckController { get; set; }
//        public bool CheckScanner { get; set; }

//        public string? ScannerModel { get; set; }


//        private string? _serverUri;
//        public string? ServerUri
//        {
//            get => _serverUri;
//            set
//            {
//                _serverUri = value;
//                SaveSettingToDb("ScannerCOMPort", value);
//            }
//        }
//        //public string? ServerUri { get=> _; set; }
//        // Пользователь и задание
//        public UserAuthResponse? User { get; set; }
//        public ArmJobRecord? SelectedTask { get; set; }
//        public ArmJobInfoRecord? SelectedTaskInfo { get; set; }
//        public ArmJobSsccRecord? SelectedTaskSscc { get; set; }
//        public ArmJobSgtinRecord? SelectedTaskSgtin { get; set; }

//        public AggregationState? AggregationState { get; set; }

//        //// Восстановление состояния агрегации
//        //public string? LoadedTemplateJson { get; set; }
//        //public string? LoadedProgressJson { get; set; }
//        //public bool HasUnfinishedAggregation => SelectedTaskInfo != null &&
//        //            !string.IsNullOrWhiteSpace(LoadedTemplateJson) &&
//        //            !string.IsNullOrWhiteSpace(LoadedProgressJson);

//        // Инициализация настроек
//        public async Task InitializeAsync(DatabaseService db)
//        {
//            await InitializeCheckFlagsAsync(db);

//            var config = db.Config;

//            PrinterIP = await config.GetConfigValueAsync("PrinterIP");
//            PrinterModel = await config.GetConfigValueAsync("PrinterModel");
//            ControllerIP = await config.GetConfigValueAsync("ControllerIP");
//            CameraIP = await config.GetConfigValueAsync("CameraIP");
//            CameraModel = await config.GetConfigValueAsync("CameraModel");
//            ScannerPort = await config.GetConfigValueAsync("ScannerCOMPort");
//            ScannerModel = await config.GetConfigValueAsync("ScannerModel");
//            DisableVirtualKeyboard = bool.TryParse(await config.GetConfigValueAsync("DisableVirtualKeyboard"), out var vkParsed) && vkParsed;
//            ServerUri = await config.GetConfigValueAsync("ServerUri");

//            // Добавим получение пользователя
//            var users = await db.UserAuth.GetUserAuthAsync();
//            if (users != null && users.Count > 1)
//            {
//                User = users.Skip(1).FirstOrDefault(); 
//            }
//        }

//        private async Task InitializeCheckFlagsAsync(DatabaseService db)
//        {
//            var config = db.Config;

//            CheckCamera = await LoadOrInitBool(config, "CheckCamera", true);
//            CheckPrinter = await LoadOrInitBool(config, "CheckPrinter", true);
//            CheckController = await LoadOrInitBool(config, "CheckController", true);
//            CheckScanner = await LoadOrInitBool(config, "CheckScanner", true);
//        }

//        private async Task<bool> LoadOrInitBool(IConfigRepository config, string key, bool defaultValue)
//        {
//            var value = await config.GetConfigValueAsync(key);
//            if (string.IsNullOrWhiteSpace(value))
//            {
//                await config.SetConfigValueAsync(key, defaultValue.ToString());
//                return defaultValue;
//            }

//            return bool.TryParse(value, out var parsed) && parsed;
//        }

//        // Загрузка сохранённого состояния агрегации
//        //public async Task LoadAggregationStateAsync(DatabaseService db)
//        //{
//        //    if (User == null || string.IsNullOrWhiteSpace(User.USER_NAME))
//        //        return;
//        //    try
//        //    {
//        //        var state = await db.AggregationState.LoadStateAsync(User.USER_NAME);
//        //        if (state != null)
//        //        {
//        //            SelectedTaskInfo = JsonSerializer.Deserialize<ArmJobInfoRecord>(state.TaskInfoJson);
//        //            LoadedTemplateJson = state.TemplateJson;
//        //            LoadedProgressJson = state.ProgressJson;
//        //        }
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        throw new Exception("Ошибка при загрузке состояния агрегации", ex);
//        //    }
//        //}
//    }
//}
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

        private string? _scannerModel;
        public string? ScannerModel
        {
            get => _scannerModel;
            set => SetAndSave(ref _scannerModel, value, "ScannerModel");
        }

        private string? _serverUri;
        public string? ServerUri
        {
            get => _serverUri;
            set => SetAndSave(ref _serverUri, value, "ServerUri");
        }

        public UserAuthResponse? User { get; set; }
        public ArmJobRecord? SelectedTask { get; set; }
        public ArmJobInfoRecord? SelectedTaskInfo { get; set; }
        public ArmJobSsccRecord? SelectedTaskSscc { get; set; }
        public ArmJobSgtinRecord? SelectedTaskSgtin { get; set; }

        public AggregationState? AggregationState { get; set; }

        public bool HasUnfinishedAggregation => SelectedTaskInfo != null &&
            AggregationState != null &&
            !string.IsNullOrWhiteSpace(AggregationState.TemplateJson) &&
            !string.IsNullOrWhiteSpace(AggregationState.ProgressJson);
        //public string? LoadedTemplateJson { get; set; }
        //public string? LoadedProgressJson { get; set; }
        //public bool HasUnfinishedAggregation => SelectedTaskInfo != null &&
        //            !string.IsNullOrWhiteSpace(LoadedTemplateJson) &&
        //            !string.IsNullOrWhiteSpace(LoadedProgressJson);

        public async Task InitializeAsync(DatabaseService db)
        {
            SetConfigRepository(db.Config);
            await InitializeCheckFlagsAsync(db);

            var config = db.Config;

            PrinterIP = await config.GetConfigValueAsync("PrinterIP");
            PrinterModel = await config.GetConfigValueAsync("PrinterModel");
            ControllerIP = await config.GetConfigValueAsync("ControllerIP");
            CameraIP = await config.GetConfigValueAsync("CameraIP");
            CameraModel = await config.GetConfigValueAsync("CameraModel");
            ScannerPort = await config.GetConfigValueAsync("ScannerCOMPort");
            ScannerModel = await config.GetConfigValueAsync("ScannerModel");
            DisableVirtualKeyboard = bool.TryParse(await config.GetConfigValueAsync("DisableVirtualKeyboard"), out var vkParsed) && vkParsed;
            ServerUri = await config.GetConfigValueAsync("ServerUri");

            var users = await db.UserAuth.GetUserAuthAsync();
            if (users != null && users.Count > 0)
            {
                User = users.FirstOrDefault();
            }
        }

        private async Task InitializeCheckFlagsAsync(DatabaseService db)
        {
            var config = db.Config;

            CheckCamera = await LoadOrInitBool(config, "CheckCamera", true);
            CheckPrinter = await LoadOrInitBool(config, "CheckPrinter", true);
            CheckController = await LoadOrInitBool(config, "CheckController", true);
            CheckScanner = await LoadOrInitBool(config, "CheckScanner", true);
        }

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
        //public async Task LoadAggregationStateAsync(DatabaseService db)
        //{
        //    if (User == null || string.IsNullOrWhiteSpace(User.USER_NAME))
        //        return;
        //    try
        //    {
        //        var state = await db.AggregationState.LoadStateAsync(User.USER_NAME);
        //        if (state != null)
        //        {
        //            SelectedTaskInfo = JsonSerializer.Deserialize<ArmJobInfoRecord>(state.TaskInfoJson);
        //            LoadedTemplateJson = state.TemplateJson;
        //            LoadedProgressJson = state.ProgressJson;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception("Ошибка при загрузке состояния агрегации", ex);
        //    }
        //}
    }
}
