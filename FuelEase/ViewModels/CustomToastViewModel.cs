using FuelEase.State.Notifications;

namespace FuelEase.ViewModels
{
    public class CustomToastViewModel
    {
        public virtual string Title { get; set; }
        public virtual string Message { get; set; }
        public virtual NotificationType Type { get; set; }
    }
}