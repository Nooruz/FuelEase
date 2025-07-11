using KIT.GasStation.State.Notifications;

namespace KIT.GasStation.ViewModels
{
    public class CustomToastViewModel
    {
        public virtual string Title { get; set; }
        public virtual string Message { get; set; }
        public virtual NotificationType Type { get; set; }
    }
}