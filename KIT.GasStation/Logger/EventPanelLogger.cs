using Serilog.Events;
using System;

namespace KIT.GasStation.Logger
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
