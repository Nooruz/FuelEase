namespace FuelEase.Domain.Models
{
    /// <summary>
    /// Прием топлива
    /// </summary>
    public class FuelIntake : DomainObject
    {
        #region Private Members

        private DateTime _createDate;
        private string? _number;
        private int _tankId;
        private int _shiftId;
        private decimal _quantity;

        #endregion

        #region Public Properties

        public DateTime CreateDate
        {
            get => _createDate;
            set
            {
                _createDate = value;
                OnPropertyChanged(nameof(CreateDate));
            }
        }
        public string? Number
        {
            get => _number;
            set
            {
                _number = value;
                OnPropertyChanged(nameof(Number));
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
        public int ShiftId
        {
            get => _shiftId;
            set
            {
                _shiftId = value;
                OnPropertyChanged(nameof(ShiftId));
            }
        }
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }
        public Tank Tank { get; set; }
        public Shift Shift { get; set; }

        public override void Update(DomainObject updatedItem)
        {

        }

        #endregion
    }
}
