using System.ComponentModel.DataAnnotations.Schema;

namespace KIT.GasStation.Domain.Models.CashRegisters
{
    public class FiscalData : DomainObject
    {
        #region Private Members

        private int? _fiscalDocument;
        private string? _fiscalModule;
        private string? _check;
        private string? _registrationNumber;
        private int _fuelSaleId;
        private OperationType _operationType;
        private PaymentType _paymentType;
        private decimal _price;
        private decimal _quantity;
        private decimal _total;
        private string? _sourceFiscalData;
        private string _fuelName = string.Empty;
        private string _unitOfMeasurement = string.Empty;
        private bool _valueAddedTax;
        private string? _tnved;
        private decimal _salesTax;

        #endregion

        #region Public Properties

        /// <summary>
        /// Фискальный документ ФД
        /// </summary>
        public int? FiscalDocument
        {
            get => _fiscalDocument;
            set
            {
                _fiscalDocument = value;
                OnPropertyChanged(nameof(FiscalDocument));
            }
        }

        /// <summary>
        /// Фискальный модуль чека ФМ
        /// </summary>
        public string? FiscalModule
        {
            get => _fiscalModule;
            set
            {
                _fiscalModule = value;
                OnPropertyChanged(nameof(FiscalModule));
            }
        }

        /// <summary>
        /// Чек продажи
        /// </summary>
        public string? Check
        {
            get => _check;
            set
            {
                _check = value;
                OnPropertyChanged(nameof(Check));
            }
        }

        /// <summary>
        /// Опциональный параметр, исходный номер ФД
        /// </summary>
        public string? SourceFiscalData
        {
            get => _sourceFiscalData;
            set
            {
                _sourceFiscalData = value;
                OnPropertyChanged(nameof(SourceFiscalData));
            }
        }

        /// <summary>
        /// Регистрационный номер ККМ
        /// </summary>
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
        /// Наименование топлива
        /// </summary>
        public string FuelName
        {
            get => _fuelName;
            set
            {
                _fuelName = value;
                OnPropertyChanged(nameof(FuelName));
            }
        }

        /// <summary>
        /// Единица измерения
        /// </summary>
        public string UnitOfMeasurement
        {
            get => _unitOfMeasurement;
            set
            {
                _unitOfMeasurement = value;
                OnPropertyChanged(nameof(UnitOfMeasurement));
            }
        }

        /// <summary>
        /// Наличие НДС
        /// </summary>
        public bool ValueAddedTax
        {
            get => _valueAddedTax;
            set
            {
                _valueAddedTax = value;
                OnPropertyChanged(nameof(ValueAddedTax));
            }
        }

        /// <summary>
        /// Налоговая ставка
        /// </summary>
        public decimal SalesTax
        {
            get => _salesTax;
            set
            {
                _salesTax = value;
                OnPropertyChanged(nameof(SalesTax));
            }
        }

        /// <summary>
        /// ТНВЭД - товарная номенклатура внешнеэкономической деятельности
        /// </summary>
        public string? Tnved
        {
            get => _tnved;
            set
            {
                _tnved = value;
                OnPropertyChanged(nameof(Tnved));
            }
        }

        /// <summary>
        /// Тип данных для фискальных данных
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
        /// Идентификатор продажи топлива
        /// </summary>
        public int FuelSaleId
        {
            get => _fuelSaleId;
            set
            {
                _fuelSaleId = value;
                OnPropertyChanged(nameof(FuelSaleId));
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
        /// Итоговая сумма
        /// </summary>
        public decimal Total
        {
            get => _total;
            set
            {
                _total = value;
                OnPropertyChanged(nameof(Total));
            }
        }

        [NotMapped]
        public decimal TotalToPay
        {
            get
            {
                var discount = Discount?.Amount ?? 0m;
                var result = Total - discount;
                return result < 0 ? 0 : result;
            }
        }

        /// <summary>
        /// Продажа топлива
        /// </summary>
        public FuelSale? FuelSale { get; set; } = null!;
        public FiscalDiscount? Discount { get; set; }

        #endregion

        #region Public Voids

        public override void Update(DomainObject updatedItem)
        {
            throw new NotImplementedException();
        }

        public FiscalData? UpdatedFiscalData(FiscalData? fiscalData)
        {
            if (fiscalData is not null)
            {
                FiscalModule = fiscalData.FiscalModule;
                FiscalDocument = fiscalData.FiscalDocument;
                RegistrationNumber = fiscalData.RegistrationNumber;
                Check = fiscalData.Check;
            }

            return this;
        }

        public FiscalData CreateReturnFiscalData()
        {
            return new FiscalData
            {
                FiscalDocument = FiscalDocument,
                FiscalModule = FiscalModule,
                RegistrationNumber = RegistrationNumber,
                SourceFiscalData = FiscalDocument.ToString(),
                FuelName = FuelName,
                UnitOfMeasurement = UnitOfMeasurement,
                ValueAddedTax = ValueAddedTax,
                SalesTax = SalesTax,
                Tnved = Tnved,
                OperationType = OperationType.Return,
                PaymentType = PaymentType,
                FuelSaleId = FuelSaleId,
                Price = Price,
                Quantity = Quantity,
                Total = Total,
                Discount = Discount,
            };
        }

        #endregion
    }
}
