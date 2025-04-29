using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using System.Linq;

namespace l2l_aggregator.Views;

public partial class InitializationView : UserControl
{
    private KeyboardWindow? _keyboard;
    public InitializationView()
    {
        InitializeComponent();
        //this.AttachedToVisualTree += OnAttachedToVisualTree;
    }
    //private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    //{
    //    var textBoxes = this.GetVisualDescendants().OfType<TextBox>();

    //    foreach (var textBox in textBoxes)
    //    {
    //        textBox.GotFocus += OnTextBoxGotFocus;
    //        textBox.LostFocus += OnTextBoxLostFocus;
    //    }
    //}

    //private void OnTextBoxGotFocus(object? sender, RoutedEventArgs e)
    //{
    //    if (sender is TextBox textBox)
    //    {
    //        if (_keyboard == null || !_keyboard.IsVisible)
    //        {
    //            _keyboard = new KeyboardWindow();
    //            _keyboard.SetTarget(textBox);
    //            _keyboard.Show();
    //        }
    //        else
    //        {
    //            _keyboard.SetTarget(textBox);
    //        }
    //    }
    //}

    //private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    //{
    //    // ≈сли потер€л фокус вообще (не в другое поле) Ч можно закрыть
    //    if (_keyboard != null)
    //    {
    //        // ћожно закрывать сразу или через проверку
    //        // _keyboard.Close();
    //    }
    //}
}