using Avalonia.Notification;
using l2l_aggregator.Services.Notification.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Notification
{
    public class NotificationService : INotificationService
    {
        public INotificationMessageManager Manager { get; }

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

        public INotificationMessage ShowMessage(string message, string header = null, Action closeAction = null)
        {
            return Manager
                .CreateMessage()
                .Accent("#1751c3") // Цвет полоски
                .Animates(true)    // Анимация появления
                .Background("#FFFFFF") // Цвет фона
                .HasBadge("Info") // Значок
                .HasMessage(message) // Сообщение
                .Dismiss().WithButton("OK", button => { }) // Кнопка
                .Dismiss().WithDelay(TimeSpan.FromSeconds(5000)) // Автоматическое скрытие
                .Queue();
        }
    }
}
