namespace FuelEase.Domain.Models
{
    public class UnitOfMeasurement : DomainObject
    {
        #region Private Members

        private string _name;

        #endregion

        #region Public Properties

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public ICollection<Fuel> Fuels { get; set; }

        #endregion

        public override void Update(DomainObject updatedItem)
        {

        }
    }
}
