using Avalonia.Notification;
using l2l_aggregator.Services.Notification.Interface;
using System;
using System.Collections.ObjectModel;

namespace l2l_aggregator.Services.Notification
{

    public class NotificationService : INotificationService
    {
        public INotificationMessageManager Manager { get; }

        // Коллекция всех уведомлений
        public ObservableCollection<string> Notifications { get; } = new();

        public NotificationService()
        {
            Manager = new NotificationMessageManager();
        }

        /// <summary>
        /// Билдер по умолчанию для уведомлений
        /// </summary>
        public NotificationMessageBuilder Default()
        {
            // Настройки по умолчанию: фоновый цвет, анимация и т.п.
            return Manager.CreateMessage()
                          .Background("#333")
                          .Animates(true);
        }

        public INotificationMessage ShowMessage(string message, NotificationType type = NotificationType.Info, Action closeAction = null)
        {
            // Настройка цветов и заголовков по типу уведомления
            var (accentColor, header) = type switch
            {
                NotificationType.Info => ("#1751c3", "Информация"),
                NotificationType.Warn => ("#E0A030", "Предупреждение"),
                NotificationType.Error => ("#e03030", "Ошибка"),
                _ => ("#1751c3", "Информация")
            };
            // Добавляем сообщение в список
            Notifications.Add(message);

            //HasHeader type "Info","Warn", "Error"
            //Accent "Info"-"#1751c3", "Warn"-"#E0A030", "Error"-#e03030
            var builder = Manager
                .CreateMessage()
                .Accent(accentColor)
                .Animates(true)
                .Background("#FFFFFF")
                .HasBadge(type.ToString())
                .HasHeader(header)
                .HasMessage(message)
                .Dismiss().WithButton("OK", button => { }); // Кнопка


            if (closeAction != null)
            {
                builder.Dismiss().WithButton("Закрыть", b => closeAction());
            }

            return builder.Dismiss().WithDelay(TimeSpan.FromSeconds(5)).Queue();
        }
    }
}
