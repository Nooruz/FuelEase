using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.FuelDispenser.Services.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.Extensions.Logging;

namespace KIT.GasStation.FuelDispenser
{
    public abstract class FuelDispenserServiceBase : IFuelDispenserService
    {
        protected readonly Controller Controller;
        protected readonly IProtocolParserFactory _protocolParserFactory;
        protected readonly IPortManager _portManager;
        protected readonly IHubClient _hubClient;
        protected readonly int Address;
        protected readonly IReadOnlyList<Column> Columns;

        public string DispenserName => throw new NotImplementedException();

        public string Version => throw new NotImplementedException();

        public Guid ControllerId => throw new NotImplementedException();

        public NozzleStatus Status {  get; set; }
        

        protected FuelDispenserServiceBase(Controller controller,
            int address,
            IProtocolParserFactory protocolParserFactory,
            IPortManager portManager,
            IHubClient hubClient)
        {
            Controller = controller;
            Columns = Controller.Columns.Where(c => c.Address == address).ToList();
            Address = address;
            _protocolParserFactory = protocolParserFactory;
            _portManager = portManager;
            _hubClient = hubClient;
        }

        public async Task RunAsync(CancellationToken token)
        {
            await OnOpenAsync(token);
        }

        protected virtual Task OnOpenAsync(CancellationToken token) => Task.CompletedTask;
        protected virtual Task OnCloseAsync() => Task.CompletedTask;

        // Обязательный шаг: тик опроса/обработки
        protected abstract Task OnTickAsync(CancellationToken token);

        public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
