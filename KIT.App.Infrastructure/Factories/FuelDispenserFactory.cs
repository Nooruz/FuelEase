using KIT.GasStation.Emulator;
using KIT.GasStation.FuelDispenser;
using KIT.GasStation.HardwareConfigurations.Models;
using Microsoft.Extensions.DependencyInjection;

namespace KIT.App.Infrastructure.Factories
{
    public class FuelDispenserFactory : IFuelDispenserFactory
    {
        private readonly IServiceProvider _provider;

        public FuelDispenserFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        //public IFuelDispenserService Create(IServiceProvider sp, Controller controller, int address, ISharedSerialPortService port)
        //{
        //    var hubClient = sp.GetRequiredService<IHubClient>();

        //    return controller.Type switch
        //    {
        //        ControllerType.Lanfeng => ActivatorUtilities.CreateInstance<LanfengFuelDispenser>(sp, controller, address, hubClient, port),
        //        ControllerType.Gilbarco => ActivatorUtilities.CreateInstance<GilbarcoFuelDispenser>(sp, controller, hubClient, port),
        //        //ControllerType.Emulator => _createEmulatorFuelDispenser(),
        //        ControllerType.PKElectronics => ActivatorUtilities.CreateInstance<PKElectronicsFuelDispenser>(sp, controller, address, hubClient),
        //        //ControllerType.TechnoProjekt => _createTechnoProjektFuelDispenser(),
        //        _ => throw new NotSupportedException($"Тип контроллера {controller.Type} не поддерживается."),
        //    };
        //}

        //public IFuelDispenserService Create(IServiceProvider sp, Controller controller, ISharedSerialPortService port)
        //{
        //    var hubClient = sp.GetRequiredService<IHubClient>();

        //    return controller.Type switch
        //    {
        //        ControllerType.Gilbarco => ActivatorUtilities.CreateInstance<GilbarcoFuelDispenser>(sp, controller, hubClient, port),
        //        ControllerType.PKElectronics => ActivatorUtilities.CreateInstance<PKElectronicsFuelDispenser>(sp, controller, hubClient),
        //        _ => throw new NotSupportedException($"Тип контроллера {controller.Type} не поддерживается."),
        //    };
        //}

        //public IFuelDispenserService Create(IServiceProvider sp, Controller controller)
        //{
        //    var hubClient = sp.GetRequiredService<IHubClient>();

        //    return ActivatorUtilities.CreateInstance<EmulatorFuelDispenser>(sp, controller, hubClient);
        //}

        public IFuelDispenserService Create(ControllerType type)
        {
            return type switch
            {
                ControllerType.Emulator =>
                    _provider.GetRequiredService<EmulatorFuelDispenser>(),

                //ControllerType.Lanfeng =>
                //    _provider.GetRequiredService<LanfengFuelDispenser>(),

                //ControllerType.Gilbarco =>
                //    _provider.GetRequiredService<GilbarcoFuelDispenser>(),

                _ => throw new NotSupportedException($"Контроллер типа {type} не поддерживается.")
            };
        }
    }
}
