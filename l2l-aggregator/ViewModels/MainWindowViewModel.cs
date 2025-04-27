using Avalonia.Notification;
using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using l2l_aggregator.Models;
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

        private readonly DatabaseService _databaseService;
        private readonly HistoryRouter<ViewModelBase> _router;
        private readonly INotificationService _notificationService;

        public INotificationMessageManager Manager => _notificationService.Manager;

        public MainWindowViewModel(HistoryRouter<ViewModelBase> router, DatabaseService databaseService, INotificationService notificationService)
        {
            _router = router;
            _databaseService = databaseService;
            _notificationService = notificationService;

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
        }
        private async void InitializeAsync()
        {
            var serverUri = await _databaseService.Config.GetConfigValueAsync("ServerUri");

            if (!string.IsNullOrEmpty(serverUri))
            {
                var client = RestService.For<IAuthApi>(new HttpClient()
                {
                    BaseAddress = new Uri(serverUri)
                });

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
