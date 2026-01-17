using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.GasStation.FuelDispenser.Services.Factories
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

        public IProtocolParser CreateIProtocolParser(ControllerType controllerType)
        {
            return controllerType switch
            {
                ControllerType.Lanfeng =>
                    new LanfengProtocolParser(_commandEncoderFactory.Create(controllerType)),
                ControllerType.PKElectronics => 
                    new PKElectronicsProtocolParser(_commandEncoderFactory.Create(controllerType)),
                ControllerType.TechnoProjekt => 
                    new TechnoprojectProtocolParser(_commandEncoderFactory.Create(controllerType)),
                ControllerType.Gilbarco => 
                    new GilbarcoProtocolParser(_commandEncoderFactory.Create(controllerType)),
                _ => throw new ArgumentException("Unknown controller type"),
            };
        }
    }
}
