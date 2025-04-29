using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;

namespace l2l_aggregator.Controls.Keyboard.Layout;
public partial class VirtualKeyboardLayoutUS : KeyboardLayout
{
    public VirtualKeyboardLayoutUS()
    {
        LayoutName = "en-US";
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}