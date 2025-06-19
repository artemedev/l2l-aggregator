﻿using Avalonia.Notification;
using l2l_aggregator.Services.Database.Repositories.Interfaces;
using l2l_aggregator.Services.Notification.Interface;
using System;
using System.Collections.ObjectModel;

namespace l2l_aggregator.Services.Notification
{

    public class NotificationService : INotificationService
    {
        private readonly INotificationLogRepository _notificationLog;
        private readonly SessionService _sessionService;
        public INotificationMessageManager Manager { get; }

        // Коллекция всех уведомлений

        public class NotificationItem
        {
            public string Message { get; set; } = "";
            public NotificationType Type { get; set; } = NotificationType.Info;
            public string? UserName { get; set; } // может быть null
            public DateTime CreatedAt { get; set; } = DateTime.Now;
        }
        public ObservableCollection<NotificationItem> Notifications { get; } = new();


        public NotificationService(SessionService sessionService, INotificationLogRepository notificationLog)
        {
            Manager = new NotificationMessageManager();
            _notificationLog = notificationLog;
            _sessionService = sessionService;
        }


        // Билдер по умолчанию для уведомлений
        public NotificationMessageBuilder Default()
        {
            // Настройки по умолчанию: фоновый цвет, анимация и т.п.
            return Manager.CreateMessage()
                          .Background("#333")
                          .Animates(true);
        }

        public INotificationMessage ShowMessage(string message, NotificationType type = NotificationType.Info, Action closeAction = null)
        {
            var userName = _sessionService.User?.USER_NAME;
            // Настройка цветов и заголовков по типу уведомления
            var (accentColor, header) = type switch
            {
                NotificationType.Info => ("#1751c3", "Информация"),
                NotificationType.Warn => ("#E0A030", "Предупреждение"),
                NotificationType.Error => ("#e03030", "Ошибка"),
                NotificationType.Success => ("#4BB543", "Успешно"),
                _ => ("#1751c3", "Информация")
            };
            var item = new NotificationItem
            {
                Message = message,
                Type = type,
                UserName = userName,
                CreatedAt = DateTime.Now
            };
            // Добавляем сообщение в список
            Notifications.Add(item);

            // Сохраняем в БД
            _ = _notificationLog.SaveNotificationAsync(item);

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
