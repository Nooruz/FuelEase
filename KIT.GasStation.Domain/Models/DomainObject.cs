using System.ComponentModel;

namespace KIT.GasStation.Domain.Models
{
    public abstract class DomainObject : INotifyPropertyChanged
    {
        #region Private Members

        private int _id;

        #endregion

        #region Public Properties

        public int Id
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged(nameof(Id));
            }
        }

        #endregion

        #region PropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override bool Equals(object? obj)
        {
            if (obj is DomainObject domainObject)
            {
                return Id == domainObject.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion

        #region Abstracts

        public abstract void Update(DomainObject updatedItem);

        #endregion
    }
}
