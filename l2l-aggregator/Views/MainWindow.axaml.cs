using Avalonia.Controls;
using Avalonia.Input;
using l2l_aggregator.Controls.Keyboard.Layout;
using l2l_aggregator.Controls.Keyboard;
using System.Timers;
using l2l_aggregator.ViewModels;
using l2l_aggregator.Services;
using Avalonia.Controls.Primitives;

namespace l2l_aggregator.Views
{
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _viewModel => this.DataContext as MainWindowViewModel;
        private VirtualKeyboardTextInputMethod virtualKeyboardTextInput = null;
        private bool iskeyboardCooldown;
        private bool iskeyboardCooldownStarted;
        //private bool _isVirtualKeyboardDisabled = true;
        private l2l_aggregator.Controls.Keyboard.VirtualKeyboard keyboard;

        private Timer keyboardCoolDown;

        public MainWindow()
        {

            l2l_aggregator.Controls.Keyboard.VirtualKeyboard.AddLayout<VirtualKeyboardLayoutUS>();
            // VirtualKeyboard.Controls.Keyboard.VirtualKeyboard.AddLayout<VirtualKeyboardLayoutDE>();
            l2l_aggregator.Controls.Keyboard.VirtualKeyboard.AddLayout<VirtualKeyboardLayoutRU>();
            l2l_aggregator.Controls.Keyboard.VirtualKeyboard.SetDefaultLayout(() => typeof(VirtualKeyboardLayoutRU));


            InitializeComponent();

            var notificationsButton = this.FindControl<Button>("NotificationsButton");
            var notificationsFlyout = notificationsButton?.Flyout as Flyout;

            notificationsButton.Click += (_, _) =>
            {
                if (notificationsFlyout != null)
                {
                    // Подгоняем ширину Flyout к ширине кнопки
                    var flyoutBorder = notificationsFlyout.Content as Border;
                    if (flyoutBorder != null)
                    {
                        flyoutBorder.Width = this.Bounds.Width / 3;
                    }

                    FlyoutBase.ShowAttachedFlyout(notificationsButton);
                }
            };

            virtualKeyboardTextInput = new VirtualKeyboardTextInputMethod((Window)this);

            keyboard = this.GetControl<l2l_aggregator.Controls.Keyboard.VirtualKeyboard>("VirtualKeyboardControl");

            keyboardCoolDown = new Timer();
            keyboardCoolDown.Interval = 200;
            keyboardCoolDown.Elapsed += resetCoolDown;

            this.AddHandler<GotFocusEventArgs>(Control.GotFocusEvent, openVirtualKeyboard);
        }
        private void resetCoolDown(object? sender, ElapsedEventArgs e)
        {
            iskeyboardCooldown = false;
            iskeyboardCooldownStarted = false;
            keyboardCoolDown.Stop();
        }
        private void openVirtualKeyboard(object? sender, GotFocusEventArgs e)
        {
            if (SessionService.Instance.DisableVirtualKeyboard)
                return;

            if (iskeyboardCooldown && !iskeyboardCooldownStarted)
            {
                iskeyboardCooldownStarted = true;
                keyboardCoolDown.Start();
            }

            if (e.Source.GetType() == typeof(TextBox) && !keyboard.IsVisible)
            {

                //virtualKeyboardTextInput.SetActive(true, e);
                var tb = e.Source as Control;




                switch (tb.Tag?.ToString())
                {
                    case "numpad":
                        keyboard.ShowKeyboard(e.Source as TextBox, typeof(VirtualKeyboardLayoutNumpad));
                        break;
                    case "ru-RU":
                        keyboard.ShowKeyboard(e.Source as TextBox, typeof(VirtualKeyboardLayoutRU));
                        break;
                    default:
                        keyboard.ShowKeyboard(e.Source as TextBox);
                        break;
                }



                //keyboard.IsVisible = true;
                //((Control)keyboard.Parent).IsVisible = true;

                iskeyboardCooldown = true;
            }


        }
    }
}