using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DM_wraper_NS;
using l2l_aggregator.Services.DmProcessing;
using SixLabors.ImageSharp;
using System.IO;
using System.Threading.Tasks;

namespace l2l_aggregator.ViewModels
{
    public partial class CameraSettingsViewModel : ViewModelBase
    {
        public string Title { get; set; } = "Настройка нулевой точки";

        private readonly DmScanService _dmScanService;
        private readonly HistoryRouter<ViewModelBase> _router;

        [ObservableProperty] private Bitmap scannedImage;
        public IRelayCommand<SizeChangedEventArgs> ImageSizeChangedCommand { get; }

        [ObservableProperty] private double imageWidth;
        [ObservableProperty] private double imageHeight;

        static result_data dmrData;
        public CameraSettingsViewModel(HistoryRouter<ViewModelBase> router, DmScanService dmScanService)
        {
            _router = router;
            _dmScanService = dmScanService;
            ImageSizeChangedCommand = new RelayCommand<SizeChangedEventArgs>(OnImageSizeChanged);
        }
        [RelayCommand]
        public async Task Scan()
        {
            _dmScanService.getScan();
            dmrData = await _dmScanService.WaitForResultAsync();
            using (var ms = new MemoryStream())
            {
                dmrData.processedImage.SaveAsBmp(ms);
                ms.Seek(0, SeekOrigin.Begin);
                ScannedImage = new Bitmap(ms);
            }
        }
        private void OnImageSizeChanged(SizeChangedEventArgs e)
        {
            imageWidth = e.NewSize.Width;
            imageHeight = e.NewSize.Height;
        }
        [RelayCommand]
        public void GoBack()
        {
            // Переход на страницу назад
            _router.GoTo<SettingsViewModel>();
        }
    }
}
