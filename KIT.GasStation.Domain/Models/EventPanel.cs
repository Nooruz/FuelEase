using System.ComponentModel.DataAnnotations;

namespace KIT.GasStation.Domain.Models
{
    /// <summary>
    /// Панель события
    /// </summary>
    public class EventPanel : DomainObject
    {
        #region Private Members

        private string _message;
        private DateTime _createdDate;
        private EventPanelType _type;
        private EventEntity _eventEntity;
        private int _shiftId;
        private int _entityId;

        #endregion

        #region Public Properties

        /// <summary>
        /// Информация
        /// </summary>
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }
        
        /// <summary>
        /// Дата создания
        /// </summary>
        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                _createdDate = value;
                OnPropertyChanged(nameof(CreatedDate));
            }
        }

        public int ShiftId
        {
            get => _shiftId;
            set
            {
                _shiftId = value;
                OnPropertyChanged(nameof(ShiftId));
            }
        }

        /// <summary>
        /// Тип информации
        /// </summary>
        public EventPanelType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        public EventEntity EventEntity
        {
            get => _eventEntity;
            set
            {
                _eventEntity = value;
                OnPropertyChanged(nameof(EventEntity));
            }
        }

        public int EntityId
        {
            get => _entityId;
            set
            {
                _entityId = value;
                OnPropertyChanged(nameof(EntityId));
            }
        }

        public Shift Shift { get; set; }

        public override void Update(DomainObject updatedItem)
        {

        }

        #endregion
    }

    public enum EventPanelType
    {
        None,

        [Display(Name = "Информация")]
        Information,

        [Display(Name = "Ошибка")]
        Error
    }

    public enum EventEntity
    {
        Shift,

        CashRegister,

        FuelSale,

        Fuel,

        Nozzle,

        Tank,

        UnregisteredSale,

        User,

        Discount
    }
}
