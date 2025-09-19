using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.FuelDispenser.Services.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace KIT.GasStation.PKElectronics
{
    public class PKElectronicsFuelDispenser : FuelDispenserServiceBase
    {
        #region Private Members

        private readonly IProtocolParser _protocolParser;
        private readonly IHubContext<DeviceResponseHub, IDeviceResponseClient> _hub;

        #endregion
        
        #region Constructors

        public PKElectronicsFuelDispenser(Controller controller, 
            ILogger<PKElectronicsFuelDispenser> logger,
            int address,
            IProtocolParserFactory protocolParserFactory,
            ISharedSerialPortService sharedSerialPortService,
            IHubContext<DeviceResponseHub, IDeviceResponseClient> hub) 
            : base(controller, logger, address, protocolParserFactory, sharedSerialPortService, hub)
        {
            _protocolParser = protocolParserFactory.CreateIProtocolParser(Controller.Type);
            _hub = hub;
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
