using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.Lanfeng;
using KIT.GasStation.PKElectronics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace KIT.GasStation.Common.Factories
{
    public class FuelDispenserFactory : IFuelDispenserFactory
    {
        public IFuelDispenserService Create(IServiceProvider sp, Controller controller, int address, IHubContext<DeviceResponseHub, IDeviceResponseClient> hub)
        {
            return controller.Type switch
            {
                ControllerType.Lanfeng => ActivatorUtilities.CreateInstance<LanfengFuelDispenser>(sp, controller, address, hub),
                ControllerType.Gilbarco => throw new NotSupportedException($"Тип контроллера {controller.Type} не поддерживается."),
                //ControllerType.Emulator => _createEmulatorFuelDispenser(),
                ControllerType.PKElectronics => ActivatorUtilities.CreateInstance<PKElectronicsFuelDispenser>(sp, controller, address),
                //ControllerType.TechnoProjekt => _createTechnoProjektFuelDispenser(),
                _ => throw new NotSupportedException($"Тип контроллера {controller.Type} не поддерживается."),
            };
        }
    }
}
