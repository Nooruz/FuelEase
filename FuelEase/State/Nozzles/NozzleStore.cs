using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace FuelEase.State.Nozzles
{
    public class NozzleStore : INozzleStore
    {
        #region Private Members

        private readonly INozzleService _nozzleService;

        #endregion

        #region Public Properties

        public ObservableCollection<Nozzle> Nozzles { get; private set; } = new();

        #endregion

        #region Actions

        public event Action<int> OnNozzleSelected;
        public event Action OnNozzleCountersRequested;

        #endregion

        #region Constructors

        public NozzleStore(INozzleService nozzleService)
        {
            _nozzleService = nozzleService;
        }

        #endregion

        #region Public Voids

        public async Task InitializeAsync()
        {
            await LoadNozzles();
        }

        public void SelectNozzle(int tube)
        {
            OnNozzleSelected?.Invoke(tube);
        }

        public void GetNozzleCounters()
        {
            OnNozzleCountersRequested?.Invoke();
        }

        #endregion

        #region Private Voids

        private async Task LoadNozzles()
        {
            var nozzles = await _nozzleService.GetAllAsync();
            if (nozzles != null)
            {
                Nozzles = new ObservableCollection<Nozzle>(nozzles);
            }
            else
            {
                Nozzles = new ObservableCollection<Nozzle>();
            }
        }

        #endregion

        #region Hosted

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return InitializeAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}
