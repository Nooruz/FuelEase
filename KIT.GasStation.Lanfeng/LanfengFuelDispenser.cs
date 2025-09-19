using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.FuelDispenser.Services.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using static KIT.GasStation.FuelDispenser.Hubs.DeviceResponseHub;

namespace KIT.GasStation.Lanfeng
{
    /// <summary>
    /// Сервис для работы с колонкой Lanfeng через COM-порт.
    /// Прослушивает статусы и обрабатывает команды.
    /// </summary>
    public sealed class LanfengFuelDispenser : FuelDispenserServiceBase
    {
        #region Private Members

        private readonly ILogger<LanfengFuelDispenser> _logger;
        private readonly Controller _controller;
        private readonly IProtocolParser _protocolParser;
        private readonly ISharedSerialPortService _sharedSerialPortService;
        private readonly int _address;
        private readonly IReadOnlyList<Column> _columns;
        private readonly IHubContext<DeviceResponseHub, IDeviceResponseClient> _hub;

        #endregion

        #region Constructors

        public LanfengFuelDispenser(Controller controller, 
            ILogger<LanfengFuelDispenser> logger,
            int address,
            IProtocolParserFactory protocolParserFactory,
            ISharedSerialPortService sharedSerialPortService,
            IHubContext<DeviceResponseHub, IDeviceResponseClient> hub) 
            : base(controller, logger, address, protocolParserFactory, sharedSerialPortService, hub)
        {
            _logger = logger;
            _controller = controller;
            _protocolParser = protocolParserFactory.CreateIProtocolParser(Controller.Type);
            _sharedSerialPortService = sharedSerialPortService;
            _address = address;
            _columns = controller.Columns
                .Where(c => c.Address == address)
                .ToList();
            _hub = hub;
        }

        #endregion

        #region Protected

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            await _sharedSerialPortService.OpenAsync(_controller.ComPort, _controller.BaudRate, token);
        }

        /// <summary>
        /// Цикл опроса статуса ТРК.
        /// Выполняется пока не придёт сигнал отмены.
        /// </summary>
        protected override async Task OnTickAsync(CancellationToken token)
        {
            // Сообщаем в лог, что сервис запущен и на каком порту работает
            _logger.LogInformation("ТРК Lanfeng запущена, используется порт {Port}", _controller.ComPort);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Формируем команду на получение статуса (Tx - передача)
                    var command = _protocolParser.BuildRequest(Command.Status, _address, 0);
                    _logger.LogInformation("[Tx] {Tx}", BitConverter.ToString(command));

                    // Отправляем команду и ожидаем ответ (Rx - приём)
                    var result = await _sharedSerialPortService.WriteReadAsync(command, 14);
                    _logger.LogInformation("[Rx] {Rx}", BitConverter.ToString(result));

                    var status = _protocolParser.ParseResponse(result, Command.Status);

                    if (status != null)
                    {
                        _ = _hub.Clients.Group(Group(_controller.Name, _address)).StatusChanged(status);
                    }
                }
                catch (Exception e)
                {

                }
            }
        }

        protected override Task OnCloseAsync()
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}
