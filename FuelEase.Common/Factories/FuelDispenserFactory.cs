using FuelEase.Domain.Models;
using FuelEase.FuelDispenser.Services;
using FuelEase.HardwareConfigurations.Models;
using FuelEase.HardwareConfigurations.Services;
using FuelEase.Lanfeng;
using FuelEase.PKElectronics;

namespace FuelEase.Common.Factories
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

        public IFuelDispenserService Create(ControllerType controllerType, IEnumerable<Nozzle>? nozzles = null)
        {
            return controllerType switch
            {
                ControllerType.Lanfeng => _createLanfengFuelDispenser(nozzles),
                ControllerType.Gilbarco => throw new NotSupportedException($"Тип контроллера {controllerType} не поддерживается."),
                //ControllerType.Emulator => _createEmulatorFuelDispenser(),
                ControllerType.PKElectronics => _createPKElectronicsFuelDispenser(nozzles),
                //ControllerType.TechnoProjekt => _createTechnoProjektFuelDispenser(),
                _ => throw new NotSupportedException($"Тип контроллера {controllerType} не поддерживается."),
            };
        }

        public async Task<IFuelDispenserService?> CreateAsync(IEnumerable<Nozzle> nozzles)
        {
            var firstNozzle = nozzles.FirstOrDefault();

            if (firstNozzle == null) return null;

            Column? column = await _hardwareConfigurationService.GetColumnByIdAsync(firstNozzle.ColumnId) ?? throw new ArgumentException($"Колонка с идентификатором {firstNozzle.ColumnId} не найдена.");
            
            return Create(column.Controller.Type, nozzles);
        }
    }
}
