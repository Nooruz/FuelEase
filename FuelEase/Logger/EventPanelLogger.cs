using Serilog.Events;
using System;

namespace FuelEase.Logger
{
    public class EventPanelLogger : IEventPanelLogger
    {
        public event Action<LogEvent> OnLogger;

        public void Write(LogEvent logEvent)
        {
            OnLogger?.Invoke(logEvent);
        }
    }
}
