using l2l_aggregator.Models;

namespace l2l_aggregator.Services
{
    public class SessionService
    {
        private static SessionService? _instance;
        public static SessionService Instance => _instance ??= new SessionService();
        public bool DisableVirtualKeyboard { get; set; }
        public ArmJobRecord? SelectedTask { get; set; }

        public ArmJobInfoRecord? SelectedTaskInfo { get; set; }
        public ArmJobSsccRecord? SelectedTaskSscc { get; set; }
        public ArmJobSgtinRecord? SelectedTaskSgtin { get; set; }
    }
}
