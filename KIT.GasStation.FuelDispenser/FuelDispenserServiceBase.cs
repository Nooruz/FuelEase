using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.FuelDispenser.Services.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace KIT.GasStation.FuelDispenser
{
    public abstract class FuelDispenserServiceBase : IFuelDispenserService
    {
        protected readonly Controller Controller;
        protected readonly ILogger<FuelDispenserServiceBase> Logger;
        protected readonly IProtocolParserFactory _protocolParserFactory;
        protected readonly ISharedSerialPortService _sharedSerialPortService;
        protected readonly IHubContext<DeviceResponseHub, IDeviceResponseClient> _hub;

        public string DispenserName => throw new NotImplementedException();

        public string Version => throw new NotImplementedException();

        public Guid ControllerId => throw new NotImplementedException();

        protected FuelDispenserServiceBase(Controller controller, 
            ILogger<FuelDispenserServiceBase> logger,
            int address,
            IProtocolParserFactory protocolParserFactory,
            ISharedSerialPortService sharedSerialPortService,
            IHubContext<DeviceResponseHub, IDeviceResponseClient> hub)
        {
            Controller = controller;
            Logger = logger;
            _protocolParserFactory = protocolParserFactory;
            _sharedSerialPortService = sharedSerialPortService;
            _hub = hub;
        }

        public async Task RunAsync(CancellationToken token)
        {
            Logger.LogInformation("Start {Type} on {Port}", Controller.Type, Controller.ComPort);

            await OnOpenAsync(token);
            try
            {
                while (!token.IsCancellationRequested)
                    await OnTickAsync(token); // вся специфика протокола тут
            }
            finally
            {
                await OnCloseAsync();
                Logger.LogInformation("Stop {Type} on {Port}", Controller.Type, Controller.ComPort);
            }
        }

        protected virtual Task OnOpenAsync(CancellationToken token) => Task.CompletedTask;
        protected virtual Task OnCloseAsync() => Task.CompletedTask;

        // Обязательный шаг: тик опроса/обработки
        protected abstract Task OnTickAsync(CancellationToken token);

        public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
