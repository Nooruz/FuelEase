using System.IO.Ports;
using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    [Serializable]
    public class GilbarcoControllerSettings : ControllerSettings
    {
        /// <summary>
        /// Контроль
        /// </summary>
        [XmlAttribute]
        public Parity Parity { get; set; }

        /// <summary>
        /// Подавление эхо
        /// </summary>
        [XmlAttribute]
        public bool EchoSuppression { get; set; }
    }
}
