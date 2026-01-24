using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.FuelDispenserEmulator;
using KIT.GasStation.Gilbarco;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.Lanfeng;
using KIT.GasStation.PKElectronics;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace KIT.GasStation.Common.Factories
{
    public class FuelDispenserFactory : IFuelDispenserFactory
    {
        public IFuelDispenserService Create(IServiceProvider sp, Controller controller, int address, ISharedSerialPortService port)
        {
            var hubClient = sp.GetRequiredService<IHubClient>();

            return controller.Type switch
            {
                ControllerType.Lanfeng => ActivatorUtilities.CreateInstance<LanfengFuelDispenser>(sp, controller, address, hubClient, port),
                ControllerType.Gilbarco => ActivatorUtilities.CreateInstance<GilbarcoFuelDispenser>(sp, controller, hubClient, port),
                //ControllerType.Emulator => _createEmulatorFuelDispenser(),
                ControllerType.PKElectronics => ActivatorUtilities.CreateInstance<PKElectronicsFuelDispenser>(sp, controller, address, hubClient),
                //ControllerType.TechnoProjekt => _createTechnoProjektFuelDispenser(),
                _ => throw new NotSupportedException($"Тип контроллера {controller.Type} не поддерживается."),
            };
        }

        public IFuelDispenserService Create(IServiceProvider sp, Controller controller, ISharedSerialPortService port)
        {
            var hubClient = sp.GetRequiredService<IHubClient>();

            return controller.Type switch
            {
                ControllerType.Gilbarco => ActivatorUtilities.CreateInstance<GilbarcoFuelDispenser>(sp, controller, hubClient, port),
                ControllerType.PKElectronics => ActivatorUtilities.CreateInstance<PKElectronicsFuelDispenser>(sp, controller, hubClient),
                _ => throw new NotSupportedException($"Тип контроллера {controller.Type} не поддерживается."),
            };
        }

        public IFuelDispenserService Create(IServiceProvider sp, Controller controller)
        {
            var hubClient = sp.GetRequiredService<IHubClient>();

            return ActivatorUtilities.CreateInstance<EmulatorFuelDispenser>(sp, controller, hubClient);
        }
    }
}
