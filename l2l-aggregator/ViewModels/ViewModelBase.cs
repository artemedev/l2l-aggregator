using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace l2l_aggregator.ViewModels
{
    public partial class ViewModelBase : ObservableObject
    {
        [ObservableProperty]
        private double windowWidth;

        [ObservableProperty]
        private double windowHeight;

        public double DynamicFontSize => Math.Clamp(WindowWidth * 0.02, 12, 48); 
        //Math.Max(12, WindowWidth / 50);
        public Thickness DynamicPadding => new Thickness(Math.Clamp(WindowWidth * 0.025, 16, 48));
    }
}
