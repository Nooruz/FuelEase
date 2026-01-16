using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels.Details
{
    public class TankDetailViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Pirvate Members

        private readonly ITankService _tankService;
        private readonly IFuelService _fuelService;
        private Tank _tank = new();
        private ObservableCollection<Fuel> _fuels;

        #endregion

        #region Public Properties

        public Tank Tank
        {
            get => _tank;
            set
            {
                _tank = value;
                OnPropertyChanged(nameof(Tank));
            }
        }
        public ObservableCollection<Fuel> Fuels
        {
            get => _fuels;
            set
            {
                _fuels = value;
                OnPropertyChanged(nameof(Fuels));
            }
        }

        #endregion

        #region Constructor

        public TankDetailViewModel(ITankService tankService, 
            IFuelService fuelService)
        {
            _tankService = tankService;
            _fuelService = fuelService;

            _tankService.OnCreated += TankService_OnCreated;
        }

        #endregion

        #region Public Voids

        [Command]
        public async void Create()
        {
            if (string.IsNullOrEmpty(Tank.Name))
            {
                MessageBoxService.ShowMessage("Введите название резервуара.",
                    "Предупреждение", MessageButton.OK, MessageIcon.Warning);
                return;
            }

            if (Tank.Number <= 0)
            {
                MessageBoxService.ShowMessage("Код резервуара должен быть больше нуля.",
                    "Предупреждение", MessageButton.OK, MessageIcon.Warning);
                return;
            }

            if (Tank.FuelId == 0)
            {
                MessageBoxService.ShowMessage(
                            "Выберите тип топлива.",
                            "Предупреждение",
                            MessageButton.OK,
                            MessageIcon.Warning);
                return;
            }

            if (Tank.Size <= 0) // должен быть выше 0
            {
                MessageBoxService.ShowMessage(
                        "Емкость резервуара должна быть больше нуля.",
                        "Предупреждение",
                        MessageButton.OK,
                        MessageIcon.Warning);
                return;
            }

            if (await _tankService.IsNumberAvailableAsync(Tank))
            {
                MessageBoxService.ShowMessage($"Резервуар с кодом \"{Tank.Number}\" уже существует. Укажите другой код.",
                    "Предупреждение", MessageButton.OK, MessageIcon.Warning);
                return;
            }

            if (Tank.Id == 0)
            {
                _ = await _tankService.CreateAsync(Tank);
                CurrentWindowService?.Close();
            }
            else
            {
                _ = await _tankService.UpdateAsync(Tank.Id, Tank);
                CurrentWindowService?.Close();
            }
        }

        [Command]
        public async void UserControlLoaded()
        {
            Fuels = new(await _fuelService.GetAllAsync());
        }

        public async Task StartAsync()
        {
            try
            {
                var tanks = await _tankService.GetAllAsync();

                if (Tank.Id > 0) return;

                if (tanks != null && tanks.Any())
                {
                    int number = tanks.Count() + 1;
                    Tank.Name = $"Резервуар {number}";
                    Tank.Number = number;
                }
                else
                {
                    Tank.Name = "Резервуар 1";
                    Tank.Number = 1;
                }
            }
            catch (Exception)
            {

            }
        }

        #endregion

        #region Private Voids

        private async void TankService_OnCreated(Tank tank)
        {
            await StartAsync();
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tankService.OnCreated -= TankService_OnCreated;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
