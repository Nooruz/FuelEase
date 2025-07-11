using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    [Serializable]
    [XmlInclude(typeof(LanfengControllerSettings))]
    [XmlInclude(typeof(PKElectronicsControllerSettings))]
    public abstract class ControllerSettings
    {
        [XmlAttribute]
        public string CommonSetting { get; set; }
    }
}
