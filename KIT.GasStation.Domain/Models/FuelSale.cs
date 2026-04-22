using KIT.GasStation.Domain.Models.CashRegisters;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KIT.GasStation.Domain.Models
{
    /// <summary>
    /// Продажи топлива
    /// </summary>
    public class FuelSale : DomainObject
    {
        #region Private Members

        private int _number;
        private int _tankId;
        private int _shiftId;
        private int _nozzleId;
        private PaymentType _paymentType;
        private OperationType _operationType;
        private DateTime _createDate;
        private decimal _price;
        private decimal _sum;
        private decimal _receivedSum;
        private decimal _resumeBaseSum;
        private decimal _receivedQuantity;
        private decimal _resumeBaseQuantity;
        private decimal _receivedCount;
        private decimal _quantity;
        private decimal? _changeSum;
        private FuelSaleStatus _fuelSaleStatus;
        private bool _isForSum;
        private ObservableCollection<FiscalData> _fiscalDatas = new();

        #endregion

        #region Public Properties

        /// <summary>
        /// Порядковый номер продажи за день (сбрасывается ежедневно, как в 1С).
        /// </summary>
        public int Number
        {
            get => _number;
            set
            {
                _number = value;
                OnPropertyChanged(nameof(Number));
            }
        }

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
        /// Тип операции
        /// </summary>
        public OperationType OperationType
        {
            get => _operationType;
            set
            {
                _operationType = value;
                OnPropertyChanged(nameof(OperationType));
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
        /// База фактически отпущенной суммы до продолжения.
        /// Используется для корректного расчёта общей суммы,
        /// так как новая сессия ТРК начинается с 0.
        /// </summary>
        [NotMapped]
        public decimal ResumeBaseSum
        {
            get => _resumeBaseSum;
            set
            {
                _resumeBaseSum = value;
                OnPropertyChanged(nameof(ResumeBaseSum));
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
                OnPropertyChanged(nameof(ReceivedPercentage));
            }
        }

        /// <summary>
        /// База фактически отпущенной литры до продолжения.
        /// Используется для корректного расчёта общей литры,
        /// так как новая сессия ТРК начинается с 0.
        /// </summary>
        [NotMapped]
        public decimal ResumeBaseQuantity
        {
            get => _resumeBaseQuantity;
            set
            {
                _resumeBaseQuantity = value;
                OnPropertyChanged(nameof(ResumeBaseQuantity));
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
        public ObservableCollection<FiscalData> FiscalDatas
        {
            get => _fiscalDatas;
            set
            {
                _fiscalDatas = value;
                OnPropertyChanged(nameof(FiscalDatas));
            }
        }

        public Nozzle? Nozzle { get; set; }

        [NotMapped]
        public string ReceivedPercentage => Quantity > 0 ? $"{(ReceivedQuantity / Quantity * 100):0}%" : "0%";

        [NotMapped]
        public bool IsProblematic => IsSaleProblematic();

        /// <summary>
        /// Общая сумма по всем фискальным операциям продажи (без учета возвратов).
        /// Отражает сумму всех чеков типа "Продажа" по данной операции.
        /// </summary>
        [NotMapped]
        public decimal TotalFiscalSale => FiscalDatas.Where(x => x.OperationType == OperationType.Sale).Sum(x => x.Total);

        /// <summary>
        /// Общая сумма по всем фискальным операциям возврата.
        /// Отражает сумму всех чеков типа "Возврат" по данной операции.
        /// </summary>
        [NotMapped]
        public decimal TotalFiscalReturn => FiscalDatas.Where(x => x.OperationType == OperationType.Return).Sum(x => x.Total);

        /// <summary>
        /// Итоговая сумма по фискальным операциям (продажи минус возвраты).
        /// Отражает фактическую сумму, прошедшую через ККМ по данной продаже.
        /// </summary>
        [NotMapped]
        public decimal TotalFiscalNet => TotalFiscalSale - TotalFiscalReturn;

        [NotMapped]
        public string PrintReceivedSaleReceiptTitle
        {
            get
            {
                if (IsVisiblePrintReceivedSaleReceipt)
                {
                    return $"Создать чек на {ReceivedSum:N2} сом";
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        [NotMapped]
        public bool IsVisiblePrintReceivedSaleReceipt
        {
            get
            {
                if (ReceivedSum <= 0)
                    return false;

                return Sum > TotalFiscalNet;
            }
        }

        #endregion

        /// <summary>
        /// Обновляет текущий объект данными из другого объекта того же типа.
        /// </summary>
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

        /// <summary>
        /// Получает полную копию текущего объекта, включая все его свойства, но без навигационных свойств.
        /// </summary>
        public FuelSale Clone()
        {
            return new FuelSale
            {
                Number = Number,
                TankId = TankId,
                NozzleId = NozzleId,
                PaymentType = PaymentType,
                OperationType = OperationType,
                CreateDate = CreateDate,
                Price = Price,
                Sum = Sum,
                ReceivedSum = ReceivedSum,
                Quantity = Quantity,
                ReceivedQuantity = ReceivedQuantity,
                ReceivedCount = ReceivedCount,
                ChangeSum = ChangeSum,
                ShiftId = ShiftId,
                IsForSum = IsForSum,
                FuelSaleStatus = FuelSaleStatus,

                // Навигационные свойства ставим NULL,
                // чтобы Entity Framework не пытался трекать их
                Tank = null,
                Shift = null,
                DiscountSale = null,
                FiscalDatas = null,
                Nozzle = null
            };
        }

        /// <summary>
        /// Создать фискальные данные на основе текущей продажи топлива.
        /// </summary>
        public FiscalData AfterCreateFiscalData(OperationType type)
        {
            var fiscalData = new FiscalData
            {
                OperationType = type,
                PaymentType = PaymentType,
                Price = Price,
                Quantity = ReceivedQuantity,
                Total = ReceivedSum,
                FuelSaleId = Id
            };

            if (Tank != null && Tank.Fuel != null)
            {
                fiscalData.UnitOfMeasurement = Tank.Fuel.UnitOfMeasurement?.Name;
                fiscalData.ValueAddedTax = Tank.Fuel.ValueAddedTax;
                fiscalData.SalesTax = Tank.Fuel.SalesTax;
                fiscalData.FuelName = Tank.Fuel.Name;
                fiscalData.Tnved = Tank.Fuel.TNVED;
            }

            return fiscalData;
        }

        public FiscalData CreateFiscalData()
        {
            var fiscalData = new FiscalData
            {
                OperationType = OperationType.Sale,
                PaymentType = PaymentType,
                Price = Price,
                Quantity = Quantity,
                Total = Sum,
                FuelSaleId = Id,
            };

            if (Tank != null && Tank.Fuel != null)
            {
                fiscalData.UnitOfMeasurement = Tank.Fuel.UnitOfMeasurement?.Name;
                fiscalData.ValueAddedTax = Tank.Fuel.ValueAddedTax;
                fiscalData.SalesTax = Tank.Fuel.SalesTax;
                fiscalData.FuelName = Tank.Fuel.Name;
                fiscalData.Tnved = Tank.Fuel.TNVED;
            }

            return fiscalData;
        }

        public FiscalData CreateReceivedFiscalData()
        {
            var fiscalData = new FiscalData
            {
                OperationType = OperationType.Sale,
                PaymentType = PaymentType,
                Price = Price,
                Quantity = ReceivedQuantity,
                Total = ReceivedSum,
                FuelSaleId = Id
            };

            if (Tank != null && Tank.Fuel != null)
            {
                fiscalData.UnitOfMeasurement = Tank.Fuel.UnitOfMeasurement?.Name;
                fiscalData.ValueAddedTax = Tank.Fuel.ValueAddedTax;
                fiscalData.SalesTax = Tank.Fuel.SalesTax;
                fiscalData.FuelName = Tank.Fuel.Name;
                fiscalData.Tnved = Tank.Fuel.TNVED;
            }

            return fiscalData;
        }

        public FiscalData CreateReturnFiscalData(Nozzle nozzle, FiscalData originalFiscalData)
        {
            var fiscalData = new FiscalData
            {
                FiscalDocument = originalFiscalData.FiscalDocument,
                FiscalModule = originalFiscalData.FiscalModule,
                OperationType = OperationType.Return,
                PaymentType = PaymentType,
                Price = Price,
                Quantity = Quantity,
                Total = Sum,
                FuelSaleId = Id
            };

            if (nozzle.Tank != null && nozzle.Tank.Fuel != null)
            {
                fiscalData.UnitOfMeasurement = nozzle.Tank.Fuel.UnitOfMeasurement?.Name;
                fiscalData.ValueAddedTax = nozzle.Tank.Fuel.ValueAddedTax;
                fiscalData.SalesTax = nozzle.Tank.Fuel.SalesTax;
                fiscalData.FuelName = nozzle.Tank.Fuel.Name;
                fiscalData.Tnved = Tank.Fuel.TNVED;
            }

            return fiscalData;
        }

        public FiscalData UpdateFiscalData(FiscalData fiscalData, Nozzle nozzle)
        {
            if (nozzle.Tank != null && nozzle.Tank.Fuel != null)
            {
                fiscalData.OperationType = fiscalData.OperationType;
                fiscalData.PaymentType = fiscalData.PaymentType;
                fiscalData.Price = fiscalData.Price;
                fiscalData.Quantity = fiscalData.Quantity;
                fiscalData.Total = fiscalData.Total;
                fiscalData.FuelSaleId = Id;
                fiscalData.UnitOfMeasurement = nozzle.Tank.Fuel.UnitOfMeasurement?.Name;
                fiscalData.ValueAddedTax = nozzle.Tank.Fuel.ValueAddedTax;
                fiscalData.SalesTax = nozzle.Tank.Fuel.SalesTax;
                fiscalData.FuelName = nozzle.Tank.Fuel.Name;
                fiscalData.Tnved = nozzle.Tank.Fuel.TNVED;
            }

            return fiscalData;
        }

        private bool IsSaleProblematic()
        {
            // если нет отпуска — вообще не трогаем
            if (ReceivedSum <= 0)
                return false;

            // сравниваем факт отпуска с тем, что прошло через ККМ
            return TotalFiscalNet != ReceivedSum;
        }
    }

    /// <summary>
    /// Статус продажи
    /// </summary>
    public enum FuelSaleStatus
    {
        None,

        /// <summary>
        /// Обрабатывается
        /// </summary>
        [Display(Name = "Обрабатывается")]
        InProcessed,

        /// <summary>
        /// Обработанный
        /// </summary>
        [Display(Name = "Обработана")]
        Processed,

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
        Ticket,

        ///// <summary>
        ///// Дисконтная карта
        ///// </summary>
        //[Display(Name = "Дисконтная карта")]
        //DiscountCard,

        ///// <summary>
        ///// Топливная карта
        ///// </summary>
        //[Display(Name = "Топливная карта")]
        //FuelCard,

        ///// <summary>
        ///// Другое
        ///// </summary>
        //[Display(Name = "Другое")]
        //Other
    }

    public enum OperationType
    {
        /// <summary>
        /// Продажа
        /// </summary>
        [Display(Name = "Продажа")]
        Sale,

        /// <summary>
        /// Возврат
        /// </summary>
        [Display(Name = "Возврат")]
        Return
    }
}