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

        /// <summary>
        /// Статус ТРК
        /// </summary>
        [XmlIgnore]
        public ControllerStatus Status { get; set; }
    }

    /// <summary>
    /// Статус ТРК
    /// </summary>
    public enum ControllerStatus
    {
        /// <summary>
        /// Неизвестный
        /// </summary>
        Unknown,

        /// <summary>
        /// Ошибка данных или неверный формат [6-8]
        /// </summary>
        DataError = 0x0,

        /// <summary>
        /// Пистолет на месте, ТРК не авторизована [6, 7, 9]
        /// </summary>
        Off = 0x6,

        /// <summary>
        /// Пистолет снят, ТРК ждет авторизации [6, 7, 9]
        /// </summary>
        Call = 0x7,

        /// <summary>
        /// ТРК авторизована, но налив не начат [6, 7, 9]
        /// </summary>
        AuthorizedNotDelivering = 0x8,

        /// <summary>
        /// Идет налив топлива [6, 7, 10]
        /// </summary>
        Busy = 0x9,

        /// <summary>
        /// Налив завершен, пистолет на месте (PEOT) [6, 7, 10]
        /// </summary>
        TransactionCompletePeot = 0xA,

        /// <summary>
        /// Налив завершен, пистолет на месте (FEOT) [6, 7, 10]
        /// </summary>
        TransactionCompleteFeot = 0xB,

        /// <summary>
        /// ТРК остановлена командой Stop [7, 11, 12]
        /// </summary>
        PumpStop = 0xC,

        /// <summary>
        /// Готовность к приему блока данных (Data Next) [7, 11]
        /// </summary>
        SendData = 0xD
    }
}
