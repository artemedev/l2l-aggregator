using Avalonia.Controls;
using Avalonia.Input;
using l2l_aggregator.Helpers;

namespace l2l_aggregator.Views;

public partial class AuthView : UserControl
{
    public AuthView()
    {
        InitializeComponent();
    }
    private void OnTextBoxGotFocus(object? sender, GotFocusEventArgs e)
    {
        // Âûחûגאול םאר ץוכןונ
        SystemKeyboardHelper.ShowSystemKeyboard();
    }
}