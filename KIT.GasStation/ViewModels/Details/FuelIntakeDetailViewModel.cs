using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Domain.Views;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels.Details
{
    public class FuelIntakeDetailViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly IViewService<TankFuelQuantityView> _tankFuelQuantityView;
        private readonly IDataService<FuelIntake> _fuelIntakeService;
        private readonly ITankService _tankService;
        private readonly IShiftStore _shiftStore;
        private FuelIntake _fuelIntake = new() { CreateDate = DateTime.Now };
        private ObservableCollection<Tank> _tanks;
        private Tank _selectedTank;
        private ObservableCollection<TankFuelQuantityView> _tankFuelQuantityViews = new();

        #endregion

        #region Public Properties

        public FuelIntake FuelIntake
        {
            get => _fuelIntake;
            set
            {
                _fuelIntake = value;
                OnPropertyChanged(nameof(FuelIntake));
            }
        }
        public ObservableCollection<Tank> Tanks
        {
            get => _tanks;
            set
            {
                _tanks = value;
                OnPropertyChanged(nameof(Tanks));
            }
        }
        public Tank SelectedTank
        {
            get => _selectedTank;
            set
            {
                _selectedTank = value;
                FuelIntake.TankId = value.Id;
                OnPropertyChanged(nameof(SelectedTank));
            }
        }

        #endregion

        #region Constructor

        public FuelIntakeDetailViewModel(IDataService<FuelIntake> fuelIntakeService,
            IViewService<TankFuelQuantityView> tankFuelQuantityView,
            ITankService tankService,
            IShiftStore shiftStore)
        {
            _tankFuelQuantityView = tankFuelQuantityView;
            _fuelIntakeService = fuelIntakeService;
            _tankService = tankService;
            _shiftStore = shiftStore;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task Create()
        {
            if (CheckFuelIntake())
            {
                if (FuelIntake.Id == 0)
                {
                    FuelIntake.ShiftId = _shiftStore.CurrentShift.Id;
                    _ = await _fuelIntakeService.CreateAsync(FuelIntake);
                }
                else
                {
                    _ = await _fuelIntakeService.UpdateAsync(FuelIntake.Id, FuelIntake);
                }
                CurrentWindowService.Close();
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
                Tanks = new(await _tankService.GetAllAsync());
                _tankFuelQuantityViews = new(await _tankFuelQuantityView.GetAllAsync());
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private bool CheckFuelIntake()
        {
            var tankFuelQuantityView = _tankFuelQuantityViews.FirstOrDefault(x => x.Id == FuelIntake.TankId);

            if (tankFuelQuantityView == null)
            {
                return false;
            }

            if (FuelIntake.TankId == 0)
            {
                MessageBoxService.ShowMessage("Не выбран резервуар", "Ошибка", MessageButton.OK, MessageIcon.Error);
                return false;
            }
            if (FuelIntake.Quantity == 0)
            {
                MessageBoxService.ShowMessage("Не указано количество", "Ошибка", MessageButton.OK, MessageIcon.Error);
                return false;
            }
            if ((FuelIntake.Quantity + tankFuelQuantityView.CurrentFuelQuantity) > SelectedTank.Size)
            {
                MessageBoxService.ShowMessage("Количество топлива, поступающее для заправки, превышает доступный объем резервуара с учетом текущего остатка топлива. Проверьте введенные данные.", "Ошибка", MessageButton.OK, MessageIcon.Error);
                return false;
            }
            return true;
        }

        #endregion
    }
}
