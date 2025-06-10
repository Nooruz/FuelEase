namespace FuelEase.Domain.Models
{
    /// <summary>
    /// Счетчики резервуара по смене
    /// </summary>
    public class TankShiftCounter : DomainObject
    {
        #region Private Members

        private int _shiftId;
        private int _tankId;
        private decimal _beginCount;
        private decimal _endCount;

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

        public int TankId
        {
            get => _tankId;
            set
            {
                _tankId = value;
                OnPropertyChanged(nameof(TankId));
            }
        }

        public decimal BeginCount
        {
            get => _beginCount;
            set
            {
                _beginCount = value;
                OnPropertyChanged(nameof(BeginCount));
            }
        }

        public decimal EndCount
        {
            get => _endCount;
            set
            {
                _endCount = value;
                OnPropertyChanged(nameof(EndCount));
            }
        }

        public Shift Shift { get; set; }
        public Tank Tank { get; set; }

        #endregion

        public override void Update(DomainObject updatedItem)
        {
            throw new NotImplementedException();
        }
    }
}
