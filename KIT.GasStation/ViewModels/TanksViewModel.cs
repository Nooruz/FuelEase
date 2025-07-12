using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Domain.Views;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Details;
using KIT.GasStation.ViewModels.Factories;
using KIT.GasStation.Views.Details;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels
{
    public class TanksViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly IFuelService _fuelService;
        private readonly INozzleService _nozzleService;
        private readonly ITankService _tankService;
        private readonly IViewService<TankFuelQuantityView> _tankFuelQuantityView;
        private readonly IUnitOfMeasurementService _unitOfMeasurementService;
        //private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly ILogger<TanksViewModel> _logger;
        private Fuel _selectedFuel;
        private Nozzle _selectedNozzle;
        private TankFuelQuantityView _selectedTank;
        private ObservableCollection<Fuel> _fuels = new();
        private ObservableCollection<TankFuelQuantityView> _tanks = new();
        private ObservableCollection<UnitOfMeasurement> _unitOfMeasurements = new();
        private ObservableCollection<Nozzle> _nozzles = new();
        //private ObservableCollection<Column> _columns = new();

        #endregion

        #region Public Properties

        public Fuel SelectedFuel
        {
            get => _selectedFuel;
            set
            {
                _selectedFuel = value;
                OnPropertyChanged(nameof(SelectedFuel));
            }
        }
        public Nozzle SelectedNozzle
        {
            get => _selectedNozzle;
            set
            {
                _selectedNozzle = value;
                OnPropertyChanged(nameof(SelectedNozzle));
            }
        }
        public TankFuelQuantityView SelectedTank
        {
            get => _selectedTank;
            set
            {
                _selectedTank = value;
                OnPropertyChanged(nameof(SelectedTank));
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
        public ObservableCollection<TankFuelQuantityView> Tanks
        {
            get => _tanks;
            set
            {
                _tanks = value;
                OnPropertyChanged(nameof(Tanks));
            }
        }
        public ObservableCollection<UnitOfMeasurement> UnitOfMeasurements
        {
            get => _unitOfMeasurements;
            set
            {
                _unitOfMeasurements = value;
                OnPropertyChanged(nameof(UnitOfMeasurements));
            }
        }
        public ObservableCollection<Nozzle> Nozzles
        {
            get => _nozzles;
            set
            {
                _nozzles = value;
                OnPropertyChanged(nameof(Nozzles));
            }
        }
        //public ObservableCollection<Column> Columns
        //{
        //    get => _columns;
        //    set
        //    {
        //        _columns = value;
        //        OnPropertyChanged(nameof(Columns));
        //    }
        //}

        #endregion

        #region Constructor

        public TanksViewModel(IFuelService fuelService,
            INozzleService nozzleService,
            IUnitOfMeasurementService unitOfMeasurementService,
            ITankService tankService,
            IViewService<TankFuelQuantityView> tankFuelQuantityView,
            ILogger<TanksViewModel> logger)
        {
            _fuelService = fuelService;
            _nozzleService = nozzleService;
            _unitOfMeasurementService = unitOfMeasurementService;
            _tankService = tankService;
            _logger = logger;
            _tankFuelQuantityView = tankFuelQuantityView;
            //_hardwareConfigurationService = hardwareConfigurationService;

            Title = "Топлива и резервуары";

            _fuelService.OnCreated += FuelService_OnCreated;
            _fuelService.OnUpdated += FuelService_OnUpdated;
            _fuelService.OnDeleted += FuelService_OnDeleted;
            _tankService.OnCreated += TankService_OnCreated;
            _tankService.OnUpdated += TankService_OnUpdated;
            _tankService.OnDeleted += TankService_OnDeleted;
            _nozzleService.OnCreated += NozzleService_OnCreated;
            _nozzleService.OnUpdated += NozzleService_OnUpdated;
            _nozzleService.OnDeleted += NozzleService_OnDeleted;
        }

        #endregion

        #region Public Voids

        [Command]
        public void CreateFuel()
        {
            WindowService.Title = "Создание топлива";
            WindowService.Show(nameof(FuelDetailView), new FuelDetailViewModel(_unitOfMeasurementService, _fuelService));
        }

        [Command]
        public async Task CreateTank()
        {
            var viewModel = new TankDetailViewModel(_tankService, _fuelService);
            await viewModel.StartAsync();
            WindowService.Title = "Создание резервуара";
            WindowService.Show(nameof(TankDetailView), viewModel);
        }

        [Command]
        public void EditFuel()
        {
            if (SelectedFuel != null)
            {
                var viewModel = new FuelDetailViewModel(_unitOfMeasurementService, _fuelService)
                {
                    Fuel = new Fuel(SelectedFuel)
                };

                WindowService.Title = $"({SelectedFuel.Name}) Редактирование";
                WindowService.Show(nameof(FuelDetailView), viewModel);
            }
        }

        [Command]
        public async Task EditTank()
        {
            if (SelectedTank != null)
            {
                var viewModel = new TankDetailViewModel(_tankService, _fuelService)
                {
                    Tank = new Tank
                    {
                        Id = SelectedTank.Id,
                        Size = SelectedTank.Size,
                        MinimumSize = SelectedTank.MinimumSize,
                        Name = SelectedTank.Name,
                        FuelId = SelectedTank.FuelId,
                        Number = SelectedTank.Number,
                    }
                };

                await viewModel.StartAsync();

                WindowService.Title = $"({SelectedTank.Name}) Редактирование";
                WindowService.Show(nameof(TankDetailView), viewModel);
            }
        }

        [Command]
        public async void DeleteFuel()
        {
            if (SelectedFuel != null)
            {
                if (MessageBoxService.ShowMessage($"Удалить топливо? ({SelectedFuel.Name})", "Внимание", MessageButton.YesNo, MessageIcon.Question) == MessageResult.Yes)
                {
                    if (SelectedFuel.Tanks == null || !SelectedFuel.Tanks.Any())
                    {
                        if (await _fuelService.DeleteAsync(SelectedFuel.Id))
                        {
                            _ = Fuels.Remove(SelectedFuel);
                        }
                    }
                }
            }
        }

        [Command]
        public async void DeleteTank()
        {
            if (SelectedTank != null)
            {
                if (MessageBoxService.ShowMessage($"Удалить резервуар? ({SelectedTank.Name})", "Внимание", MessageButton.YesNo, MessageIcon.Question) == MessageResult.Yes)
                {
                    if (await _tankService.DeleteAsync(SelectedTank.Id))
                    {
                        foreach (var item in Fuels)
                        {
                            var tank = item.Tanks.FirstOrDefault(t => t.Id == SelectedTank.Id);
                            if (tank != null)
                            {
                                _ = item.Tanks.Remove(tank);
                            }
                        }
                        _ = Tanks.Remove(SelectedTank);
                    }
                }
            }
        }

        [Command]
        public async Task CreateNozzle()
        {
            //var viewModel = new NozzleDetailViewModel(_nozzleService, _tankService, _hardwareConfigurationService);

            //await viewModel.StartAsync();

            //WindowService.Title = "Создание ТРК";
            //WindowService.Show(nameof(NozzleDetailView), viewModel);
        }

        [Command]
        public async Task EditNozzle()
        {
            //if (SelectedNozzle != null)
            //{
            //    var viewModel = new NozzleDetailViewModel(_nozzleService, _tankService, _hardwareConfigurationService)
            //    {
            //        CreatedNozzle = new Nozzle(SelectedNozzle)
            //    };

            //    await viewModel.StartAsync();

            //    WindowService.Title = $"({SelectedNozzle.Name}) Редактирование";
            //    WindowService.Show(nameof(NozzleDetailView), viewModel);
            //}
        }

        [Command]
        public async Task DeleteNozzle()
        {
            if (SelectedNozzle != null)
            {
                var result = MessageBoxService.ShowMessage($"Удалить ТРК? ({SelectedNozzle.Name})", "Внимание", MessageButton.YesNo, MessageIcon.Question);

                if (result == MessageResult.Yes)
                {
                    _ = await _nozzleService.DeleteAsync(SelectedNozzle.Id);
                }
            }
            else
            {
                MessageBoxService.ShowMessage("Выберите ТРК", "Внимание", MessageButton.OK, MessageIcon.Warning);
            }
        }

        public async Task StartAsync()
        {
            Fuels = new(await _fuelService.GetAllAsync());
            Tanks = new(await _tankFuelQuantityView.GetAllAsync());
            Nozzles = new(await _nozzleService.GetAllAsync());
            //Columns = await _hardwareConfigurationService.GetColumnsAsync();
        }

        #endregion

        #region Private Voids

        private void FuelService_OnCreated(Fuel obj)
        {
            Fuels.Add(obj);
        }

        private void FuelService_OnUpdated(Fuel obj)
        {
            Fuel? fuel = Fuels.FirstOrDefault(f => f.Id == obj.Id);
            fuel?.Update(obj);
        }

        private void FuelService_OnDeleted(int id)
        {
            Fuel? fuel = Fuels.FirstOrDefault(f => f.Id == id);
            if (fuel != null)
            {
                Fuels.Remove(fuel);
            }
        }

        private void TankService_OnUpdated(Tank tank)
        {
            try
            {
                TankFuelQuantityView selectedTank = Tanks.First(t => t.Id == tank.Id);
                selectedTank.Name = tank.Name;
                selectedTank.Size = tank.Size;
                selectedTank.FuelId = tank.FuelId;
                selectedTank.MinimumSize = tank.MinimumSize;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private void TankService_OnCreated(Tank tank)
        {
            try
            {
                Tanks.Add(new TankFuelQuantityView
                {
                    Id = tank.Id,
                    Name = tank.Name,
                    Size = tank.Size,
                    MinimumSize = tank.MinimumSize,
                    FuelId = tank.FuelId,
                    Fuel = tank.Fuel.Name
                });
                Fuel fuel = Fuels.First(f => f.Id == tank.FuelId);
                fuel.Tanks.Add(tank);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private void TankService_OnDeleted(int id)
        {
            TankFuelQuantityView? tank = Tanks.FirstOrDefault(f => f.Id == id);
            if (tank != null)
            {
                Tanks.Remove(tank);
            }
        }

        private void NozzleService_OnUpdated(Nozzle updateNozzle)
        {
            Nozzle? nozzle = Nozzles.FirstOrDefault(n => n.Id == updateNozzle.Id);
            nozzle?.Update(updateNozzle);
        }

        private void NozzleService_OnCreated(Nozzle createdNozzle)
        {
            Nozzles.Add(createdNozzle);
        }

        private void NozzleService_OnDeleted(int id)
        {
            Nozzle? nozzle = Nozzles.FirstOrDefault(f => f.Id == id);
            if (nozzle != null)
            {
                Nozzles.Remove(nozzle);
            }
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fuelService.OnCreated -= FuelService_OnCreated;
                _fuelService.OnUpdated -= FuelService_OnUpdated;
                _fuelService.OnDeleted -= FuelService_OnDeleted;
                _tankService.OnCreated -= TankService_OnCreated;
                _tankService.OnUpdated -= TankService_OnUpdated;
                _tankService.OnDeleted -= TankService_OnDeleted;
                _nozzleService.OnCreated -= NozzleService_OnCreated;
                _nozzleService.OnUpdated -= NozzleService_OnUpdated;
                _nozzleService.OnDeleted -= NozzleService_OnDeleted;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
