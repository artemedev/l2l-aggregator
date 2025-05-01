using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using l2l_aggregator.Controls.Keyboard.Layout;
using Material.Icons;
using Material.Icons.Avalonia;
using System;
using System.Linq;
using System.Windows.Input;

namespace l2l_aggregator.Controls.Keyboard;

public partial class VirtualKey : TemplatedControl
{
    public static readonly StyledProperty<ICommand> ButtonCommandProperty =
        AvaloniaProperty.Register<VirtualKey, ICommand>(nameof(ButtonCommand));
    public ICommand ButtonCommand
    {
        get => GetValue(ButtonCommandProperty);
        set => SetValue(ButtonCommandProperty, value);
    }

    public static readonly StyledProperty<string> NormalKeyProperty =
        AvaloniaProperty.Register<VirtualKey, string>(nameof(NormalKey));
    public string NormalKey
    {
        get => GetValue(NormalKeyProperty);
        set => SetValue(NormalKeyProperty, value);
    }

    public static readonly StyledProperty<string> ShiftKeyProperty =
        AvaloniaProperty.Register<VirtualKey, string>(nameof(ShiftKey));
    public string ShiftKey
    {
        get => GetValue(ShiftKeyProperty);
        set => SetValue(ShiftKeyProperty, value);
    }

    public static readonly StyledProperty<string> AltCtrlKeyProperty =
        AvaloniaProperty.Register<VirtualKey, string>(nameof(AltCtrlKey));
    public string AltCtrlKey
    {
        get => GetValue(AltCtrlKeyProperty);
        set => SetValue(AltCtrlKeyProperty, value);
    }

    public static readonly StyledProperty<object> CaptionProperty =
        AvaloniaProperty.Register<VirtualKey, object>(nameof(Caption));
    public object Caption
    {
        get => GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public static readonly StyledProperty<Key> SpecialKeyProperty =
        AvaloniaProperty.Register<VirtualKey, Key>(nameof(SpecialKey));
    public Key SpecialKey
    {
        get => GetValue(SpecialKeyProperty);
        set => SetValue(SpecialKeyProperty, value);
    }

    public static readonly StyledProperty<MaterialIconKind> SpecialIconProperty =
        AvaloniaProperty.Register<VirtualKey, MaterialIconKind>(nameof(SpecialIcon));
    public MaterialIconKind SpecialIcon
    {
        get => GetValue(SpecialIconProperty);
        set => SetValue(SpecialIconProperty, value);
    }

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<VirtualKey, double>(nameof(FontSize), 12);
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public VirtualKeyboardLayoutRU VirtualKeyboardLayout { get; set; }

    private ToggleButton _toggleButton;

    public VirtualKey()
    {
        DataContext = this;

        this.PropertyChanged += (_, e) =>
        {
            if (e.Property == BoundsProperty)
            {
                var width = Bounds.Width;
                FontSize = width / 6;
            }
        };


        Initialized += (sender, args) =>
        {
            VirtualKeyboard keyboard = null;

            if (!Design.IsDesignMode)
            {
                keyboard = this.GetVisualAncestors().OfType<VirtualKeyboard>().FirstOrDefault();

                keyboard.OnKeyboardStateChanged += (s, state) =>
                {
                    if (!string.IsNullOrEmpty(NormalKey))
                    {
                        Caption = state switch
                        {
                            VirtualKeyboardState.Default => NormalKey,
                            VirtualKeyboardState.Shift => ShiftKey,
                            VirtualKeyboardState.Capslock => ShiftKey,
                            VirtualKeyboardState.AltCtrl => AltCtrlKey,
                            _ => Caption
                        };
                    }
                };

                ButtonCommand = new RelayCommand(() =>
                {
                    if (SpecialKey != Key.None)
                        keyboard?.ProcessKey(SpecialKey);
                    else if (Caption is string s && !string.IsNullOrEmpty(s))
                        keyboard?.ProcessText(s);
                });
            }

            if (SpecialKey == Key.LeftShift || SpecialKey == Key.RightShift || SpecialKey == Key.CapsLock || SpecialKey == Key.RightAlt)
            {
                _toggleButton = new ToggleButton
                {
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Colors.White),
                    Background = new SolidColorBrush(Colors.White),
                    Foreground = new SolidColorBrush(Colors.White),
                    CornerRadius = new CornerRadius(5),
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center,

                    [!ToggleButton.WidthProperty] = new Binding("Width"),
                    [!ToggleButton.HeightProperty] = new Binding("Height"),
                    [!ToggleButton.ContentProperty] = new Binding("Caption"),
                    [!ToggleButton.CommandProperty] = new Binding("ButtonCommand"),
                    [!ToggleButton.FontSizeProperty] = new Binding("FontSize")
                };

                Template = new FuncControlTemplate((control, scope) => _toggleButton);

                keyboard.OnKeyboardStateChanged += (s, state) =>
                {
                    _toggleButton.IsChecked = state switch
                    {
                        VirtualKeyboardState.Default => false,
                        VirtualKeyboardState.Shift => SpecialKey == Key.LeftShift || SpecialKey == Key.RightShift,
                        VirtualKeyboardState.Capslock => SpecialKey == Key.CapsLock,
                        VirtualKeyboardState.AltCtrl => SpecialKey == Key.RightAlt,
                        _ => false
                    };
                };
            }
            else
            {
                Template = new FuncControlTemplate((control, scope) =>
                {
                    var presenter = new ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [!ContentPresenter.ContentProperty] = new Binding("Caption"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    return new Button
                    {
                        BorderThickness = new Thickness(1),
                        BorderBrush = Brushes.Gray,
                        Background = Brushes.White, // временно для видимости
                        Foreground = Brushes.Black,
                        CornerRadius = new CornerRadius(10),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Content = presenter,

                        [!Button.WidthProperty] = new Binding("Width"),
                        [!Button.HeightProperty] = new Binding("Height"),
                        [!Button.ContentProperty] = new Binding("Caption"),
                        [!Button.CommandProperty] = new Binding("ButtonCommand"),
                        [!Button.FontSizeProperty] = new Binding("FontSize")
                    };
                });
            }

            if (string.IsNullOrEmpty(NormalKey) && SpecialKey != Key.None)
            {
                Caption = new MaterialIcon
                {
                    Kind = SpecialIcon,
                    Width = 24,
                    Height = 24,
                    BorderBrush = Brushes.Gray,
                    Background = Brushes.White,
                    Foreground = Brushes.Black, // Текст и иконки будут черными
                };
            }
            else
            {
                Caption = NormalKey;
            }
        };
    }
}
