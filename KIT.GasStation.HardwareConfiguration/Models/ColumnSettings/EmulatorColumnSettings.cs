using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    [Serializable]
    public sealed class EmulatorColumnSettings : ColumnSettings
    {
        /// <summary>
        /// Последняя установленная цена
        /// </summary>
        [XmlAttribute]
        public decimal LastPrice { get; set; }

        /// <summary>
        /// Последнее установленное количество
        /// </summary>
        [XmlAttribute]
        public decimal LastQuantity { get; set; }

        /// <summary>
        /// Последняя установленная сумма
        /// </summary>
        [XmlAttribute]
        public decimal LastSum { get; set; }

        /// <summary>
        /// Залишившееся количество
        /// </summary>
        [XmlAttribute]
        public decimal ReceivedQuantity { get; set; }

        /// <summary>
        /// Залишившаяся сумма
        /// </summary>
        [XmlAttribute]
        public decimal ReceivedSum { get; set; }
    }
}
