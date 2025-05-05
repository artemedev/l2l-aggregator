using l2l_aggregator.Models;

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

        public ArmJobRecord? SelectedTask { get; set; }

        public ArmJobInfoRecord? SelectedTaskInfo { get; set; }
        public ArmJobSsccRecord? SelectedTaskSscc { get; set; }
        public ArmJobSgtinRecord? SelectedTaskSgtin { get; set; }
    }
}
