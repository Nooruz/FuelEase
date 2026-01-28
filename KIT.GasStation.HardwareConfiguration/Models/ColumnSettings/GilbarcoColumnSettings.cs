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
        public ColumnQuantity ColumnQuantity { get; set; }

        /// <summary>
        /// Цена, после запятой
        /// </summary>
        [XmlAttribute]
        public PriceDecimalPoint PriceDecimalPoint { get; set; }

        /// <summary>
        /// Статус пистолета
        /// </summary>
        [XmlIgnore]
        public ColumnStatus Status { get; set; }
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
    public enum ColumnQuantity
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

    /// <summary>
    /// Статус пистолета
    /// </summary>
    public enum ColumnStatus
    {
        /// <summary>
        /// Неизвестный
        /// </summary>
        Unknown,

        /// <summary>
        /// Пистолет находится в гнезде [2, 14]
        /// </summary>
        NozzleIn = 0,

        /// <summary>
        /// Пистолет снят [2, 14]
        /// </summary>
        NozzleOut = 1,

        /// <summary>
        /// Требуется выбор уровня цены [2, 14]
        /// </summary>
        PriceLevelNeeded = 2, 

        /// <summary>
        /// Требуется выбор сорта топлива [2, 14]
        /// </summary>
        GradeNeeded = 3,

        /// <summary>
        /// Ожидание нажатия кнопки «Старт» на ТРК [2, 15]
        /// </summary>
        PushToStartNeeded = 4
    }
}
