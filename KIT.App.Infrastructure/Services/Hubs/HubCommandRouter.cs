using KIT.GasStation.FuelDispenser.Hubs;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace KIT.App.Infrastructure.Services.Hubs
{
    public sealed class HubCommandRouter : IHubCommandRouter
    {
        #region Private Members

        private readonly IHubClient _hubClient;
        private readonly IFuelDispenserRegistry _registry;
        private readonly ILogger<HubCommandRouter> _logger;

        private readonly List<IDisposable> _subscriptions = new();
        private bool _registered;

        #endregion

        #region Constructors

        public HubCommandRouter(IHubClient hubClient,
            IFuelDispenserRegistry registry,
            ILogger<HubCommandRouter> logger)
        {
            _hubClient = hubClient;
            _registry = registry;
            _logger = logger;
        }

        #endregion

        #region Public Voids

        public void RegisterHandlers()
        {
            if (_registered)
                return;

            var hub = _hubClient.Connection;

            _subscriptions.Add(hub.On<string, decimal, bool>("StartFuelingAsync",
                async (groupName, sum, bySum) =>
                {
                    await RouteStartFuelingAsync(groupName, sum, bySum);
                }));

            //_subscriptions.Add(hub.On<string>("StopFuelingAsync",
            //    async groupName =>
            //    {
            //        await RouteStopFuelingAsync(groupName);
            //    }));

            //_subscriptions.Add(hub.On<string, decimal, bool>("AuthorizeAsync",
            //    async (groupName, amount, bySum) =>
            //    {
            //        await RouteAuthorizeAsync(groupName, amount, bySum);
            //    }));

            _registered = true;
            _logger.LogInformation("HubCommandRouter: обработчики зарегистрированы");
        }

        #endregion

        #region Private Voids

        private async Task RouteStartFuelingAsync(string groupName, decimal sum, bool bySum)
        {
            var dispenser = _registry.GetByGroup(groupName);
            if (dispenser is null)
            {
                _logger.LogWarning("StartFuelingAsync: ТРК для группы {Group} не найдена", groupName);
                return;
            }

            _logger.LogInformation("Маршрутизация StartFuelingAsync -> {Group}", groupName);
            await dispenser.StartFuelingAsync(groupName, sum, bySum);
        }

        //private async Task RouteStopFuelingAsync(string groupName)
        //{
        //    var dispenser = _registry.GetByGroup(groupName);
        //    if (dispenser is null)
        //    {
        //        _logger.LogWarning("StopFuelingAsync: ТРК для группы {Group} не найдена", groupName);
        //        return;
        //    }

        //    _logger.LogInformation("Маршрутизация StopFuelingAsync -> {Group}", groupName);
        //    await dispenser.StopFuelingAsync();
        //}

        //private async Task RouteAuthorizeAsync(string groupName, decimal amount, bool bySum)
        //{
        //    var dispenser = _registry.GetByGroup(groupName);
        //    if (dispenser is null)
        //    {
        //        _logger.LogWarning("AuthorizeAsync: ТРК для группы {Group} не найдена", groupName);
        //        return;
        //    }

        //    _logger.LogInformation("Маршрутизация AuthorizeAsync -> {Group}", groupName);
        //    await dispenser.AuthorizeAsync(amount, bySum);
        //}

        #endregion

        #region Other Voids

        public ValueTask DisposeAsync()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
            return ValueTask.CompletedTask;
        }

        #endregion
    }
}
