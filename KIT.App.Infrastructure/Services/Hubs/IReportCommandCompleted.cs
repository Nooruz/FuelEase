using KIT.GasStation.FuelDispenser.Models;

namespace KIT.App.Infrastructure.Services.Hubs
{
    public interface IReportCommandCompleted
    {
        Task ReportCommandCompletedAsync(CommandCompletion completion, CancellationToken ct = default);
    }
}
