using System.ComponentModel.DataAnnotations;

namespace FuelEase.Domain.Models
{
    /// <summary>
    /// Продажи топлива
    /// </summary>
    public class FuelSale : DomainObject
    {
        #region Private Members

        private int _tankId;
        private int _shiftId;
        private int _nozzleId;
        private int? _discountSaleId;
        private int? _fiscalDataId;
        private PaymentType _paymentType;
        private DateTime _createDate;
        private decimal _price;
        private decimal _sum;
        private decimal _receivedSum;
        private decimal _receivedQuantity;
        private decimal _receivedCount;
        private decimal _quantity;
        private decimal? _customerSum;
        private decimal? _changeSum;
        private FuelSaleStatus _fuelSaleStatus;
        private bool _isForSum;
        
        #endregion

        #region Public Properties

        /// <summary>
        /// Id резервуара
        /// </summary>
        public int TankId
        {
            get => _tankId;
            set
            {
                _tankId = value;
                OnPropertyChanged(nameof(TankId));
            }
        }

        /// <summary>
        /// Id ТРК
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
        /// Id скидки
        /// </summary>
        public int? DiscountSaleId
        {
            get => _discountSaleId;
            set
            {
                _discountSaleId = value;
                OnPropertyChanged(nameof(DiscountSaleId));
            }
        }

        /// <summary>
        /// Id фискальных данных
        /// </summary>
        public int? FiscalDataId
        {
            get => _fiscalDataId;
            set
            {
                _fiscalDataId = value;
                OnPropertyChanged(nameof(FiscalDataId));
            }
        }

        /// <summary>
        /// Тип оплаты
        /// </summary>
        public PaymentType PaymentType
        {
            get => _paymentType;
            set
            {
                _paymentType = value;
                OnPropertyChanged(nameof(PaymentType));
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
        /// Цена
        /// </summary>
        public decimal Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
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
        /// Полученная сумма
        /// </summary>
        public decimal ReceivedSum
        {
            get => _receivedSum;
            set
            {
                _receivedSum = value;
                OnPropertyChanged(nameof(ReceivedSum));
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
        /// Полученная количество топлива
        /// </summary>
        public decimal ReceivedQuantity
        {
            get => _receivedQuantity;
            set
            {
                _receivedQuantity = value;
                OnPropertyChanged(nameof(ReceivedQuantity));
            }
        }

        /// <summary>
        /// Полученная счетчик показаний на конец
        /// </summary>
        public decimal ReceivedCount
        {
            get => _receivedCount;
            set
            {
                _receivedCount = value;
                OnPropertyChanged(nameof(ReceivedCount));
            }
        }

        /// <summary>
        /// Сумма клиента
        /// </summary>
        public decimal? CustomerSum
        {
            get => _customerSum;
            set
            {
                _customerSum = value;
                OnPropertyChanged(nameof(CustomerSum));
            }
        }

        /// <summary>
        /// Сдача
        /// </summary>
        public decimal? ChangeSum
        {
            get => _changeSum;
            set
            {
                _changeSum = value;
                OnPropertyChanged(nameof(ChangeSum));
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

        /// <summary>
        /// Залитие на сумму
        /// </summary>
        public bool IsForSum
        {
            get => _isForSum;
            set
            {
                _isForSum = value;
                OnPropertyChanged(nameof(IsForSum));
            }
        }

        /// <summary>
        /// Статус продажи
        /// </summary>
        [EnumDataType(typeof(FuelSaleStatus))]
        public FuelSaleStatus FuelSaleStatus
        {
            get => _fuelSaleStatus;
            set
            {
                _fuelSaleStatus = value;
                OnPropertyChanged(nameof(FuelSaleStatus));
            }
        }

        /// <summary>
        /// Резервуар
        /// </summary>
        public Tank? Tank { get; set; }
        
        /// <summary>
        /// Смена
        /// </summary>
        public Shift? Shift { get; set; }

        /// <summary>
        /// Скидки
        /// </summary>
        public DiscountSale? DiscountSale { get; set; }

        /// <summary>
        /// Фискальные данные
        /// </summary>
        public FiscalData? FiscalData { get; set; }

        public Nozzle? Nozzle { get; set; }

        #endregion

        public override void Update(DomainObject updatedItem)
        {
            if (updatedItem is FuelSale fuelSale)
            {
                PaymentType = fuelSale.PaymentType;
                Sum = fuelSale.Sum;
                Quantity = fuelSale.Quantity;
                ReceivedSum = fuelSale.ReceivedSum;
                ReceivedQuantity = fuelSale.ReceivedQuantity;
                FuelSaleStatus = fuelSale.FuelSaleStatus;

            }
        }
    }

    /// <summary>
    /// Статус продажи
    /// </summary>
    public enum FuelSaleStatus
    {
        None,

        /// <summary>
        /// В процессе
        /// </summary>
        [Display(Name = "В процессе")]
        InProgress,

        /// <summary>
        /// Завершенный
        /// </summary>
        [Display(Name = "Завершенный")]
        Completed,

        /// <summary>
        /// Незавершенный
        /// </summary>
        [Display(Name = "Незавершенный")]
        Uncompleted
    }

    /// <summary>
    /// Тип оплаты
    /// </summary>
    public enum PaymentType
    {
        None,

        /// <summary>
        /// Наличными
        /// </summary>
        [Display(Name = "Наличными")]
        Cash,

        /// <summary>
        /// Безналичными
        /// </summary>
        [Display(Name = "Безналичными")]
        Cashless,

        /// <summary>
        /// Ведомость
        /// </summary>
        [Display(Name = "Ведомость")]
        Statement,

        /// <summary>
        /// Талон
        /// </summary>
        [Display(Name = "Талон")]
        Ticket
    }
}
