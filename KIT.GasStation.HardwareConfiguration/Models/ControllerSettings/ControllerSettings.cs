using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    [Serializable]
    [XmlInclude(typeof(LanfengControllerSettings))]
    [XmlInclude(typeof(PKElectronicsControllerSettings))]
    [XmlInclude(typeof(GilbarcoControllerSettings))]
    public abstract class ControllerSettings
    {
        [XmlAttribute]
        public string CommonSetting { get; set; }
    }
}
