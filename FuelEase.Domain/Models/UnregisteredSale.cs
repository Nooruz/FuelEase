using System.ComponentModel.DataAnnotations;

namespace FuelEase.Domain.Models
{
    /// <summary>
    /// Незарегистрированная продажа
    /// </summary>
    [Display(Name = "Незарегистрированная продажа")]
    public class UnregisteredSale : DomainObject
    {
        #region Private Members

        private int _nozzleId;
        private int _shiftId;
        private DateTime _createDate;
        private decimal _sum;
        private decimal _quantity;
        private UnregisteredSaleState _state;

        #endregion

        #region Public Properties

        /// <summary>
        /// Id пистолета
        /// </summary>
        public int NozzleId
        {
            get => _nozzleId;
            set
            {
                _nozzleId = value;
                OnPropertyChanged(nameof(NozzleId));
            }
        }

        /// <summary>
        /// Дата создании чека
        /// </summary>
        public DateTime CreateDate
        {
            get => _createDate;
            set
            {
                _createDate = value;
                OnPropertyChanged(nameof(CreateDate));
            }
        }

        /// <summary>
        /// Сумма
        /// </summary>
        public decimal Sum
        {
            get => _sum;
            set
            {
                _sum = value;
                OnPropertyChanged(nameof(Sum));
            }
        }

        /// <summary>
        /// Количество
        /// </summary>
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }

        /// <summary>
        /// Id смены
        /// </summary>
        public int ShiftId
        {
            get => _shiftId;
            set
            {
                _shiftId = value;
                OnPropertyChanged(nameof(ShiftId));
            }
        }

        [EnumDataType(typeof(UnregisteredSaleState))]
        public UnregisteredSaleState State
        {
            get => _state;
            set
            {
                _state = value;
                OnPropertyChanged(nameof(State));
            }
        }

        public Nozzle Nozzle { get; set; }

        public Shift Shift { get; set; }

        #endregion

        public override void Update(DomainObject updatedItem)
        {
            if (updatedItem is UnregisteredSale unregisteredSale)
            {
                NozzleId = unregisteredSale.NozzleId;
                CreateDate = unregisteredSale.CreateDate;
                Sum = unregisteredSale.Sum;
                Quantity = unregisteredSale.Quantity;
                ShiftId = unregisteredSale.ShiftId;
                State = unregisteredSale.State;
                Nozzle = unregisteredSale.Nozzle;
                Shift = unregisteredSale.Shift;
            }
        }
    }

    /// <summary>
    /// Состояние незарегистрированной продажи
    /// </summary>
    public enum UnregisteredSaleState
    {
        None,

        /// <summary>
        /// Ожидание
        /// </summary>
        [Display(Name = "Ожидание")]
        Waiting,

        /// <summary>
        /// Зарегистрировано как продажа
        /// </summary>
        [Display(Name = "Зарегистрировано как продажа")]
        Registered,

        /// <summary>
        /// Зарегистрировано как обратка (перекачка)
        /// </summary>
        [Display(Name = "Зарегистрировано как обратка (перекачка)")]
        Returned,

        /// <summary>
        /// Удалено
        /// </summary>
        [Display(Name = "Удалено")]
        Deleted
    }
}
