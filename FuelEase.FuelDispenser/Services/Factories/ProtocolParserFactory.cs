using FuelEase.Domain.Models;
using FuelEase.HardwareConfigurations.Models;

namespace FuelEase.FuelDispenser.Services.Factories
{
    public class ProtocolParserFactory : IProtocolParserFactory
    {
        #region Private Members

        private readonly ICommandEncoderFactory _commandEncoderFactory;

        #endregion

        #region Constructors

        public ProtocolParserFactory(ICommandEncoderFactory commandEncoderFactory)
        {
            _commandEncoderFactory = commandEncoderFactory;
        }

        #endregion

        public IProtocolParser CreateIProtocolParser(ControllerType controllerType,
            List<Nozzle> nozzles)
        {
            return controllerType switch
            {
                ControllerType.Lanfeng =>
                    new LanfengProtocolParser(_commandEncoderFactory.Create(ControllerType.Lanfeng)),
                ControllerType.PKElectronics => 
                    new PKElectronicsProtocolParser(_commandEncoderFactory.Create(ControllerType.PKElectronics), nozzles),
                _ => throw new ArgumentException("Unknown controller type"),
            };
        }
    }
}
