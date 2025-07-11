using KIT.GasStation.Domain.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.ObjectModel;

namespace KIT.GasStation.State.Nozzles
{
    public interface INozzleStore : IHostedService
    {
        ObservableCollection<Nozzle> Nozzles { get; }
        event Action<int> OnNozzleSelected;
        event Action OnNozzleCountersRequested;
        void SelectNozzle(int tube);
        void GetNozzleCounters();
    }
}
