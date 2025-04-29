using Avalonia.Controls;
using Avalonia.Input;
using l2l_aggregator.Controls.Keyboard.Layout;
using l2l_aggregator.Controls.Keyboard;
using System.Timers;

namespace l2l_aggregator.Views
{
    public partial class MainWindow : Window
    {
        private VirtualKeyboardTextInputMethod virtualKeyboardTextInput = null;
        private bool iskeyboardCooldown;
        private bool iskeyboardCooldownStarted;

        private l2l_aggregator.Controls.Keyboard.VirtualKeyboard keyboard;

        private Timer keyboardCoolDown;

        public MainWindow()
        {

            l2l_aggregator.Controls.Keyboard.VirtualKeyboard.AddLayout<VirtualKeyboardLayoutUS>();
            // VirtualKeyboard.Controls.Keyboard.VirtualKeyboard.AddLayout<VirtualKeyboardLayoutDE>();
            l2l_aggregator.Controls.Keyboard.VirtualKeyboard.AddLayout<VirtualKeyboardLayoutRU>();
            l2l_aggregator.Controls.Keyboard.VirtualKeyboard.SetDefaultLayout(() => typeof(VirtualKeyboardLayoutRU));


            InitializeComponent();


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