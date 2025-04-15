using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using l2l_aggregator.Models;
using l2l_aggregator.Services.Database;

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
        private UserAuthResponse _user;

        private readonly DatabaseRepository _databaseRepository;

        private readonly HistoryRouter<ViewModelBase> _router;

        public MainWindowViewModel(HistoryRouter<ViewModelBase> router, DatabaseRepository databaseRepository)
        {
            _router = router;
            _databaseRepository = databaseRepository;

            router.CurrentViewModelChanged += viewModel =>
            {

                Content = viewModel;
                IsNotLoginPage = !(viewModel is AuthViewModel || viewModel is InitializationViewModel);
                if (IsNotLoginPage)
                {
                    LoadUserData(Content); // Загружаем данные пользователя при входе
                }
            };
        }
        private void LoadUserData(ViewModelBase ContentViewModel)
        {

            var response = _databaseRepository.GetUserAuth()[0];
            var IsSettingsPage = (ContentViewModel is SettingsViewModel || ContentViewModel is CameraSettingsViewModel);
            if (IsSettingsPage)
            {
                response = _databaseRepository.GetUserAuth()[0];
            }
            else
            {
                response = _databaseRepository.GetUserAuth()[1];
            }


            _user = new UserAuthResponse
            {
                USER_NAME = response.USER_NAME,
            };
            OnPropertyChanged(nameof(User));
        }
        public void ButtonExit()
        {

        }
    }
}
