namespace KIT.GasStation.Domain.Models.CashRegisters
{
    /// <summary>
    /// Фискальная скидка, применяемая к продаже топлива на ККМ. Содержит информацию о названии скидки и ее сумме. Используется для учета скидок при формировании фискальных данных и отчетов.
    /// </summary>
    public sealed class FiscalDiscount : DomainObject
    {
        #region Private Members

        private string _title = "Скидка";
        private decimal _amount;

        #endregion

        #region Public Properties

        /// <summary>
        /// Название скидки, применяемой к продаже топлива. Это может быть, например, "Скидка по акции" или "Скидка для постоянных клиентов". Название используется для идентификации типа скидки при формировании фискальных данных и отчетов.
        /// </summary>
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        /// <summary>
        /// Сумма скидки, применяемой к продаже топлива. Это числовое значение, которое вычитается из общей суммы продажи при формировании фискальных данных. Сумма скидки может быть указана в денежном выражении или в процентах от общей суммы, в зависимости от типа скидки.
        /// </summary>
        public decimal Amount
        {
            get => _amount;
            set
            {
                _amount = value;
                OnPropertyChanged(nameof(Amount));
            }
        }

        public int FiscalDataId { get; set; }
        public FiscalData FiscalData { get; set; } = null!;

        #endregion

        public override void Update(DomainObject updatedItem)
        {
            throw new NotImplementedException();
        }
    }
}
