﻿using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using l2l_aggregator.ViewModels;
using System.Collections.ObjectModel;

namespace l2l_aggregator.ViewModels.VisualElements
{
    public partial class DmCellViewModel : ObservableObject
    {
        private readonly AggregationViewModel _parent;

        // Конструктор
        public DmCellViewModel(AggregationViewModel parent)
        {
            _parent = parent;
        }

        // Координаты и размеры DM-квадрата (для основного экрана)
        [ObservableProperty] private double x;
        [ObservableProperty] private double y;
        [ObservableProperty] private double sizeHeight;
        [ObservableProperty] private double sizeWidth;
        [ObservableProperty] private double angle;

        // Можно ли сделать валидацию, цвет рамки и т.д.
        [ObservableProperty] private bool isValid;

        public IBrush BorderColor => IsValid ? Brushes.Green : Brushes.Red;
        partial void OnIsValidChanged(bool value) => OnPropertyChanged(nameof(BorderColor));



        [ObservableProperty]
        public DmSquareViewModel dmCell;

        // **Список OCR-квадратов**, связанных с этим DM-квадратом:
        public ObservableCollection<SquareCellViewModel> OcrCells { get; set; }
            = new ObservableCollection<SquareCellViewModel>();
        public ObservableCollection<SquareCellViewModel> OcrCellsInPopUp { get; set; }
            = new ObservableCollection<SquareCellViewModel>();
        // Команда, вызываемая при нажатии на DM-квадрат
        [RelayCommand]
        public void DmSquareClicked()
        {
            // Устанавливаем в родительской VM «текущую выбранную DMCell»
            _parent.SelectedDmCell = this;
            _parent.OnCellClicked(this);
            // И открываем Popup
            _parent.IsPopupOpen = true;
        }
    }
}
