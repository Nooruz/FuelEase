using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.IO.Ports;

namespace KIT.GasStation.PKElectronics
{
    public class PKElectronicsFuelDispenser : FuelDispenserServiceBase
    {
        #region Private Members

        private readonly IHubClient _hubClient;
        private readonly ILogger _logger;
        private PortKey _portKey;
        private HubConnection _hub;
        private bool _hubHandlersRegistered;
        private int _hubRestartLoop;

        #endregion

        #region Constructors

        public PKElectronicsFuelDispenser(Controller controller,
            int address,
            ISharedSerialPortService sharedSerialPortService,
            IHubClient hubClient) 
            : base(controller, address, sharedSerialPortService, hubClient)
        {
            _hubClient = hubClient;
        }

        #endregion

        #region Protected

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            try
            {
                _logger.Information("Начало инициализации ТРК {Id}. Состояние HubConnection: {State}", Controller.Id, _hub?.State.ToString() ?? "null");

                _portKey = new PortKey(
                    portName: Controller.ComPort,                // например, "COM3"
                    baudRate: Controller.BaudRate,               // напр., 9600
                    parity: Parity.None,                // System.IO.Ports.Parity
                    dataBits: 8,              // обычно 8
                    stopBits: StopBits.One               // StopBits.One и т.п.
                );

                _hub = _hubClient.Connection;



            }
            catch (Exception ex)
            {

            }
        }

        protected override async Task OnTickAsync(CancellationToken token)
        {

        }

        protected override Task OnCloseAsync()
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Hub

        private void RegisterHubConnectionHandlers()
        {
            if (_hubHandlersRegistered || _hub is null)
                return;

            _hub.Reconnecting += OnHubReconnecting;
            _hub.Reconnected += OnHubReconnected;
            _hub.Closed += OnHubClosed;
            _hubHandlersRegistered = true;
        }

        private Task OnHubReconnecting(Exception? error)
        {
            _logger.Warning("Потеряно соединение с SignalR: {Message}", error?.Message ?? "unknown");
            return Task.CompletedTask;
        }

        private async Task OnHubReconnected(string? connectionId)
        {
            _logger.Information("SignalR переподключен. ConnectionId={ConnectionId}", connectionId);
            try
            {
                //await JoinWorkerGroupsAsync();
                //await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Не удалось повторно присоединиться к группам после переподключения");
            }
        }

        private Task OnHubClosed(Exception? error)
        {
            _logger.Error(error, "Соединение с SignalR было закрыто");
            return RestartHubConnectionLoopAsync();
        }

        private Task RestartHubConnectionLoopAsync()
        {
            if (_hub is null)
                return Task.CompletedTask;

            if (Interlocked.CompareExchange(ref _hubRestartLoop, 1, 0) != 0)
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                try
                {
                    while (_hub.State != HubConnectionState.Connected)
                    {
                        try
                        {
                            await _hub.StartAsync();
                            //await JoinWorkerGroupsAsync();
                            //await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Не удалось переподключиться к SignalR, повтор через 5 секунд");
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _hubRestartLoop, 0);
                }
            });
        }

        #endregion
    }
}
