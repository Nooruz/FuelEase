using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Models;

namespace KIT.GasStation.TechnoProjekt.Utilities
{
    public static class ProtocolParser
    {
        public static byte[] BuildRequest(Command command, int address, int mask, decimal? sum)
        {
            return null;
        }

        //public static ControllerResponse ParseResponse(byte[] rawResponse)
        //{
        //    return new ControllerResponse();
        //}
    }
}
