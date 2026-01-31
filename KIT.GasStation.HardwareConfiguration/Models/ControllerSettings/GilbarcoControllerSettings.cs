using System.IO.Ports;
using System.Xml.Serialization;

namespace KIT.GasStation.HardwareConfigurations.Models
{
    [Serializable]
    public class GilbarcoControllerSettings : ControllerSettings
    {
        #region Private Members

        private GilbarcoStatus _status;
        private bool _isLifted;
        private MiscPumpConfig _pumpConfig = new();

        #endregion

        #region Public Properties

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
        public GilbarcoStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>
        /// Пистолет поднят или нет
        /// </summary>
        [XmlIgnore]
        public bool IsLifted
        {
            get => _isLifted;
            set
            {
                _isLifted = value;
                OnPropertyChanged(nameof(IsLifted));
            }
        }

        [XmlIgnore]
        public MiscPumpConfig PumpConfig
        {
            get => _pumpConfig;
            set
            {
                _pumpConfig = value;
                OnPropertyChanged(nameof(PumpConfig));
            }
        }

        #endregion

        #region Public Voids

        public override void SetStatus(object status)
        {
            if (status is GilbarcoStatus gilbarcoStatus)
            {
                Status = gilbarcoStatus;
            }
        }

        public override object GetStatus()
        {
            return Status;
        }

        public override bool GetIsLifted()
        {
            return IsLifted;
        }

        public override void SetIsLifted(bool isLifted)
        {
            IsLifted = isLifted;
        }

        public override void SetConfig(object config)
        {
            if (config is MiscPumpConfig pumpConfig)
            {
                PumpConfig = pumpConfig;
            }
        }

        public override object GetConfig()
        {
            return PumpConfig;
        }

        #endregion
    }

    /// <summary>
    /// Статусы ответа ТРК (старший ниббл байта ответа) [3].
    /// </summary>
    public enum GilbarcoStatus
    {
        /// <summary>
        /// Ошибка в формате принятых данных или контрольной сумме (Data Error)
        /// </summary>
        DataError = 0x0,

        /// <summary>
        /// ТРК в режиме ожидания: пистолет на месте, авторизации нет (Off)
        /// </summary>
        Off = 0x6,

        /// <summary>
        /// Пистолет снят: ТРК ожидает авторизации от консоли (Call)
        /// </summary>
        Call = 0x7,

        /// <summary>
        /// ТРК авторизована, но подача топлива еще не началась (Authorized/Not Delivering)
        /// </summary>
        AuthorizedNotDelivering = 0x8,

        /// <summary>
        /// Идет активный процесс налива топлива (Busy)
        /// </summary>
        Busy = 0x9,

        /// <summary>
        /// Налив завершен, пистолет повешен: ожидание обработки транзакции (PEOT)
        /// </summary>
        TransactionCompletePeot = 0xA,

        /// <summary>
        /// Налив завершен, пистолет повешен: транзакция зафиксирована (FEOT)
        /// </summary>
        TransactionCompleteFeot = 0xB,

        /// <summary>
        /// ТРК находится в состоянии блокировки после команды Stop (Pump Stop)
        /// </summary>
        PumpStop = 0xC,

        /// <summary>
        /// Подтверждение готовности ТРК к приему расширенного блока данных (Send Data)
        /// </summary>
        /// <remarks>Возвращается только в ответ на команду Data Next</remarks>
        SendData = 0xD
    }

    /// <summary>
    /// Тип топливораздаточной колонки Gilbarco (Unit Type Code),
    /// возвращаемый в ответе Special Function 00E (Miscellaneous Pump Data).
    /// Код состоит из двух десятичных цифр (MSD + LSD).
    /// </summary>
    public enum GilbarcoUnitType : int
    {
        /// <summary>MPD: 1 сорт топлива на сторону.</summary>
        Mpd1Grade = 10,

        /// <summary>MPD: 2 сорта топлива на сторону.</summary>
        Mpd2Grades = 11,

        /// <summary>MPD: 3 сорта топлива на сторону.</summary>
        Mpd3Grades = 12,

        /// <summary>MPD: 4 сорта топлива на сторону.</summary>
        Mpd4Grades = 13,

        /// <summary>MPD: 5 сортов топлива на сторону.</summary>
        Mpd5Grades = 14,

        /// <summary>MPD: 6 сортов топлива на сторону.</summary>
        Mpd6Grades = 15,

        /// <summary>Одношланговый MPD: схема 2+0 (2 сорта, 1 шланг).</summary>
        SingleHoseMpd2_0 = 20,

        /// <summary>Одношланговый MPD: схема 2+1 (3 сорта, 2 шланга).</summary>
        SingleHoseMpd2_1 = 21,

        /// <summary>Одношланговый MPD: схема 3+0 (3 сорта, 1 шланг).</summary>
        SingleHoseMpd3_0 = 22,

        /// <summary>Одношланговый MPD: схема 3+1 (4 сорта, 2 шланга).</summary>
        SingleHoseMpd3_1 = 23,

        /// <summary>Сверхвысокопроизводительная колонка (Super High Flow).</summary>
        SuperHighFlow = 28,

        /// <summary>
        /// Блендер: семейство конфигураций 3+0 или 3+1
        /// (точная схема определяется дополнительными данными).
        /// </summary>
        Blender3_0_Or_3_1 = 30,

        /// <summary>
        /// Блендер (универсальный тип): от 2 до 5 сортов топлива.
        /// </summary>
        BlenderGeneric = 40,

        /// <summary>Блендер конфигурации 2+1+1.</summary>
        Blender2_1_1 = 41,

        /// <summary>Блендер конфигурации 3+1+1.</summary>
        Blender3_1_1 = 42,

        /// <summary>Комбинированная колонка: блендер 2 + MPD 2.</summary>
        Blender2_SingleMpd2 = 43,

        /// <summary>Блендер с двумя манифольдами (2+2).</summary>
        Blender46_48 = 46,

        /// <summary>Блендер с двумя манифольдами (3+2).</summary>
        Blender47_49 = 47
    }

    /// <summary>
    /// Единицы измерения объёма топлива,
    /// возвращаемые в Special Function 00E.
    /// </summary>
    public enum GilbarcoVolumeUnit : int
    {
        /// <summary>Единицы не запрограммированы.</summary>
        NotProgrammed = 0,

        /// <summary>Галлоны США (US Gallons).</summary>
        USGallons = 1,

        /// <summary>Имперские галлоны (UK Gallons).</summary>
        UKGallons = 2,

        /// <summary>Литры.</summary>
        Liters = 3,

        /// <summary>Специальный импульсный режим (используется, например, на Гавайях).</summary>
        HawaiiPulse = 4
    }

    /// <summary>
    /// Денежный режим отображения сумм на колонке Gilbarco.
    /// </summary>
    public enum GilbarcoMoneyMode : int
    {
        /// <summary>Пятизначный денежный режим.</summary>
        FiveDigits = 0,

        /// <summary>Шестизначный денежный режим.</summary>
        SixDigits = 1
    }

    /// <summary>
    /// Режим запуска отпуска топлива.
    /// </summary>
    public enum GilbarcoAutoOnMode : int
    {
        /// <summary>Стандартный режим (без подтверждения кнопкой Start).</summary>
        Standard = 0,

        /// <summary>Режим Push-To-Start (требуется подтверждение кнопкой Start).</summary>
        PushToStart = 1
    }

    /// <summary>
    /// Конфигурация колонки Gilbarco,
    /// получаемая из ответа Special Function 00E (Miscellaneous Pump Data).
    /// </summary>
    public class MiscPumpConfig
    {
        /// <summary>Тип колонки (Unit Type Code).</summary>
        public GilbarcoUnitType UnitType { get; set; }

        /// <summary>Единицы измерения объёма топлива.</summary>
        public GilbarcoVolumeUnit VolumeUnit { get; set; }

        /// <summary>Денежный режим (5 или 6 знаков).</summary>
        public GilbarcoMoneyMode MoneyMode { get; set; }

        /// <summary>Режим Auto-On / Push-To-Start.</summary>
        public GilbarcoAutoOnMode AutoOnMode { get; set; }
    }


}
