using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace l2l_aggregator.ViewModels.VisualElements
{
    public partial class AggregatedItem : ObservableObject
    {
        [ObservableProperty] private string name;
        [ObservableProperty] private bool isCompleted;
        [ObservableProperty] private string type;
        [ObservableProperty] private int index;
        [ObservableProperty] private int total;
        public string StatusText => $"{Type} {Index} из {Total} — {(IsCompleted ? "Агрегировано ✅" : "Ожидает ⏳")}";
        partial void OnIsCompletedChanged(bool value)
        {
            OnPropertyChanged(nameof(StatusText));
        }
    }
}
