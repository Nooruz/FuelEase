namespace KIT.GasStation.Domain.Models
{
    /// <summary>
    /// Счетчики ТРК по смене
    /// </summary>
    public class ShiftCounter : DomainObject
    {
        #region Private Members

        private int _shiftId;
        private int _nozzleId;
        private decimal _beginNozzleCounter;
        private decimal _endNozzleCountCounter;
        private decimal _beginSaleCounter;
        private decimal _endSaleCounter;

        #endregion

        #region Public Properties

        public int ShiftId
        {
            get => _shiftId;
            set
            {
                _shiftId = value;
                OnPropertyChanged(nameof(ShiftId));
            }
        }

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
        /// Счетчик на начало ТРК
        /// </summary>
        public decimal BeginNozzleCounter
        {
            get => _beginNozzleCounter;
            set
            {
                _beginNozzleCounter = value;
                OnPropertyChanged(nameof(BeginNozzleCounter));
            }
        }

        /// <summary>
        /// Счетчик на конец ТРК
        /// </summary>
        public decimal EndNozzleCounter
        {
            get => _endNozzleCountCounter;
            set
            {
                _endNozzleCountCounter = value;
                OnPropertyChanged(nameof(EndNozzleCounter));
            }
        }

        /// <summary>
        /// Счетчик на начало СУ
        /// </summary>
        public decimal BeginSaleCounter
        {
            get => _beginSaleCounter;
            set
            {
                _beginSaleCounter = value;
                OnPropertyChanged(nameof(BeginSaleCounter));
            }
        }

        /// <summary>
        /// Счетчик на конец СУ
        /// </summary>
        public decimal EndSaleCounter
        {
            get => _endSaleCounter;
            set
            {
                _endSaleCounter = value;
                OnPropertyChanged(nameof(EndSaleCounter));
            }
        }

        public Shift Shift { get; set; }
        public Nozzle Nozzle { get; set; }

        #endregion

        public override void Update(DomainObject updatedItem)
        {
            throw new NotImplementedException();
        }
    }
}
