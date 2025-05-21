using Avalonia.Notification;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.Notification.Interface
{
    public interface INotificationService
    {
        INotificationMessageManager Manager { get; }

        INotificationMessage ShowMessage(string message, string header = null, Action closeAction = null);

        NotificationMessageBuilder Default();

        // Коллекция уведомлений для отображения в Flyout
        ObservableCollection<string> Notifications { get; }
    }
}
