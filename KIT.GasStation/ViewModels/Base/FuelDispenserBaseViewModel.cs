namespace KIT.GasStation.ViewModels.Base
{
    public class FuelDispenserBaseViewModel : BaseViewModel
    {
        #region Private Members

        private double _x;
        private double _y;

        // Дизайнерская ширина FuelTransferColumnControl = 340px.
        // При ширине карточки 350px (доступно ~338px) масштаб Viewbox ≈ 1.0.
        private double _width = 350;

        // Дизайнерская высота при 4 пистолетах = 440px.
        // При высоте карточки 452px (доступно ~440px) контент в Viewbox не обрезается.
        private double _height = 452;

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

        public double Height
        {
            get => _height;
            set
            {
                if (_height == value) return;
                _height = value;
                OnPropertyChanged(nameof(Height));
            }
        }

        /// <summary>
        /// При дизайнерской ширине 340px минимум 220px даёт масштаб ~0.62 (шрифт ~12px).
        /// </summary>
        public double MinWidth => 170;

        /// <summary>
        /// При дизайнерской высоте 440px минимум 300px даёт масштаб ~0.65.
        /// </summary>
        public double MinHeight => 250;

        #endregion
    }
}
