using Serilog;
using Serilog.Events;
using System;

namespace KIT.GasStation.Logger
{
    public interface IEventPanelLogger : ILogger
    {
        event Action<LogEvent> OnLogger;
    }
}
