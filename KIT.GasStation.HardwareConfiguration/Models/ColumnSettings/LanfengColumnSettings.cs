using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    [Serializable]
    public class LanfengColumnSettings : ColumnSettings
    {
        [XmlAttribute]
        public LanfengFuelRequestType LanfengFuelRequestType { get; set; }
    }

    /// <summary>
    /// Тип заявки на ТРК Ланфэнг
    /// </summary>
    public enum LanfengFuelRequestType
    {
        /// <summary>
        /// Только на сумму
        /// </summary>
        [Display(Name = "Только на сумму")]
        ByAmount,

        /// <summary>
        /// Только на литры
        /// </summary>
        [Display(Name = "Только на литры")]
        ByVolume,

        /// <summary>
        /// На целые литры
        /// </summary>
        [Display(Name = "На целые литры")]
        ByWholeLiters
    }
}
