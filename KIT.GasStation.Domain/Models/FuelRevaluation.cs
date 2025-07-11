namespace KIT.GasStation.Domain.Models
{
    public class FuelRevaluation : DomainObject
    {
        #region Private Members

        private DateTime _createdDate;
        private int _fuelId;
        private decimal _newPrice;
        private decimal _oldPrice;

        #endregion

        #region Public Properties

        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                _createdDate = value;
                OnPropertyChanged(nameof(CreatedDate));
            }
        }

        public int FuelId
        {
            get => _fuelId;
            set
            {
                _fuelId = value;
                OnPropertyChanged(nameof(FuelId));
            }
        }

        public decimal NewPrice
        {
            get => _newPrice;
            set
            {
                _newPrice = value;
                OnPropertyChanged(nameof(NewPrice));
            }
        }

        public decimal OldPrice
        {
            get => _oldPrice;
            set
            {
                _oldPrice = value;
                OnPropertyChanged(nameof(OldPrice));
            }
        }

        public Fuel Fuel { get; set; }

        #endregion

        public override void Update(DomainObject updatedItem)
        {

        }
    }
}
