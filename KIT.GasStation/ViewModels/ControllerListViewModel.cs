using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Domain.Views;
using KIT.GasStation.State.Navigators;
using KIT.GasStation.State.Nozzles;
using KIT.GasStation.State.Users;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace KIT.GasStation.ViewModels
{
    public class ControllerListViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly INozzleStore _nozzleStore;
        private readonly INozzleService _nozzleService;
        private readonly IUserStore _userStore;
        private readonly ILogger<ControllerListViewModel> _logger;
        private readonly IViewService<TankFuelQuantityView> _tankFuelQuantityView;
        private readonly INavigator _navigator;
        private Nozzle _selectedNozzle;
        private ObservableCollection<FuelDispenserViewModel> _fuelDispenserViewModels = new();
        private FuelDispenserViewModel _selectedFuelDispenser;
        private readonly Dictionary<string, ControllerPosition> _positions;
        private readonly string _positionsFilePath;
        private bool _isLoadingPositions;

        #endregion

        #region Public Properties

        public ObservableCollection<Nozzle> Nozzles => _nozzleStore.Nozzles;
        public Nozzle SelectedNozzle
        {
            get => _selectedNozzle;
            set
            {
                _selectedNozzle = value;
                OnPropertyChanged(nameof(SelectedNozzle));
            }
        }
        public ObservableCollection<FuelDispenserViewModel> FuelDispenserViewModels
        {
            get => _fuelDispenserViewModels;
            set
            {
                _fuelDispenserViewModels = value;
                OnPropertyChanged(nameof(FuelDispenserViewModels));
            }
        }
        public FuelDispenserViewModel SelectedFuelDispenser
        {
            get => _selectedFuelDispenser;
            set
            {
                _selectedFuelDispenser = value;
                OnPropertyChanged(nameof(SelectedFuelDispenser));
            }
        }
        public bool IsCurrentUserAdmin => _userStore.CurrentUser?.UserType == UserType.Admin;

        #endregion

        #region Constructor

        public ControllerListViewModel(INozzleStore nozzleStore,
            INavigator navigator,
            IViewService<TankFuelQuantityView> tankFuelQuantityView,
            ILogger<ControllerListViewModel> logger,
            IUserStore userStore,
            INozzleService nozzleService)
        {
            _nozzleStore = nozzleStore;
            _tankFuelQuantityView = tankFuelQuantityView;
            _logger = logger;
            _navigator = navigator;
            _userStore = userStore;
            _nozzleService = nozzleService;

            _userStore.OnLogin += UserStore_OnLogin;
            _nozzleService.OnCreated += NozzleService_OnCreated;

            _positionsFilePath = GetPositionsFilePath();
            _positions = LoadPositions();
            _navigator.OnDispose += Navigator_OnDispose;
        }

        #endregion

        #region Public Voids

        [Command]
        public void ItemsControlLoaded(RoutedEventArgs args)
        {
            // Позиции уже восстановлены в GetData()
        }

        #endregion

        #region Private Voids

        /// <summary>
        /// Подписка на PropertyChanged вью-модели для автосохранения позиций.
        /// </summary>
        private void SubscribeToPositionChanges(FuelDispenserViewModel vm)
        {
            vm.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void UnsubscribeFromPositionChanges(FuelDispenserViewModel vm)
        {
            vm.PropertyChanged -= ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_isLoadingPositions)
                return;

            if (e.PropertyName is nameof(FuelDispenserViewModel.X)
                              or nameof(FuelDispenserViewModel.Y)
                              or nameof(FuelDispenserViewModel.Width)
                              or nameof(FuelDispenserViewModel.Height))
            {
                if (sender is FuelDispenserViewModel vm)
                {
                    var key = vm.Side.ToString();
                    _positions[key] = new ControllerPosition
                    {
                        Side = vm.Side,
                        X = vm.X,
                        Y = vm.Y,
                        Width = vm.Width,
                        Height = vm.Height
                    };
                    SavePositions();
                }
            }
        }

        /// <summary>
        /// Применяет сохранённые позиции к вью-модели.
        /// </summary>
        private void ApplyPosition(FuelDispenserViewModel vm)
        {
            var key = vm.Side.ToString();
            if (_positions.TryGetValue(key, out var pos))
            {
                _isLoadingPositions = true;
                try
                {
                    vm.X = pos.X;
                    vm.Y = pos.Y;

                    // Применяем сохранённую ширину только если она не меньше MinWidth.
                    // Старые записи (до введения Viewbox) могли хранить ширину 100–200px,
                    // что при Viewbox scale < 0.65 даёт нечитаемый контент.
                    if (pos.Width >= vm.MinWidth)
                        vm.Width = pos.Width;
                    // иначе остаётся дефолтное значение из FuelDispenserBaseViewModel (_width = 350)

                    // Height не привязан к ListBoxItem (высота auto из Viewbox).
                    // Сохраняем в ViewModel на будущее, но визуально не влияет.
                    if (pos.Height > 0)
                        vm.Height = pos.Height;
                }
                finally
                {
                    _isLoadingPositions = false;
                }
            }
        }

        private Dictionary<string, ControllerPosition> LoadPositions()
        {
            if (!File.Exists(_positionsFilePath))
            {
                return new Dictionary<string, ControllerPosition>(StringComparer.Ordinal);
            }
            try
            {
                using FileStream fs = File.OpenRead(_positionsFilePath);
                var serializer = new XmlSerializer(typeof(List<ControllerPosition>));
                var list = (List<ControllerPosition>)serializer.Deserialize(fs);
                return list.ToDictionary(p => p.Side.ToString(), StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось загрузить позиции контроллеров.");
                return new Dictionary<string, ControllerPosition>(StringComparer.Ordinal);
            }
        }

        private void SavePositions()
        {
            try
            {
                using FileStream fs = File.Create(_positionsFilePath);
                var serializer = new XmlSerializer(typeof(List<ControllerPosition>));
                serializer.Serialize(fs, _positions.Values.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Не удалось сохранить позиции контроллеров.");
            }
        }

        private string GetPositionsFilePath()
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDir = Path.Combine(baseDir, "КИТ-АЗС");
            Directory.CreateDirectory(appDir);
            return Path.Combine(appDir, "ControllerPositions.xml");
        }

        private async Task GetData()
        {
            var createdViewModels = new List<FuelDispenserViewModel>();

            foreach (var nozzle in Nozzles.GroupBy(n => n.Side).OrderBy(g => g.Key))
            {
                var viewModel = (FuelDispenserViewModel)await _navigator.GetViewModelAsync(ViewType.FuelDispenser);
                viewModel.Side = nozzle.Key;
                viewModel.Nozzles = new(nozzle.OrderBy(n => n.Tube).ToList());

                // Восстанавливаем сохранённую позицию и подписываемся на изменения
                ApplyPosition(viewModel);
                SubscribeToPositionChanges(viewModel);

                createdViewModels.Add(viewModel);
            }

            FuelDispenserViewModels = new ObservableCollection<FuelDispenserViewModel>(createdViewModels);
        }

        private async void NozzleService_OnCreated(Nozzle createdNozzle)
        {
            if (!Nozzles.Any(n => n.Side == createdNozzle.Side))
            {
                var viewModel = (FuelDispenserViewModel)await _navigator.GetViewModelAsync(ViewType.FuelDispenser);
                viewModel.Side = createdNozzle.Side;
                viewModel.Nozzles.Add(createdNozzle);

                ApplyPosition(viewModel);
                SubscribeToPositionChanges(viewModel);

                FuelDispenserViewModels.Add(viewModel);
            }
        }

        private void UserStore_OnLogin(User user)
        {
            OnPropertyChanged(nameof(IsCurrentUserAdmin));
        }

        #endregion

        #region HostedService

        public async Task StartAsync()
        {
            await GetData();
        }

        #endregion

        #region Dispose

        private async void Navigator_OnDispose()
        {
            _userStore.OnLogin -= UserStore_OnLogin;
            _nozzleService.OnCreated -= NozzleService_OnCreated;
            _navigator.OnDispose -= Navigator_OnDispose;

            foreach (var item in FuelDispenserViewModels)
            {
                UnsubscribeFromPositionChanges(item);
                await item.StopAsync();
            }
        }

        #endregion
    }

    public class ControllerPosition
    {
        public int Side { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public enum FuelTransperControllerControlMode
    {
        None,

        /// <summary>
        /// Управление через программы
        /// </summary>
        Program,

        /// <summary>
        /// Управление через клавиатуры
        /// </summary>
        Keyboard
    }
}
