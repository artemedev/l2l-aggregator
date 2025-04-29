using Avalonia.Markup.Xaml;

namespace l2l_aggregator.Controls.Keyboard.Layout;

public partial class VirtualKeyboardLayoutRU : KeyboardLayout
{
    public VirtualKeyboardLayoutRU()
    {
        LayoutName = "ru-RU";
        InitializeComponent();
    }
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}