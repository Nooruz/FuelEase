using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.FuelDispenser.Services.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Serilog;

namespace KIT.GasStation.PKElectronics
{
    public class PKElectronicsFuelDispenser : FuelDispenserServiceBase
    {
        #region Private Members

        private readonly IProtocolParser _protocolParser;
        private readonly IHubClient _hubClient;
        private readonly ILogger _logger;

        #endregion

        #region Constructors

        public PKElectronicsFuelDispenser(Controller controller,
            int address,
            IProtocolParserFactory protocolParserFactory,
            IPortManager portManager,
            IHubClient hubClient) 
            : base(controller, address, protocolParserFactory, portManager, hubClient)
        {
            _protocolParser = protocolParserFactory.CreateIProtocolParser(Controller.Type);
            _hubClient = hubClient;
        }

        #endregion

        #region Protected

        protected override Task OnOpenAsync(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        protected override async Task OnTickAsync(CancellationToken token)
        {

        }

        protected override Task OnCloseAsync()
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}
