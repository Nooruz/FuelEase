using System.Xml.Serialization;

namespace FuelEase.HardwareConfigurations.Models
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
