using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.FuelDispenser.Services;

namespace KIT.GasStation.Gilbarco.Helpers
{
    public static class ProtocolParser
    {
        #region Private Members

        private const byte StartOfText = 0xFF;
        private const byte EndOfText = 0xF0;
        private const byte DataControl_VolumePreset = 0xF1;
        private const byte DataControl_MoneyPreset = 0xF2;
        private const byte DataControl_Level1 = 0xF4;
        private const byte DataControl_Level2 = 0xF5;
        private const byte DataControl_GradeNext = 0xF6;
        private const byte DataControl_PpuNext = 0xF7;
        private const byte DataControl_PresetAmountNext = 0xF8;
        private const byte DataControl_LrcNext = 0xFB;
        private const byte DataWordPrefix = 0xE0;

        #endregion

        public static byte[] BuildRequest(Command cmd, int controllerAddress,
            int columnAddress = 0, decimal? value = null, bool bySum = true,
            LanfengControllerType controllerType = LanfengControllerType.Single)
        {
            return null;
        }

        public static ControllerResponse ParseResponse(byte[] rawResponse)
        {
            return new ControllerResponse();
        }
    }
}
