using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using l2l_aggregator.Controls.Keyboard.Layout;
using Material.Icons.Avalonia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace l2l_aggregator.Controls.Keyboard;

public enum VirtualKeyboardState
{
    Default,
    Shift,
    Capslock,
    AltCtrl
}

public partial class VirtualKeyboard : UserControl
{
    private static List<Type> Layouts { get; } = new List<Type>();
    private static Func<Type> DefaultLayout { get; set; }

    public static void AddLayout<TLayout>() where TLayout : KeyboardLayout => Layouts.Add(typeof(TLayout));

    public static void SetDefaultLayout(Func<Type> getDefaultLayout) => DefaultLayout = getDefaultLayout;

    public static async Task<string?> ShowDialog(TextInputOptions options, Window? owner = null)
    {
        var keyboard = new VirtualKeyboard();


        //if (options.ContentType == TextInputContentType.)
        //{
        //    keyboard.TextBox.Text = textBox.Text;
        //    keyboard.TextBox.PasswordChar = textBox.PasswordChar;

        //    if (textBox.Tag != null && ((string)textBox.Tag).Contains("numeric"))
        //    {
        //        keyboard.targetLayout = "numpad";
        //    }
        //}



        var window = new CoporateWindow();
        window.CoporateContent = keyboard;
        window.Title = "Keyboard";

        var mw = ((IClassicDesktopStyleApplicationLifetime)App.Current.ApplicationLifetime).MainWindow;



        await window.ShowDialog(owner ?? mw);
        if (window.Tag is string s)
        {
            //if (options.Source is TextBox tb)
            //    tb.Text = s;
            return s;
        }
        return null;
    }

    public TextBox TextBox_ { get; }
    public Button AcceptButton_ { get; }
    public Button CloseButton_ { get; }
    public string targetLayout { get; set; }
    public TransitioningContentControl TransitioningContentControl_ { get; }

    public TextBox source { get; set; }


    // Поле состояния
    private VirtualKeyboardState _keyboardState = VirtualKeyboardState.Default;
    public VirtualKeyboardState KeyboardState
    {
        get => _keyboardState;
        private set
        {
            if (_keyboardState != value)
            {
                _keyboardState = value;
                OnKeyboardStateChanged?.Invoke(this, _keyboardState);
            }
        }
    }

    // Событие для оповещения об изменении состояния
    public event EventHandler<VirtualKeyboardState>? OnKeyboardStateChanged;
    // Команда закрытия
    public IRelayCommand CloseCommand { get; }

    public VirtualKeyboard()
    {
        InitializeComponent();
        TextBox_ = this.Get<TextBox>("TextBox");
        TransitioningContentControl_ = this.Get<Avalonia.Controls.TransitioningContentControl>("TransitioningContentControl");
        AcceptButton_ = this.Get<Button>("AcceptButton");
        CloseButton_ = this.Get<Button>("CloseButton");

        AcceptButton_.AddHandler(Button.ClickEvent, acceptClicked);
        CloseButton_.AddHandler(Button.ClickEvent, closeClicked);
        CloseCommand = new RelayCommand(() => Close());

        Initialized += async (sender, args) =>
        {

            if (targetLayout == null)
            {
                TransitioningContentControl_.Content = Activator.CreateInstance(DefaultLayout.Invoke());
            }
            else
            {
                var layout = Layouts.FirstOrDefault(x => x.Name.ToLower().Contains(targetLayout.ToLower()));
                if (layout != null)
                {
                    TransitioningContentControl_.Content = Activator.CreateInstance(layout);
                }
                else
                {
                    TransitioningContentControl_.Content = Activator.CreateInstance(DefaultLayout.Invoke());
                }
            }
            // Обновить визуал после смены лейаута
            Dispatcher.UIThread.Post(() =>
            {
                UpdateKeyVisuals(this.Bounds.Width);
            }, DispatcherPriority.Render);
        };

        KeyDown += (sender, args) =>
        {
            TextBox_.Focus();
            if (args.Key == Key.Escape)
            {
                TextBox_.Text = "";
            }
            else if (args.Key == Key.Enter)
            {

                source.Text = TextBox_.Text;

                Close();
            }
        };
        this.PropertyChanged += (_, e) =>
        {
            if (e.Property == BoundsProperty)
            {
                var rect = this.Bounds;
                UpdateKeyVisuals(rect.Width);
            }
        };


        TextBox_.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty || e.Property == BoundsProperty)
            {
                AdjustTextBoxFontSize();
            }
        };
        //// Замените подписку на ContentChanged в конструкторе:
        //TransitioningContentControl_.PropertyChanged += (sender, e) =>
        //{
        //    if (e.Property == ContentControl.ContentProperty) // Проверяем, что изменилось именно Content
        //    {
        //        Dispatcher.UIThread.Post(() =>
        //        {
        //            UpdateKeyVisuals(this.Bounds.Width);
        //        }, DispatcherPriority.Loaded);
        //    }
        //};
        _keyboardState = VirtualKeyboardState.Default;
    }
    //private void UpdateKeyMargins(double width)
    //{
    //    double adaptiveMargin = Math.Clamp(width * 0.003, 0.5, 10); // Пример: 1.5% от ширины

    //    var keys = this.GetVisualDescendants().OfType<VirtualKey>();
    //    foreach (var key in keys)
    //    {
    //        key.Margin = new Thickness(adaptiveMargin);
    //    }
    //}
    //private void UpdateKeyVisuals(double width)
    //{
    //    double margin = Math.Clamp(width * 0.003, 0.5, 10);
    //    double fontSize = Math.Clamp(width * 0.02, 10, 36);      // Пример: от 10 до 36
    //    double iconSize = Math.Clamp(width * 0.025, 16, 48);     // Пример: от 16 до 48
    //    double textBoxFontSize = Math.Clamp(width * 0.02, 14, 36); // Текстбокс

    //    if (TransitioningContentControl_.Content is Control layoutRoot)
    //    {
    //        var keys = layoutRoot.GetVisualDescendants().OfType<VirtualKey>();
    //        foreach (var key in keys)
    //        {
    //            key.Margin = new Thickness(margin);
    //            key.FontSize = fontSize;

    //            // Обработка MaterialIcon, если используется
    //            var icon = key.GetVisualDescendants().OfType<MaterialIcon>().FirstOrDefault();
    //            if (icon != null)
    //                icon.Width = icon.Height = iconSize;

    //            // Или обработка Image, если у вас иконки как изображения
    //            var image = key.GetVisualDescendants().OfType<Image>().FirstOrDefault();
    //            if (image != null)
    //                image.Width = image.Height = iconSize;
    //        }
    //    }
    //}
    private void UpdateKeyVisuals(double width)
    {
        double margin = Math.Clamp(width * 0.003, 0.5, 10);
        double fontSize = Math.Clamp(width * 0.02, 12, 48);       // Клавиши
        double iconSize = Math.Clamp(width * 0.025, 16, 48);      // Иконки

        if (TransitioningContentControl_.Content is Control layoutRoot)
        {
            var keys = layoutRoot.GetVisualDescendants().OfType<VirtualKey>();
            foreach (var key in keys)
            {
                key.Margin = new Thickness(margin);
                key.FontSize = fontSize;

                // MaterialIcon
                var icon = key.GetVisualDescendants().OfType<Control>()
                              .FirstOrDefault(c => c.GetType().Name == "MaterialIcon");
                if (icon != null)
                {
                    icon.Width = icon.Height = iconSize;
                }

                // Image (если вдруг иконка через PNG)
                var image = key.GetVisualDescendants().OfType<Image>().FirstOrDefault();
                if (image != null)
                {
                    image.Width = image.Height = iconSize;
                }
            }
        }

        //// Адаптивный шрифт для TextBox — по его собственной ширине
        //if (TextBox_ != null && TextBox_.Bounds.Width > 0)
        //{
        //    double textboxFontSize = Math.Clamp(TextBox_.Bounds.Width * 0.05, 12, 40);
        //    TextBox_.FontSize = textboxFontSize;
        //}
    }
    private void AdjustTextBoxFontSize()
    {
        if (TextBox_ == null || string.IsNullOrWhiteSpace(TextBox_.Text))
            return;

        double availableWidth = TextBox_.Bounds.Width - 20;
        double availableHeight = TextBox_.Bounds.Height - 10;

        string text = TextBox_.Text;
        double fontSize = 80;

        // Грубый коэффициент ширины: 0.6 — эмпирически подходит для большинства шрифтов
        while (fontSize >= 8)
        {
            double estimatedWidth = text.Length * fontSize * 0.6;
            double estimatedHeight = fontSize * 1.5;

            if (estimatedWidth <= availableWidth && estimatedHeight <= availableHeight)
                break;

            fontSize -= 1;
        }

        TextBox_.FontSize = fontSize;
    }






    private void closeClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    public void Close()
    {


        TextBox_.Text = "";
        IsVisible = false;
        ((Control)this.Parent).IsVisible = false;
    }

    public void ShowKeyboard(TextBox source)
    {
        Dispatcher.UIThread.Post(() =>
        {
            TextBox_.Focus();
        });
        TextBox_.Focus();
        this.source = source;

        this.TextBox_.PasswordChar = source.PasswordChar;
        this.TextBox_.Text = source.Text == null ? "" : source.Text;
        //this.TextBox_.CaretIndex = this.TextBox_.Text.Length;
        this.IsVisible = true;
        ((Control)this.Parent).IsVisible = true;
        TransitioningContentControl_.Content = Activator.CreateInstance(DefaultLayout.Invoke());
        // Обновить визуал после смены лейаута
        Dispatcher.UIThread.Post(() =>
        {
            UpdateKeyVisuals(this.Bounds.Width);
        }, DispatcherPriority.Render);
    }
    public void ShowKeyboard(TextBox source, Type layout)
    {
        Dispatcher.UIThread.Post(() =>
        {
            TextBox_.Focus();
        });

        TextBox_.Focus();
        this.source = source;
        this.TextBox_.PasswordChar = source.PasswordChar;
        this.TextBox_.Text = source.Text == null ? "" : source.Text;

        TextBox_.CaretIndex = TextBox_.Text.Length;
        this.IsVisible = true;
        ((Control)this.Parent).IsVisible = true;
        TransitioningContentControl_.Content = Activator.CreateInstance(layout);
        // Обновить визуал после смены лейаута
        Dispatcher.UIThread.Post(() =>
        {
            UpdateKeyVisuals(this.Bounds.Width);
        }, DispatcherPriority.Render);
    }


    private void acceptClicked(object? sender, RoutedEventArgs e)
    {

        source.Text = TextBox_.Text;

        Close();

    }

    public void ProcessText(string text)
    {
        TextBox_.Focus();
        // place the pressed text at the createIndex into the textbox


        if (TextBox_.CaretIndex <= TextBox_.Text.Length)
        {
            TextBox_.Text = TextBox_.Text.Insert(TextBox_.CaretIndex, text);
            TextBox_.CaretIndex = Math.Clamp(TextBox_.CaretIndex + 1, 0, TextBox_.Text.Length);
        }

        TextBox_.Focus();

        //InputManager.Instance.ProcessInput(new RawTextInputEventArgs(KeyboardDevice.Instance, (ulong)DateTime.Now.Ticks, (Window)TextBox.GetVisualRoot(), text));
        if (_keyboardState == VirtualKeyboardState.Shift)
        {
            KeyboardState = VirtualKeyboardState.Default; // это вызовет событие
        }
    }

    public void ProcessKey(Key key)
    {
        if (key == Key.LeftShift || key == Key.RightShift)
        {
            if (_keyboardState == VirtualKeyboardState.Shift)
            {
                KeyboardState = VirtualKeyboardState.Default;
            }
            else
            {
                KeyboardState = VirtualKeyboardState.Shift;
            }
        }
        else if (key == Key.RightAlt)
        {
            if (_keyboardState == VirtualKeyboardState.AltCtrl)
            {
                KeyboardState = VirtualKeyboardState.Default;
            }
            else
            {
                KeyboardState = VirtualKeyboardState.AltCtrl;
            }
        }
        else if (key == Key.CapsLock)
        {
            if (_keyboardState == VirtualKeyboardState.Capslock)
            {
                KeyboardState = VirtualKeyboardState.Default;
            }
            else
            {
                KeyboardState = VirtualKeyboardState.Capslock;
            }
        }
        else
        {
            if (key == Key.Clear)
            {
                TextBox_.Text = "";
                TextBox_.Focus();
            }
            else if (key == Key.Enter || key == Key.ImeAccept)
            {
                if (TextBox_.Text?.Length > 0)
                {
                    source.Text = TextBox_.Text;
                }
                Close();
            }
            else if (key == Key.Help)
            {
                _keyboardState = VirtualKeyboardState.Default;
                if (TransitioningContentControl_.Content is KeyboardLayout layout)
                {
                    var index = Layouts.IndexOf(layout.GetType());
                    if (Layouts.Count - 1 > index)
                    {
                        TransitioningContentControl_.Content = Activator.CreateInstance(Layouts[index + 1]);
                    }
                    else
                    {
                        TransitioningContentControl_.Content = Activator.CreateInstance(Layouts[0]);
                    }
                    // Обновить визуал после смены лейаута
                    Dispatcher.UIThread.Post(() =>
                    {
                        UpdateKeyVisuals(this.Bounds.Width);
                    }, DispatcherPriority.Render);
                }
            }
            else if (key == Key.Back)
            {

                if (TextBox_.Text != null && TextBox_.CaretIndex <= TextBox_.Text.Length && TextBox_.CaretIndex > 0)
                {
                    int dd = 0;
                    if (TextBox_.CaretIndex != TextBox_.Text.Length)
                    {
                        dd = TextBox_.CaretIndex - 1;
                    }

                    TextBox_.Text = TextBox_.Text.Remove(TextBox_.CaretIndex - 1, 1);
                    if (dd != 0)
                    {
                        TextBox_.CaretIndex = dd;
                    }


                }
                TextBox_.Focus();

            }
            else if (key == Key.BrowserFavorites)
            {
                _keyboardState = VirtualKeyboardState.Default;

                if (TransitioningContentControl_.Content is VirtualKeyboardLayoutNumpad)
                {
                    TransitioningContentControl_.Content = Activator.CreateInstance(DefaultLayout());
                }
                else
                {
                    TransitioningContentControl_.Content = Activator.CreateInstance(typeof(VirtualKeyboardLayoutNumpad));
                }
                // Обновить визуал после смены лейаута
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateKeyVisuals(this.Bounds.Width);
                }, DispatcherPriority.Render);
            }
            else if (key == Key.LaunchApplication1)
            {
                _keyboardState = VirtualKeyboardState.Default;

                if (TransitioningContentControl_.Content is VirtualKeyboardLayoutUS)
                {
                    TransitioningContentControl_.Content = Activator.CreateInstance(DefaultLayout());
                }
                else
                {
                    TransitioningContentControl_.Content = Activator.CreateInstance(typeof(VirtualKeyboardLayoutUS));
                }
                // Обновить визуал после смены лейаута
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateKeyVisuals(this.Bounds.Width);
                }, DispatcherPriority.Render);
            }
            else if (key == Key.LaunchApplication2)
            {
                _keyboardState = VirtualKeyboardState.Default;

                if (TransitioningContentControl_.Content is VirtualKeyboardLayoutRU)
                {
                    TransitioningContentControl_.Content = Activator.CreateInstance(DefaultLayout());
                }
                else
                {
                    TransitioningContentControl_.Content = Activator.CreateInstance(typeof(VirtualKeyboardLayoutRU));
                }
                // Обновить визуал после смены лейаута
                Dispatcher.UIThread.Post(() =>
                {
                    UpdateKeyVisuals(this.Bounds.Width);
                }, DispatcherPriority.Render);
            }
            else if (key == Key.Left)
            {
                TextBox_.CaretIndex = Math.Clamp(TextBox_.CaretIndex - 1, 0, TextBox_.Text.Length);
                TextBox_.Focus();
            }
            else if (key == Key.Right)
            {
                TextBox_.CaretIndex = Math.Clamp(TextBox_.CaretIndex + 1, 0, TextBox_.Text.Length);
                TextBox_.Focus();
            }
            else
            {
                TextBox_.Focus();



                //InputManager.Instance.ProcessInput(new RawKeyEventArgs(KeyboardDevice.Instance, (ulong)DateTime.Now.Ticks, (Window)TextBox.GetVisualRoot(), RawKeyEventType.KeyDown, key, RawInputModifiers.None));
                //InputManager.Instance.ProcessInput(new RawKeyEventArgs(KeyboardDevice.Instance, (ulong)DateTime.Now.Ticks, (Window)TextBox.GetVisualRoot(), RawKeyEventType.KeyUp, key, RawInputModifiers.None));
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}