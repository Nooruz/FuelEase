using DevExpress.Mvvm.DataAnnotations;
using FuelEase.Domain.Models;
using FuelEase.State.Nozzles;
using FuelEase.ViewModels.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FuelEase.ViewModels
{
    public class NozzleCounterPanelViewModel : PanelViewModel
    {
        #region Private Members

        private readonly INozzleStore _nozzleStore;
        private readonly ILogger<NozzleCounterPanelViewModel> _logger;
        private bool _showLoadingPanel;

        #endregion

        #region Public Properties

        public ObservableCollection<Nozzle> Nozzles
        {
            get
            {
                if (_nozzleStore.Nozzles != null && _nozzleStore.Nozzles.Any())
                {
                    return new(_nozzleStore.Nozzles.OrderBy(n => n.Tube).ToList());
                }
                return new();
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

        public NozzleCounterPanelViewModel(INozzleStore nozzleStore,
            ILogger<NozzleCounterPanelViewModel> logger)
        {
            _nozzleStore = nozzleStore;
            _logger = logger;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task RequestCounter()
        {
            try
            {
                ShowLoadindPanel(true);

                _nozzleStore.GetNozzleCounters();

                await Task.Delay(2000);

                //await _fuelSaleStore.GetAllNozzleCounter();

                OnPropertyChanged(nameof(Nozzles));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запросе счетчиков в GetNozzleCounters");
            }
            finally
            {
                ShowLoadindPanel(false);
            }
        }

        #endregion

        #region Private Voids

        private void ShowLoadindPanel(bool show)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ShowLoadingPanel = show;
            });
        }

        #endregion

    }
}
