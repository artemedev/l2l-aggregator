using Avalonia.Notification;
using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using l2l_aggregator.Models;
using l2l_aggregator.Services;
using l2l_aggregator.Services.Api;
using l2l_aggregator.Services.Database;
using l2l_aggregator.Services.Database.Interfaces;
using l2l_aggregator.Services.Notification.Interface;
using Refit;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

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

        public MainWindowViewModel(HistoryRouter<ViewModelBase> router, DatabaseService databaseService, INotificationService notificationService, SessionService sessionService, ConfigurationLoaderService configLoader)
        {
            _router = router;
            _databaseService = databaseService;
            _configLoader = configLoader;
            _notificationService = notificationService;
            _sessionService = sessionService;

            router.CurrentViewModelChanged += async viewModel =>
            {
                Content = viewModel;
                IsNotLoginPage = !(viewModel is AuthViewModel || viewModel is InitializationViewModel);
                if (IsNotLoginPage)
                {
                    await LoadUserData(Content); // Загружаем данные пользователя при входе
                }
            };
            InitializeAsync();

            _ = InitializeSessionFromDatabaseAsync();
        }
        //private async Task InitializeSessionFromDatabaseAsync()
        //{
        //    await _sessionService.InitializeCheckFlagsAsync(_databaseService);
        //    var config = _databaseService.Config;

        //    _sessionService.PrinterIP = await config.GetConfigValueAsync("PrinterIP");
        //    _sessionService.PrinterModel = await config.GetConfigValueAsync("PrinterModel");
        //    _sessionService.ControllerIP = await config.GetConfigValueAsync("ControllerIP");
        //    _sessionService.CameraIP = await config.GetConfigValueAsync("CameraIP");
        //    _sessionService.CameraModel = await config.GetConfigValueAsync("CameraModel");
        //    _sessionService.ScannerPort = await config.GetConfigValueAsync("ScannerCOMPort");
        //    _sessionService.ScannerModel = await config.GetConfigValueAsync("ScannerModel");

        //    _sessionService.DisableVirtualKeyboard = bool.TryParse(await config.GetConfigValueAsync("DisableVirtualKeyboard"), out var parsed) && parsed;

        //    _sessionService.CheckCamera = await config.GetConfigValueAsync("CheckCamera") == "True";
        //    _sessionService.CheckPrinter = await config.GetConfigValueAsync("CheckPrinter") == "True";
        //    _sessionService.CheckController = await config.GetConfigValueAsync("CheckController") == "True";
        //    _sessionService.CheckScanner = await config.GetConfigValueAsync("CheckScanner") == "True";
        //}
        private async Task InitializeSessionFromDatabaseAsync()
        {
            var (camera, disableVK) = await _configLoader.LoadSettingsToSessionAsync();
            DisableVirtualKeyboard = disableVK;
        }
        private async void InitializeAsync()
        {
            var serverUri = await _databaseService.Config.GetConfigValueAsync("ServerUri");

            // Redirect to appropriate startup page
            if (!string.IsNullOrEmpty(serverUri))
            {
                _router.GoTo<AuthViewModel>();
            }
            else
            {
                _router.GoTo<InitializationViewModel>();
            }
        }
        private async Task LoadUserData(ViewModelBase contentViewModel)
        {

            var users = await _databaseService.UserAuth.GetUserAuthAsync();

            if (users == null || users.Count == 0)
                return;

            UserAuthResponse? response;

            var isSettingsPage = contentViewModel is SettingsViewModel || contentViewModel is CameraSettingsViewModel;
            response = isSettingsPage ? users.FirstOrDefault() : users.Skip(1).FirstOrDefault();

            if (response != null)
            {
                User = new UserAuthResponse
                {
                    USER_NAME = response.USER_NAME,
                };
            }
        }
        public void ButtonExit()
        {
            _router.GoTo<AuthViewModel>();
        }
    }
}
