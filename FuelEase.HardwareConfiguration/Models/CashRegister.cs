using FuelEase.HardwareConfigurations.Services;
using System.ComponentModel.DataAnnotations;
using System.Xml.Serialization;

namespace FuelEase.HardwareConfigurations.Models
{
    /// <summary>
    /// Кассовый аппарат
    /// </summary>
    [Serializable]
    public class CashRegister : DomainObject, IDevice<CashRegisterType>
    {
        #region Private Members

        private Guid _id = Guid.NewGuid();
        private string _name;
        private CashRegisterType _type;
        private string? _address;
        private string? _registrationNumber;
        private string? _fiscalModuleNumber;
        private string? _userName;
        private string? _password;
        private string? _token;
        private CashRegisterStatus _status;
        private CashRegisterSettings _settings;

        #endregion

        #region Public Properties

        /// <summary>
        /// Идентификатор
        /// </summary>
        [XmlAttribute]
        public Guid Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        /// <summary>
        /// Имя кассового аппарата
        /// </summary>
        [XmlAttribute]
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// Тип кассового аппарата
        /// </summary>
        [XmlAttribute]
        public CashRegisterType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        /// <summary>
        /// url адресс
        /// </summary>
        [XmlAttribute]
        public string? Address
        {
            get => _address;
            set
            {
                _address = value;
                OnPropertyChanged(nameof(Address));
            }
        }

        /// <summary>
        /// Регистрационный номер
        /// </summary>
        [XmlAttribute]
        public string? RegistrationNumber
        {
            get => _registrationNumber;
            set
            {
                _registrationNumber = value;
                OnPropertyChanged(nameof(RegistrationNumber));
            }
        }

        /// <summary>
        /// Номер фискального модуля
        /// </summary>
        [XmlAttribute]
        public string? FiscalModulNumber
        {
            get => _fiscalModuleNumber;
            set
            {
                _fiscalModuleNumber = value;
                OnPropertyChanged(nameof(FiscalModulNumber));
            }
        }

        /// <summary>
        /// Имя пользователя
        /// </summary>
        [XmlAttribute]
        public string? UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged(nameof(UserName));
            }
        }

        /// <summary>
        /// Пароль
        /// </summary>
        [XmlAttribute]
        public string? Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged(nameof(Password));
            }
        }

        /// <summary>
        /// Токен
        /// </summary>
        [XmlIgnore]
        public string? Token
        {
            get => _token;
            set
            {
                _token = value;
                OnPropertyChanged(nameof(Token));
            }
        }

        /// <summary>
        /// Статус
        /// </summary>
        [XmlIgnore]
        public CashRegisterStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>
        /// Настройки
        /// </summary>
        public CashRegisterSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged(nameof(Settings));
            }
        }

        /// <summary>
        /// Присваивает свойства объекта
        /// </summary>
        public override void Update(DomainObject updatedItem)
        {
            if (updatedItem is CashRegister cashRegister)
            {
                Name = cashRegister.Name;
                Type = cashRegister.Type;
                Address = cashRegister.Address;
                RegistrationNumber = cashRegister.RegistrationNumber;
                FiscalModulNumber = cashRegister.FiscalModulNumber;
                UserName = cashRegister.UserName;
                Password = cashRegister.Password;
                Settings = cashRegister.Settings;
            }
        }

        #endregion
    }

    /// <summary>
    /// Тип кассового аппарата
    /// </summary>
    public enum CashRegisterType
    {
        [Display(Name = "Не выбран")]
        None,

        [Display(Name = "eKassa")]
        EKassa,

        [Display(Name = "ОК МФ")]
        MF,

        [Display(Name = "New Cas")]
        NewCas
    }

    /// <summary>
    /// Статус кассового аппарата
    /// </summary>
    public enum CashRegisterStatus
    {
        /// <summary>
        /// Неизвестный статус кассы
        /// </summary>
        Unknown,

        /// <summary>
        /// Смена открыта
        /// </summary>
        Open,

        /// <summary>
        /// Смена закрыта
        /// </summary>
        Close,

        /// <summary>
        /// Смена открыта более 24 часов
        /// </summary>
        Exceeded24Hours,

        /// <summary>
        /// Ошибка при взаимодействии с ККМ
        /// </summary>
        Error,

        /// <summary>
        /// Нет открытой смены
        /// </summary>
        NoOpenedShift
    }

}
