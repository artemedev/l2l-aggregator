using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace l2l_aggregator.Helpers
{
    public static class SystemKeyboardHelper
    {
        public static void ShowSystemKeyboard()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ShowWindowsKeyboard();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                ShowLinuxKeyboard();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                ShowMacKeyboard();
            }
            else
            {
                // Если вы также целитесь на мобильные платформы (Android / iOS),
                // где Avalonia может работать, нужно использовать соответствующий способ
                // вызова клавиатуры (через нативные API). Для Android—через Android InputMethodManager и т.д.
            }
        }

        private static void ShowWindowsKeyboard()
        {
            // OSK (On-Screen Keyboard) — стандартная клавиатура в Windows
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "osk.exe",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // Обработка ошибок (например, если osk.exe недоступна)
                Console.WriteLine(ex);
            }
        }

        private static void ShowLinuxKeyboard()
        {
            // На Linux нет одной-единственной системной клавиатуры,
            // наиболее распространённые: onboard (Ubuntu), florence, matchbox-keyboard и т.д.
            // Нужно проверить, что что-то из них установлено и вызвать её.
            // Например, для Ubuntu "onboard":
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "onboard",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // Если onboard не установлена, можно попробовать другой вариант.
                Console.WriteLine(ex);
            }
        }

        private static void ShowMacKeyboard()
        {
            // На macOS нет «отдельно вызываемой» системной клавиатуры,
            // но можно включить "Accessibility Keyboard" (через настройки системы).
            // Прямого способа принудительно вызвать её командой обычно нет.
            // Теоретически можно открыть "Keyboard Viewer" через AppleScript:
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = "-e 'tell application \"System Events\" to key code 102 using {control down, option down, command down}'",
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                // Это не всегда сработает (зависит от настроек безопасности macOS)
                Console.WriteLine(ex);
            }
        }
    }
}
