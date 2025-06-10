using Serilog;
using Serilog.Events;
using System;

namespace FuelEase.Logger
{
    public interface IEventPanelLogger : ILogger
    {
        event Action<LogEvent> OnLogger;
    }
}
