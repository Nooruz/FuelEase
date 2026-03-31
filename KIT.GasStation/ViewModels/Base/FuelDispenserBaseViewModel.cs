namespace KIT.GasStation.ViewModels.Base
{
    public class FuelDispenserBaseViewModel : BaseViewModel
    {
        #region Private Members

        private double _x;
        private double _y;
        private double _width = 200;

        #endregion

        #region Public Properties

        public double X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged(nameof(X));
            }
        }
        public double Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged(nameof(Y));
            }
        }
        public double Width
        {
            get => _width;
            set
            {
                if (_width == value) return;
                _width = value;
                OnPropertyChanged(nameof(Width));
            }
        }
        public double MinWidth => 100;

        #endregion

        #region Public Voids



        #endregion
    }
}
