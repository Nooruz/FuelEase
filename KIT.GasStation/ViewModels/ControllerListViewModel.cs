using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Docking;
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
using System.Windows.Controls;
using System.Windows.Threading;
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
        private readonly Dictionary<string, DocumentPanelPosition> _positions;
        private readonly HashSet<string> _initializingDocumentPanels = new(StringComparer.Ordinal);
        private readonly string _documentPanelPositionPath;

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
        public DockLayoutManager MainDockLayoutManager { get; set; }
        public Canvas MainCanvas { get; set; }
        public ObservableCollection<FuelDispenserViewModel> FuelDispenserViewModels
        {
            get => _fuelDispenserViewModels;
            set
            {
                _fuelDispenserViewModels = value;
                OnPropertyChanged(nameof(FuelDispenserViewModels));
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

            _documentPanelPositionPath = GetDocumentPanelPositionPath();
            _positions = LoadPositions();
            _navigator.OnDispose += Navigator_OnDispose;
        }

        #endregion

        #region Public Voids

        [Command]
        public void ItemsControlLoaded(RoutedEventArgs args)
        {
            try
            {
                if (args.Source is ItemsControl itemsControl)
                {
                    // Проверяем наличие XML файла
                    if (!File.Exists(_documentPanelPositionPath))
                    {
                        DefaultPositionDocumentPanel(itemsControl);
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        [Command]
        public void DocumentPanelLoaded(RoutedEventArgs args)
        {
            try
            {
                if (args.Source is DocumentPanel documentPanel)
                {
                    if (!string.IsNullOrWhiteSpace(documentPanel.ActualTabCaption))
                    {
                        _initializingDocumentPanels.Add(documentPanel.ActualTabCaption);
                    }

                    DependencyPropertyDescriptor.FromProperty(DocumentPanel.MDILocationProperty, typeof(DocumentPanel)).AddValueChanged(documentPanel, OnMDILocationPropertyChanged);
                    documentPanel.SizeChanged += DocumentPanel_SizeChanged;
                    SetDocumentPanelPosition(documentPanel);

                    documentPanel.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(documentPanel.ActualTabCaption))
                        {
                            _initializingDocumentPanels.Remove(documentPanel.ActualTabCaption);
                        }
                    }), DispatcherPriority.Loaded);
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public bool CanMoveItem(
            FuelDispenserViewModel current,
            double newX,
            double newY,
            double currentWidth,
            double currentHeight,
            double canvasWidth,
            double canvasHeight)
        {
            const double spacing = 10;

            if (newX < 0 || newY < 0)
                return false;

            if (newX + currentWidth > canvasWidth || newY + currentHeight > canvasHeight)
                return false;

            Rect newRect = new(
                newX - spacing,
                newY - spacing,
                currentWidth + spacing * 2,
                currentHeight + spacing * 2);

            foreach (var item in FuelDispenserViewModels)
            {
                if (item == current)
                    continue;

                // Тут нужен доступ к реальным размерам других элементов
                // Ниже покажу нормальный способ
            }

            return true;
        }

        #endregion

        #region Private Voids

        private void OnMDILocationPropertyChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is DocumentPanel documentPanel)
                {
                    // Если размеры равны нулю, не обновляем позицию
                    if (documentPanel.ActualWidth == 0 || documentPanel.ActualHeight == 0)
                        return;

                    UpdatePosition(documentPanel, position =>
                    {
                        position.X = documentPanel.MDILocation.X;
                        position.Y = documentPanel.MDILocation.Y;
                        position.Width = documentPanel.ActualWidth;
                        position.Height = documentPanel.ActualHeight;
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update document panel position on MDI location change.");
            }
        }

        private void DefaultPositionDocumentPanel(ItemsControl itemsControl)
        {
            //double maxWidth = documentGroup.ActualWidth;
            //double allItemsWidth = 0;
            //double allItemsHeight = 0;

            //foreach (var item in documentGroup.Items.OfType<DocumentPanel>())
            //{
            //    // Если размеры не определены, пропускаем этот элемент
            //    if (item.ActualWidth <= 0 || item.ActualHeight <= 0)
            //        continue;

            //    if (allItemsWidth + item.ActualWidth >= maxWidth)
            //    {
            //        allItemsWidth = 0;
            //        allItemsHeight += item.ActualHeight + 5;
            //    }

            //    item.MDILocation = new Point(allItemsWidth, allItemsHeight);
            //    allItemsWidth += item.ActualWidth + 5;

            //    if (!string.IsNullOrWhiteSpace(item.ActualTabCaption))
            //    {
            //        _positions[item.ActualTabCaption] = new DocumentPanelPosition
            //        {
            //            DocumentPanelName = item.ActualTabCaption,
            //            X = item.MDILocation.X,
            //            Y = item.MDILocation.Y,
            //            Width = item.ActualWidth,
            //            Height = item.ActualHeight
            //        };
            //    }
            //}

            //SavePositions();
        }

        private void DocumentPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (sender is DocumentPanel documentPanel)
                {
                    // Проверяем, что новые размеры больше нуля
                    if (e.NewSize.Width <= 0 || e.NewSize.Height <= 0)
                        return;

                    UpdatePosition(documentPanel, position =>
                    {
                        position.Width = documentPanel.ActualWidth;
                        position.Height = documentPanel.ActualHeight;
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update document panel position on size change.");
            }
        }

        private void SetDocumentPanelPosition(DocumentPanel documentPanel)
        {
            if (string.IsNullOrWhiteSpace(documentPanel.ActualTabCaption))
            {
                return;
            }
            if (_positions.TryGetValue(documentPanel.ActualTabCaption, out var position))
            {
                documentPanel.MDILocation = new Point(position.X, position.Y);
                documentPanel.MDISize = new Size(position.Width, position.Height);
            }
        }

        private Dictionary<string, DocumentPanelPosition> LoadPositions()
        {
            var path = GetDocumentPanelPositionPath();

            if (!File.Exists(_documentPanelPositionPath))
            {
                return new Dictionary<string, DocumentPanelPosition>(StringComparer.Ordinal);
            }
            try
            {
                using FileStream fs = File.OpenRead(_documentPanelPositionPath);
                var serializer = new XmlSerializer(typeof(List<DocumentPanelPosition>));
                var positions = (List<DocumentPanelPosition>)serializer.Deserialize(fs);
                return positions
                    .Where(position => !string.IsNullOrWhiteSpace(position.DocumentPanelName))
                    .ToDictionary(position => position.DocumentPanelName, StringComparer.Ordinal);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load document panel positions.");
                return new Dictionary<string, DocumentPanelPosition>(StringComparer.Ordinal);
            }
        }

        private void SavePositions()
        {
            try
            {
                using FileStream fs = File.Create(_documentPanelPositionPath);
                var serializer = new XmlSerializer(typeof(List<DocumentPanelPosition>));
                serializer.Serialize(fs, _positions.Values.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save document panel positions.");
            }
        }

        private void UpdatePosition(DocumentPanel documentPanel, Action<DocumentPanelPosition> update)
        {
            if (string.IsNullOrWhiteSpace(documentPanel.ActualTabCaption))
            {
                return;
            }

            if (_initializingDocumentPanels.Contains(documentPanel.ActualTabCaption))
            {
                return;
            }

            if (!_positions.TryGetValue(documentPanel.ActualTabCaption, out var position))
            {
                position = new DocumentPanelPosition
                {
                    DocumentPanelName = documentPanel.ActualTabCaption
                };
                _positions[documentPanel.ActualTabCaption] = position;
            }
            update(position);
            SavePositions();
        }

        private string GetDocumentPanelPositionPath()
        {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appDir = Path.Combine(baseDir, "КИТ-АЗС");

            Directory.CreateDirectory(appDir); // гарантируем, что папка существует

            return Path.Combine(appDir, "DocumentPanelPosition.xml");
        }

        private async Task GetData()
        {
            var createdViewModels = new List<FuelDispenserViewModel>();

            foreach (var nozzle in Nozzles.GroupBy(n => n.Side).OrderBy(g => g.Key))
            {
                var viewModel = (FuelDispenserViewModel)await _navigator.GetViewModelAsync(ViewType.FuelDispenser);
                viewModel.Side = nozzle.Key;
                viewModel.Nozzles = new(nozzle.OrderBy(n => n.Tube).ToList());
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
                await item.StopAsync();
            }
        }

        #endregion
    }

    public class DocumentPanelPosition
    {
        public string DocumentPanelName { get; set; }
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
