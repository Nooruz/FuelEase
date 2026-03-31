using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace KIT.App.Infrastructure.Services.Hubs
{
    public sealed class ReportCommandCompleted : IReportCommandCompleted
    {
        private readonly IHubClient _hubClient;
        private readonly ILogger<ReportCommandCompleted> _logger;

        public ReportCommandCompleted(
            IHubClient hubClient,
            ILogger<ReportCommandCompleted> logger)
        {
            _hubClient = hubClient;
            _logger = logger;
        }


        public Task ReportCommandCompletedAsync(CommandCompletion completion, CancellationToken ct = default) =>
        _hubClient.Connection.InvokeAsync("ReportCommandCompleted", completion, ct);

    }
}
