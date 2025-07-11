using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace KIT.GasStation.ViewModels.Details
{
    public class FuelDetailViewModel : BaseViewModel
    {
        #region Private Members

        private readonly IFuelService _fuelService;
        private readonly IUnitOfMeasurementService _unitOfMeasurementService;
        private ObservableCollection<UnitOfMeasurement> _unitOfMeasurements;
        private Fuel _fuel = new();

        #endregion

        #region Public Properties

        public ObservableCollection<UnitOfMeasurement> UnitOfMeasurements
        {
            get => _unitOfMeasurements;
            set
            {
                _unitOfMeasurements = value;
                OnPropertyChanged(nameof(UnitOfMeasurements));
            }
        }
        public Fuel Fuel
        {
            get => _fuel;
            set
            {
                _fuel = value;
                OnPropertyChanged(nameof(Fuel));
            }
        }
        public Color Color
        {
            get => (Color)ColorConverter.ConvertFromString(Fuel.ColorHex);
            set
            {
                if (Fuel != null)
                {
                    Fuel.ColorHex = value.ToString();
                }
                OnPropertyChanged(nameof(Color));
            }
        }

        #endregion

        #region Constructor

        public FuelDetailViewModel(IUnitOfMeasurementService unitOfMeasurementService, 
            IFuelService fuelService)
        {
            _unitOfMeasurementService = unitOfMeasurementService;
            _fuelService = fuelService;
        }

        #endregion

        #region Public Voids

        [Command]
        public async void UserControlLoaded()
        {
            UnitOfMeasurements = new(await _unitOfMeasurementService.GetAllAsync());
        }

        [Command]
        public async void Create()
        {
            if (Fuel.Id == 0)
            {
                await _fuelService.CreateAsync(Fuel);
            }
            else
            {
                await _fuelService.UpdateAsync(Fuel.Id, Fuel);
            }
            CurrentWindowService?.Close();
        }

        #endregion
    }
}
