using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    [Serializable]
    public class GilbarcoColumnSettings : ColumnSettings
    {
        /// <summary>
        /// Тип счетчика
        /// </summary>
        [XmlAttribute]
        public CounterType CounterType { get; set; }

        /// <summary>
        /// Количество пистолетов на сторону
        /// </summary>
        [XmlAttribute]
        public NozzleQuantity NozzleQuantity { get; set; }

        /// <summary>
        /// Цена, после запятой
        /// </summary>
        [XmlAttribute]
        public PriceDecimalPoint PriceDecimalPoint { get; set; }
    }

    /// <summary>
    /// Тип счетчика
    /// </summary>
    public enum CounterType
    {
        /// <summary>
        /// Миллилитры
        /// </summary>
        Milliliter,

        /// <summary>
        /// Сантилитры
        /// </summary>
        Centimeter
    }

    /// <summary>
    /// Количество пистолетов на сторону
    /// </summary>
    public enum NozzleQuantity
    {
        None,

        [Display(Name = "1 пистолет")]
        One,

        [Display(Name = "2 пистолета")]
        Two,

        [Display(Name = "3 пистолета")]
        Three,

        [Display(Name = "4 пистолета")]
        Four,

        [Display(Name = "5 пистолетов")]
        Five,

        [Display(Name = "6 пистолетов")]
        Six
    }

    /// <summary>
    /// Цена, после запятой
    /// </summary>
    public enum PriceDecimalPoint
    {
        [Display(Name = "0 знаков")]
        None,

        [Display(Name = "1 знак")]
        One,

        [Display(Name = "2 знака")]
        Two,

        [Display(Name = "3 знака")]
        Three
    }
}
