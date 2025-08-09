using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.Lanfeng;
using KIT.GasStation.PKElectronics;

namespace KIT.GasStation.Common.Factories
{
    public class FuelDispenserFactory : IFuelDispenserFactory
    {
        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly CreateFuelDispenser<LanfengFuelDispenser> _createLanfengFuelDispenser;
        private readonly CreateFuelDispenser<PKElectronicsFuelDispenser> _createPKElectronicsFuelDispenser;
        //private readonly CreateFuelDispenser<TechnoProjektFuelDispenser> _createTechnoProjektFuelDispenser;
        //private readonly CreateFuelDispenser<EmulatorFuelDispenser> _createEmulatorFuelDispenser;

        public FuelDispenserFactory(IHardwareConfigurationService hardwareConfigurationService,
            CreateFuelDispenser<LanfengFuelDispenser> createLanfengFuelDispenser,
            CreateFuelDispenser<PKElectronicsFuelDispenser> createPKElectronicsFuelDispenser)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
            _createLanfengFuelDispenser = createLanfengFuelDispenser;
            _createPKElectronicsFuelDispenser = createPKElectronicsFuelDispenser;
        }

        public IFuelDispenserService Create(ControllerType controllerType)
        {
            return controllerType switch
            {
                ControllerType.Lanfeng => _createLanfengFuelDispenser(),
                ControllerType.Gilbarco => throw new NotSupportedException($"Тип контроллера {controllerType} не поддерживается."),
                //ControllerType.Emulator => _createEmulatorFuelDispenser(),
                ControllerType.PKElectronics => _createPKElectronicsFuelDispenser(),
                //ControllerType.TechnoProjekt => _createTechnoProjektFuelDispenser(),
                _ => throw new NotSupportedException($"Тип контроллера {controllerType} не поддерживается."),
            };
        }
    }
}
