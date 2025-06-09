using Avalonia.Controls;
using Avalonia.Notification;
using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l2l_aggregator.Models;
using l2l_aggregator.Services;
using l2l_aggregator.Services.ControllerService;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Notification.Interface;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using static l2l_aggregator.Services.Notification.NotificationService;

namespace l2l_aggregator.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        [ObservableProperty]
        private ViewModelBase _content = default!;

        // Новое булевое свойство для управления видимостью
        [ObservableProperty]
        private bool _isNotLoginPage;

        // Новое свойство для хранения данных пользователя
        [ObservableProperty]
        private UserAuthResponse? _user;

        [ObservableProperty]
        private bool _disableVirtualKeyboard;

        private readonly DatabaseService _databaseService;
        private readonly HistoryRouter<ViewModelBase> _router;
        private readonly INotificationService _notificationService;
        private readonly SessionService _sessionService;
        private readonly ConfigurationLoaderService _configLoader;
        public INotificationMessageManager Manager => _notificationService.Manager;

        //-------Notification--------
        [ObservableProperty]
        private ObservableCollection<NotificationItem> _notifications = new();

        public IRelayCommand ToggleNotificationsFlyoutCommand { get; }

        private Flyout? _notificationsFlyout;
        public IRelayCommand ClearNotificationsCommand { get; }


        private readonly PcPlcConnectionService _plcConnection;

        public MainWindowViewModel(HistoryRouter<ViewModelBase> router, DatabaseService databaseService, INotificationService notificationService, SessionService sessionService, ConfigurationLoaderService configLoader, PcPlcConnectionService plcConnection)
        {
            _router = router;
            _databaseService = databaseService;
            _configLoader = configLoader;
            _notificationService = notificationService;
            _sessionService = sessionService;
            _plcConnection = plcConnection;

            router.CurrentViewModelChanged += async viewModel =>
            {
                Content = viewModel;
                //если страница 
                IsNotLoginPage = !(viewModel is AuthViewModel || viewModel is InitializationViewModel);
                User = sessionService.User;
                _disableVirtualKeyboard = _sessionService.DisableVirtualKeyboard;
                //if (IsNotLoginPage)
                //{
                //    await LoadUserData(Content); // Загружаем данные пользователя при входе
                //}
            };
            
            InitializeAsync();
            
            //_ = InitializeSessionFromDatabaseAsync();
            //-------Notification--------
            Notifications = _notificationService.Notifications;
            ClearNotificationsCommand = new RelayCommand(ClearNotifications);
        }
        //private async Task InitializeSessionFromDatabaseAsync()
        //{
        //    var (camera, disableVK) = await _configLoader.LoadSettingsToSessionAsync();
        //    DisableVirtualKeyboard = disableVK;
        //}
        private async void InitializeAsync()
        {
            await _sessionService.InitializeAsync(_databaseService);


            if (!string.IsNullOrEmpty(_sessionService.ServerUri))
            {
                _router.GoTo<AuthViewModel>();
            }
            else
            {
                _router.GoTo<InitializationViewModel>();
            }
        }
        //private async Task LoadUserData(ViewModelBase contentViewModel)
        //{

        //    var users = await _databaseService.UserAuth.GetUserAuthAsync();

        //    if (users == null || users.Count == 0)
        //        return;

        //    UserAuthResponse? response;

        //    var isSettingsPage = contentViewModel is SettingsViewModel || contentViewModel is CameraSettingsViewModel;
        //    response = isSettingsPage ? users.FirstOrDefault() : users.Skip(1).FirstOrDefault();

        //    if (response != null)
        //    {
        //        _sessionService.User = response; // Сохраняем ссылку глобально
        //        User = response;                 // Локально для ViewModel
        //    }
        //}
        public void ButtonExit()
        {
            _ = _plcConnection.StopAsync();
            Notifications.Clear();
            _sessionService.User = null;
            User = null;
            _router.GoTo<AuthViewModel>();
        }

        public void SetFlyout(Flyout flyout)
        {
            _notificationsFlyout = flyout;
        }
        private void ClearNotifications()
        {
            Notifications.Clear();
        }
    }
}
