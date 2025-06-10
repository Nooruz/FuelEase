namespace FuelEase.Domain.Models
{
    public class FiscalData : DomainObject
    {
        #region Private Members

        private int? _fiscalDocument;
        private string? _fiscalModule;
        private string? _check;
        private string? _returnCheck;
        private string? _registrationNumber;

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
        /// Чек возврата
        /// </summary>
        public string? ReturnCheck
        {
            get => _returnCheck;
            set
            {
                _returnCheck = value;
                OnPropertyChanged(nameof(ReturnCheck));
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
        /// Продажа топлива
        /// </summary>
        public FuelSale FuelSale { get; set; }

        #endregion

        public override void Update(DomainObject updatedItem)
        {
            throw new NotImplementedException();
        }
    }
}
