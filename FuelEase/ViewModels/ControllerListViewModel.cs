using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Docking;
using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.Domain.Views;
using FuelEase.State.Navigators;
using FuelEase.State.Nozzles;
using FuelEase.ViewModels.Base;
using FuelEase.ViewModels.Factories;
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

namespace FuelEase.ViewModels
{
    public class ControllerListViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly INozzleStore _nozzleStore;
        private readonly ILogger<ControllerListViewModel> _logger;
        private readonly IViewService<TankFuelQuantityView> _tankFuelQuantityView;
        private readonly INavigator _navigator;
        private Nozzle _selectedNozzle;
        private List<DocumentPanelPosition> _positions = new();
        private ObservableCollection<Nozzle> _nozzles = new();
        private ObservableCollection<FuelDispenserViewModel> _fuelDispenserViewModels = new();
        private readonly string _documentPanelPositionPath = Path.Combine(AppContext.BaseDirectory, "DocumentPanelPosition.xml");

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
        public ObservableCollection<FuelDispenserViewModel> FuelDispenserViewModels
        {
            get => _fuelDispenserViewModels;
            set
            {
                _fuelDispenserViewModels = value;
                OnPropertyChanged(nameof(FuelDispenserViewModels));
            }
        }

        #endregion

        #region Constructor

        public ControllerListViewModel(INozzleStore nozzleStore,
            INavigator navigator,
            IViewService<TankFuelQuantityView> tankFuelQuantityView,
            ILogger<ControllerListViewModel> logger)
        {
            _nozzleStore = nozzleStore;
            _tankFuelQuantityView = tankFuelQuantityView;
            _logger = logger;
            _navigator = navigator;

            EnsureXmlFileExists(); // Создаем XML файл, если он отсутствует
        }

        #endregion

        #region Public Voids

        [Command]
        public void DocumentGroupLoaded(RoutedEventArgs args)
        {
            try
            {
                if (args.Source is DocumentGroup documentGroup)
                {
                    // Проверяем наличие XML файла
                    if (!File.Exists(_documentPanelPositionPath))
                    {
                        DefaultPositionDocumentPanel(documentGroup);
                    }
                }
            }
            catch (Exception e)
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
                    DependencyPropertyDescriptor.FromProperty(DocumentPanel.MDILocationProperty, typeof(DocumentPanel)).AddValueChanged(documentPanel, OnMDILocationPropertyChanged);
                    documentPanel.SizeChanged += DocumentPanel_SizeChanged;
                    SetDocumentPanelPosition(documentPanel);
                }
            }
            catch (Exception)
            {
                //ignore
            }
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

                    var positions = DeserializePositions();
                    var position = positions.FirstOrDefault(p => p.DocumentPanelName == documentPanel.ActualTabCaption);

                    if (position != null)
                    {
                        position.X = documentPanel.MDILocation.X;
                        position.Y = documentPanel.MDILocation.Y;
                        position.Width = documentPanel.ActualWidth;
                        position.Height = documentPanel.ActualHeight;
                    }
                    else
                    {
                        positions.Add(new DocumentPanelPosition
                        {
                            DocumentPanelName = documentPanel.ActualTabCaption,
                            X = documentPanel.MDILocation.X,
                            Y = documentPanel.MDILocation.Y,
                            Width = documentPanel.ActualWidth,
                            Height = documentPanel.ActualHeight
                        });
                    }

                    SerializePositions(positions);
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private void DefaultPositionDocumentPanel(DocumentGroup documentGroup)
        {
            var positions = new List<DocumentPanelPosition>();
            double maxWidth = documentGroup.ActualWidth;
            double allItemsWidth = 0;
            double allItemsHeight = 0;

            foreach (var item in documentGroup.Items.OfType<DocumentPanel>())
            {
                // Если размеры не определены, пропускаем этот элемент
                if (item.ActualWidth <= 0 || item.ActualHeight <= 0)
                    continue;

                if (allItemsWidth + item.ActualWidth >= maxWidth)
                {
                    allItemsWidth = 0;
                    allItemsHeight += item.ActualHeight + 5;
                }

                item.MDILocation = new Point(allItemsWidth, allItemsHeight);
                allItemsWidth += item.ActualWidth + 5;

                positions.Add(new DocumentPanelPosition
                {
                    DocumentPanelName = item.ActualTabCaption,
                    X = item.MDILocation.X,
                    Y = item.MDILocation.Y,
                    Width = item.ActualWidth,
                    Height = item.ActualHeight
                });
            }

            SerializePositions(positions);
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

                    var positions = DeserializePositions();
                    var position = positions.FirstOrDefault(p => p.DocumentPanelName == documentPanel.ActualTabCaption);

                    if (position != null)
                    {
                        position.Width = documentPanel.ActualWidth;
                        position.Height = documentPanel.ActualHeight;
                    }
                    else
                    {
                        positions.Add(new DocumentPanelPosition
                        {
                            DocumentPanelName = documentPanel.ActualTabCaption,
                            Width = documentPanel.ActualWidth,
                            Height = documentPanel.ActualHeight,
                        });
                    }

                    SerializePositions(positions);
                }
            }
            catch (Exception)
            {

            }
        }

        private void SetDocumentPanelPosition(DocumentPanel documentPanel)
        {
            var positions = DeserializePositions();
            var position = positions.FirstOrDefault(p => p.DocumentPanelName == documentPanel.ActualTabCaption);

            if (position != null)
            {
                documentPanel.MDILocation = new Point(position.X, position.Y);
                documentPanel.MDISize = new Size(position.Width, position.Height);
            }
        }

        private void EnsureXmlFileExists()
        {
            if (!File.Exists(_documentPanelPositionPath))
            {
                SerializePositions(new List<DocumentPanelPosition>());
            }
        }

        private List<DocumentPanelPosition> DeserializePositions()
        {
            EnsureXmlFileExists();
            using FileStream fs = File.OpenRead(_documentPanelPositionPath);
            var serializer = new XmlSerializer(typeof(List<DocumentPanelPosition>));
            return (List<DocumentPanelPosition>)serializer.Deserialize(fs);
        }

        private void SerializePositions(List<DocumentPanelPosition> positions)
        {
            using FileStream fs = File.Create(_documentPanelPositionPath);
            var serializer = new XmlSerializer(typeof(List<DocumentPanelPosition>));
            serializer.Serialize(fs, positions);
        }

        private async Task GetData()
        {
            foreach (var nozzle in Nozzles.GroupBy(n => new { n.Side }))
            {
                var viewModel = (FuelDispenserViewModel)await _navigator.GetViewModelAsync(ViewType.FuelDispenser);
                viewModel.Side = nozzle.Key.Side;
                viewModel.Nozzles = new(nozzle.OrderBy(n => n.Tube).ToList());
                FuelDispenserViewModels.Add(viewModel);
            }
        }

        #endregion

        #region HostedService

        public async Task StartAsync()
        {
            await GetData();
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {

            base.Dispose(disposing);
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
