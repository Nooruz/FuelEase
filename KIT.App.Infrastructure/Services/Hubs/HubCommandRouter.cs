using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace KIT.App.Infrastructure.Services.Hubs
{
    /// <summary>
    /// Подписывается на входящие команды SignalR ОДИН РАЗ
    /// и маршрутизирует их в нужный IFuelDispenserService по groupName.
    /// </summary>
    public sealed class HubCommandRouter : IHubCommandRouter, IAsyncDisposable
    {
        #region Private Members

        private readonly IHubClient _hubClient;
        private readonly IFuelDispenserRegistry _registry;
        private readonly ILogger<HubCommandRouter> _logger;
        private readonly IReportCommandCompleted _reportCommandCompleted;

        private readonly List<IDisposable> _subscriptions = new();
        private bool _registered;

        #endregion

        #region Constructors

        public HubCommandRouter(IHubClient hubClient,
            IFuelDispenserRegistry registry,
            ILogger<HubCommandRouter> logger,
            IReportCommandCompleted reportCommandCompleted)
        {
            _hubClient = hubClient;
            _registry = registry;
            _logger = logger;
            _reportCommandCompleted = reportCommandCompleted;
        }

        #endregion

        #region Public Voids

        public void RegisterHandlers()
        {
            if (_registered)
                return;

            var hub = _hubClient.Connection;

            _subscriptions.Add(hub.On<FuelingRequest>("StartFuelingAsync",
                async (fuelingRequest) =>
                {
                    await RouteStartFuelingAsync(fuelingRequest);
                }));

            _subscriptions.Add(hub.On<string>("StopFuelingAsync",
                async (groupName) =>
                {
                    await RouteStopFuelingAsync(groupName);
                }));

            _subscriptions.Add(hub.On<string>("CompleteFuelingAsync",
                async (groupName) =>
                {
                    await RouteCompleteFuelingAsync(groupName);
                }));

            _subscriptions.Add(hub.On<Guid, string>("GetCounterAsync",
                async (commandId, groupName) =>
                {
                    await RouteGetCounterAsync(commandId, groupName);
                }));

            _subscriptions.Add(hub.On<Guid, string>("GetCountersAsync",
                async (commandId, groupName) =>
                {
                    await RouteGetCountersAsync(commandId, groupName);
                }));

            _subscriptions.Add(hub.On<Guid, PriceRequest>("SetPriceAsync",
                async (commandId, priceRequeste) =>
                {
                    await RouteSetPriceAsync(commandId, priceRequeste);
                }
            ));

            _subscriptions.Add(hub.On<Guid, IReadOnlyCollection<PriceRequest>>("SetPricesAsync",
                async (commandId, prices) =>
                {
                    await RouteSetPricesAsync(commandId, prices);
                }
            ));

            _subscriptions.Add(hub.On<Guid, string>("InitializeConfigurationAsync",
                async (commandId, groupName) =>
                {
                    await RouteInitializeConfigurationAsync(commandId, groupName);
                }
            ));

            _subscriptions.Add(hub.On<ResumeFuelingRequest>("ResumeFuelingAsync",
                async (resumeFuelingRequest) =>
                {
                    await RouteResumeFuelingAsync(resumeFuelingRequest);
                }
            ));

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

        private async Task RouteStartFuelingAsync(FuelingRequest fuelingRequest)
        {
            var dispenser = _registry.GetByGroup(fuelingRequest.GroupName);
            if (dispenser is null)
            {
                _logger.LogWarning("StartFuelingAsync: ТРК для группы {Group} не найдена", fuelingRequest.GroupName);
                return;
            }

            _logger.LogInformation("Маршрутизация StartFuelingAsync -> {Group}", fuelingRequest.GroupName);
            await dispenser.StartFuelingAsync(fuelingRequest);
        }

        private async Task RouteStopFuelingAsync(string groupName)
        {
            var dispenser = _registry.GetByGroup(groupName);
            if (dispenser is null)
            {
                _logger.LogWarning("StopFuelingAsync: ТРК для группы {Group} не найдена", groupName);
                return;
            }

            _logger.LogInformation("Маршрутизация StopFuelingAsync -> {Group}", groupName);
            await dispenser.StopFuelingAsync(groupName);
        }

        private async Task RouteCompleteFuelingAsync(string groupName)
        {
            var dispenser = _registry.GetByGroup(groupName);
            if (dispenser is null)
            {
                _logger.LogWarning("CompleteFuelingAsync: ТРК для группы {Group} не найдена", groupName);
                return;
            }

            _logger.LogInformation("Маршрутизация CompleteFuelingAsync -> {Group}", groupName);
            await dispenser.CompleteFuelingAsync(groupName);
        }

        private async Task RouteGetCounterAsync(Guid commandId, string groupName)
        {
            try
            {
                var dispenser = _registry.GetByGroup(groupName);
                if (dispenser is null)
                {
                    await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                    {
                        CommandId = commandId,
                        GroupName = groupName,
                        IsSuccess = false,
                        ErrorMessage = $"ТРК для группы '{groupName}' не найдена"
                    });

                    return;
                }

                await dispenser.GetCounterAsync(commandId, groupName);

                await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                {
                    CommandId = commandId,
                    GroupName = groupName,
                    IsSuccess = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка GetCounterAsync для {Group}", groupName);

                await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                {
                    CommandId = commandId,
                    GroupName = groupName,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        private async Task RouteGetCountersAsync(Guid commandId, string groupName)
        {
            try
            {
                var dispenser = _registry.GetByGroup(groupName);
                if (dispenser is null)
                {
                    await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                    {
                        CommandId = commandId,
                        GroupName = groupName,
                        IsSuccess = false,
                        ErrorMessage = $"ТРК для группы '{groupName}' не найдена"
                    });

                    return;
                }

                await dispenser.GetCountersAsync(commandId, groupName);

                await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                {
                    CommandId = commandId,
                    GroupName = groupName,
                    IsSuccess = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка GetCounterAsync для {Group}", groupName);

                await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                {
                    CommandId = commandId,
                    GroupName = groupName,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        private async Task RouteSetPriceAsync(Guid commandId, PriceRequest priceRequest)
        {
            try
            {
                var dispenser = _registry.GetByGroup(priceRequest.GroupName);
                if (dispenser is null)
                {
                    await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                    {
                        CommandId = commandId,
                        GroupName = priceRequest.GroupName,
                        IsSuccess = false,
                        ErrorMessage = $"ТРК для группы '{priceRequest.GroupName}' не найдена"
                    });

                    return;
                }

                await dispenser.SetPriceAsync(commandId, priceRequest);

                await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                {
                    CommandId = commandId,
                    GroupName = priceRequest.GroupName,
                    IsSuccess = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка SetPriceAsync для {Group}", priceRequest.GroupName);

                await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                {
                    CommandId = commandId,
                    GroupName = priceRequest.GroupName,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        private async Task RouteSetPricesAsync(Guid commandId, IReadOnlyCollection<PriceRequest> prices)
        {
            var groupName = prices.First().GroupName;
            try
            {
                var dispenser = _registry.GetByGroup(groupName);
                if (dispenser is null)
                {
                    await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                    {
                        CommandId = commandId,
                        GroupName = groupName,
                        IsSuccess = false,
                        ErrorMessage = $"ТРК для группы '{groupName}' не найдена"
                    });

                    return;
                }

                await dispenser.SetPricesAsync(commandId, prices);

                await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                {
                    CommandId = commandId,
                    GroupName = groupName,
                    IsSuccess = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка SetPricesAsync для {Group}", groupName);

                await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                {
                    CommandId = commandId,
                    GroupName = groupName,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        private async Task RouteInitializeConfigurationAsync(Guid commandId, string groupName)
        {
            try
            {
                var dispenser = _registry.GetByGroup(groupName);
                if (dispenser is null)
                {
                    await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                    {
                        CommandId = commandId,
                        GroupName = groupName,
                        IsSuccess = false,
                        ErrorMessage = $"ТРК для группы '{groupName}' не найдена"
                    });

                    return;
                }

                await dispenser.InitializeConfigurationAsync(commandId, groupName);

                await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                {
                    CommandId = commandId,
                    GroupName = groupName,
                    IsSuccess = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка InitializeConfigurationAsync для {Group}", groupName);

                await _reportCommandCompleted.ReportCommandCompletedAsync(new CommandCompletion
                {
                    CommandId = commandId,
                    GroupName = groupName,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
            }
        }

        private async Task RouteResumeFuelingAsync(ResumeFuelingRequest resumeFuelingRequest)
        {
            var dispenser = _registry.GetByGroup(resumeFuelingRequest.GroupName);
            if (dispenser is null)
            {
                _logger.LogWarning("ResumeFuelingAsync: ТРК для группы {Group} не найдена", resumeFuelingRequest.GroupName);
                return;
            }

            _logger.LogInformation("Маршрутизация ResumeFuelingAsync -> {Group}", resumeFuelingRequest.GroupName);
            await dispenser.ResumeFuelingAsync(resumeFuelingRequest);
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
