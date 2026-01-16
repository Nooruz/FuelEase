using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace KIT.GasStation.Domain.Views
{
    public class TankFuelQuantityView : ViewObject
    {
        #region Private Members

        private int _id;
        private int _fuelId;
        private int _number;
        private string _name;
        private string _fuel;
        private decimal _size;
        private decimal _minimumSize;
        private decimal _currentFuelQuantity;
        private string _colorHex;

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

        public int FuelId
        {
            get => _fuelId;
            set
            {
                _fuelId = value;
                OnPropertyChanged(nameof(FuelId));
            }
        }

        public int Number
        {
            get => _number;
            set
            {
                _number = value;
                OnPropertyChanged(nameof(Number));
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

        public string Fuel
        {
            get => _fuel;
            set
            {
                _fuel = value;
                OnPropertyChanged(nameof(Fuel));
            }
        }

        public decimal Size
        {
            get => _size;
            set
            {
                _size = value;
                OnPropertyChanged(nameof(Size));
            }
        }

        public decimal MinimumSize
        {
            get => _minimumSize;
            set
            {
                _minimumSize = value;
                OnPropertyChanged(nameof(MinimumSize));
            }
        }

        public decimal CurrentFuelQuantity
        {
            get => _currentFuelQuantity;
            set
            {
                _currentFuelQuantity = value;
                OnPropertyChanged(nameof(CurrentFuelQuantity));
            }
        }

        public string ColorHex
        {
            get => _colorHex;
            set
            {
                _colorHex = value;
                OnPropertyChanged(nameof(ColorHex));
                OnPropertyChanged(nameof(ColorBrush));
            }
        }

        #endregion

        #region NotMapped

        [NotMapped]
        public Color ColorBrush
        {
            get => ColorTranslator.FromHtml(ColorHex);
            set => ColorHex = ColorToHex(value);
        }

        #endregion

        #region Private Voids

        private static string ColorToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        #endregion
    }
}
