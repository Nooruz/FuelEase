using System;

namespace KIT.GasStation.State.Notifications
{
    public interface INotificationStore
    {
        event Action<string, string, NotificationType> OnShowing;

        void Show(string title, string message, NotificationType type);

        void Show(string title, string message);
    }
    public enum NotificationType
    {
        Information,

        Warning,

        Error
    }
}
