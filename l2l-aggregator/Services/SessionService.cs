using l2l_aggregator.Models;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Database.Repositories.Interfaces;
using System.Threading.Tasks;

namespace l2l_aggregator.Services
{
    public class SessionService
    {
        private static SessionService? _instance;
        public static SessionService Instance => _instance ??= new SessionService();
        public bool DisableVirtualKeyboard { get; set; }
        public string? ScannerPort { get; set; }
        public string? CameraIP { get; set; }
        public string? CameraModel { get; set; }
        public string? PrinterIP { get; set; }
        public string? PrinterModel { get; set; }
        public string? ControllerIP { get; set; }
        public bool CheckCamera { get; set; }
        public bool CheckPrinter { get; set; }
        public bool CheckController { get; set; }
        public bool CheckScanner { get; set; }
        public string? ScannerModel { get; set; }
        public ArmJobRecord? SelectedTask { get; set; }

        public ArmJobInfoRecord? SelectedTaskInfo { get; set; }
        public ArmJobSsccRecord? SelectedTaskSscc { get; set; }
        public ArmJobSgtinRecord? SelectedTaskSgtin { get; set; }
        public async Task InitializeCheckFlagsAsync(DatabaseService db)
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
        public async Task InitializeAsync(DatabaseService db)
        {
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
        }
    }
}
