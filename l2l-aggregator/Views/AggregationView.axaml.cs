using Avalonia.Controls;
using Avalonia.Input;
using l2l_aggregator.ViewModels;
using l2l_aggregator.ViewModels.VisualElements;

namespace l2l_aggregator.Views;

public partial class AggregationView : UserControl
{
    public AggregationView()
    {
        InitializeComponent();
        // Подписка на изменение Bounds у ScannedImageControl:
    }
    //private void ScannedImageControl_SizeChanged(object? sender, SizeChangedEventArgs e)
    //{
    //    if (DataContext is AggregationViewModel vm)
    //    {
    //        vm.ImageWidth = e.NewSize.Width;
    //        vm.ImageHeight = e.NewSize.Height;
    //    }
    //}
    private void HandleCellPointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (sender is Border border &&
            border.DataContext is DmCellViewModel vm &&
            DataContext is AggregationViewModel avm)
        {
            avm.OnCellClicked(vm);
        }
    }
    private void OnImageSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is AggregationViewModel vm)
        {
            vm.ImageSize = e.NewSize;
        }
    }
    private void OnImageSizeCellChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is AggregationViewModel vm)
        {
            vm.ImageSizeCell = e.NewSize;
        }
    }
    private void OnImageGridSizeCellChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is AggregationViewModel vm)
        {
            vm.ImageSizeGridCell = e.NewSize;
        }
    }
    private void Popup_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is AggregationViewModel vm)
        {
            vm.IsPopupOpen = false; // Закрываем Popup через привязанное свойство
        }
        //scanImagePopup.IsOpen = false; // Закрываем popup при нажатии
    }
    // Чтобы был доступ к элементу x:Name="ScannedImageControl":
    //public Image ScannedImageControl => this.FindControl<Image>("ScannedImageControl");
    public void OnRawScanReceived(string scannedData)
    {
        // Найдём активную ViewModel
        if (DataContext is AggregationViewModel vm)
        {
            vm.HandleScannedBarcode(scannedData);
        }
    }
}