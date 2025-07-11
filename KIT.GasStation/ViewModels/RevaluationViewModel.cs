using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels
{
    public class RevaluationViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly IFuelService _fuelService;
        private readonly IDataService<FuelRevaluation> _fuelRevaluationService;
        private ObservableCollection<FuelRevaluation> _fuelRevaluations = new();
        private ObservableCollection<Fuel> _fuels;
        private bool _showLoadingPanel;

        #endregion

        #region Public Properties

        public ObservableCollection<FuelRevaluation> FuelRevaluations
        {
            get => _fuelRevaluations;
            set
            {
                _fuelRevaluations = value;
                OnPropertyChanged(nameof(FuelRevaluations));
            }
        }
        public bool ShowLoadingPanel
        {
            get => _showLoadingPanel;
            set
            {
                _showLoadingPanel = value;
                OnPropertyChanged(nameof(ShowLoadingPanel));
            }
        }

        #endregion

        #region Constructor

        public RevaluationViewModel(IFuelService fuelService, 
            IDataService<FuelRevaluation> fuelRevaluationService)
        {
            _fuelService = fuelService;
            _fuelRevaluationService = fuelRevaluationService;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task StartRevaluation()
        {
            try
            {
                foreach (FuelRevaluation fuelRevaluation in FuelRevaluations.Where(f => f.NewPrice != f.OldPrice))
                {
                    if (fuelRevaluation.NewPrice > 0)
                    {
                        _ = await _fuelRevaluationService.CreateAsync(fuelRevaluation);
                        Fuel fuel = _fuels.FirstOrDefault(f => f.Id == fuelRevaluation.FuelId);
                        fuel.Price = fuelRevaluation.NewPrice;
                        _ = await _fuelService.UpdateAsync(fuel.Id, fuel);
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public async Task StartAsync()
        {
            await GetData();
        }

        #endregion

        #region Private Voids

        private async Task GetData()
        {
            try
            {
                _fuels = new(await _fuelService.GetAllAsync());
                foreach (Fuel fuel in _fuels)
                {

                    FuelRevaluations.Add(new FuelRevaluation
                    {
                        Fuel = fuel,
                        FuelId = fuel.Id,
                        OldPrice = fuel.Price,
                        NewPrice = fuel.Price,
                        CreatedDate = DateTime.Now
                    });
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                
            }

            base.Dispose(disposing);
        }

        

        #endregion
    }
}
