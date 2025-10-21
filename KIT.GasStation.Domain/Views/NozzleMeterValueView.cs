using System.ComponentModel.DataAnnotations.Schema;

namespace KIT.GasStation.Domain.Views
{
    public class NozzleMeterValueView : ViewObject
    {
        #region Private Members

        private int _id;
        private int _tankId;
        private string _name;
        private decimal _quantity;
        private decimal controllerMeter;
        private decimal _balance;

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

        public int TankId
        {
            get => _tankId;
            set
            {
                _tankId = value;
                OnPropertyChanged(nameof(TankId));
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
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

        [NotMapped]
        public decimal ControllerMeter
        {
            get => controllerMeter; 
            set
            {
                controllerMeter = value;
                OnPropertyChanged(nameof(ControllerMeter));
                Balance = ControllerMeter - Quantity;
            }
        }

        [NotMapped]
        public decimal Balance
        {
            get => _balance;
            set
            {
                _balance = value;
                OnPropertyChanged(nameof(Balance));
            }
        }

        #endregion
    }
}
