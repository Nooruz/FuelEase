using System.ComponentModel.DataAnnotations.Schema;

namespace FuelEase.Domain.Views
{
    public class NozzleMeterValueView : ViewObject
    {
        #region Private Members

        private int _id;
        private int _controllerId;
        private string _name;
        private double _quantity;
        private double controllerMeter;
        private double _balance;

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

        public int ControllerId
        {
            get => _controllerId;
            set
            {
                _controllerId = value;
                OnPropertyChanged(nameof(ControllerId));
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

        public double Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }

        [NotMapped]
        public double ControllerMeter
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
        public double Balance
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
