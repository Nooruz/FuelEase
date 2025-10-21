using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.FuelDispenser.Services.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

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
        private readonly IProtocolParser _protocolParser;
        private readonly ISharedSerialPortService _sharedSerialPortService;
        private readonly IHubClient _hubClient;
        private HubConnection _hub;

        #endregion

        #region Constructors

        public LanfengFuelDispenser(Controller controller, 
            ILogger<LanfengFuelDispenser> logger,
            int address,
            IProtocolParserFactory protocolParserFactory,
            ISharedSerialPortService sharedSerialPortService,
            IHubClient hubClient) 
            : base(controller, logger, address, protocolParserFactory, sharedSerialPortService, hubClient)
        {
            _logger = logger;
            _protocolParser = protocolParserFactory.CreateIProtocolParser(Controller.Type);
            _sharedSerialPortService = sharedSerialPortService;
            _hubClient = hubClient;
        }

        #endregion

        #region Protected Voids

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            try
            {
                _hub = _hubClient.Connection;

                _hub.On<StartPollingCommand>("StartPolling", async e =>
                {   
                    await OnTickAsync(token);
                });

                await _hubClient.EnsureStartedAsync();
                await _sharedSerialPortService.OpenAsync(Controller.ComPort, Controller.BaudRate, token);

                foreach (var item in Controller.Columns)
                {
                    await _hub.InvokeAsync("JoinController", $"{Controller.Name}/{item.Name}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        /// <summary>
        /// Цикл опроса статуса ТРК.
        /// Выполняется пока не придёт сигнал отмены.
        /// </summary>
        protected override async Task OnTickAsync(CancellationToken token)
        {
            // Сообщаем в лог, что сервис запущен и на каком порту работает
            _logger.LogInformation("ТРК Lanfeng запущена, используется порт {Port}", Controller.ComPort);

            await GetStatusAsync();

            if (Status != NozzleStatus.Unknown)
            {
                // Задаем программное управление
                await SetProgramControlModeAsync();

                // Получаем версию прошивки
                await GetFirmwareVersionAsync();

                // Инициализация по пистолетам
                await InitializeByColumns();
            }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    // Формируем команду на получение статуса (Tx - передача)
                    var command = _protocolParser.BuildRequest(Command.Status, Address, 0);
                    _logger.LogInformation("[Tx] {Tx}", BitConverter.ToString(command));

                    // Отправляем команду и ожидаем ответ (Rx - приём)
                    var result = await _sharedSerialPortService.WriteReadAsync(command, 14);
                    _logger.LogInformation("[Rx] {Rx}", BitConverter.ToString(result));

                    var status = _protocolParser.ParseResponse(result);

                    if (status != null)
                    {
                        await _hub.InvokeAsync("PublishStatus", status, "jf/Колонка_1");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message, e.StackTrace);
                }
            }
        }

        protected override Task OnCloseAsync()
        {
            return Task.CompletedTask;
        }

        protected override async Task GetStatusAsync()
        {
            try
            {
                // Формируем команду на получение статуса (Tx - передача)
                var command = _protocolParser.BuildRequest(Command.Status, Address, 0);
                _logger.LogInformation("[Tx] {Tx}", BitConverter.ToString(command));

                // Отправляем команду и ожидаем ответ (Rx - приём)
                var result = await _sharedSerialPortService.WriteReadAsync(command, 14);
                _logger.LogInformation("[Rx] {Rx}", BitConverter.ToString(result));

                var status = _protocolParser.ParseResponse(result);
                Status = status.Status;
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message, e.StackTrace);
            }
        }

        #endregion

        #region Private Voids

        private async Task SetProgramControlModeAsync()
        {
            try
            {
                // Формируем команду на получение статуса (Tx - передача)
                var command = _protocolParser.BuildRequest(Command.ProgramControlMode, Address, 0);
                _logger.LogInformation("[Tx] {Tx}", BitConverter.ToString(command));

                // Отправляем команду и ожидаем ответ (Rx - приём)
                var result = await _sharedSerialPortService.WriteReadAsync(command, 14);
                _logger.LogInformation("[Rx] {Rx}", BitConverter.ToString(result));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message, e.StackTrace);
            }
        }

        private async Task GetFirmwareVersionAsync()
        {
            try
            {
                // Формируем команду на получение статуса (Tx - передача)
                var command = _protocolParser.BuildRequest(Command.FirmwareVersion, Address, 0);
                _logger.LogInformation("[Tx] {Tx}", BitConverter.ToString(command));

                // Отправляем команду и ожидаем ответ (Rx - приём)
                var result = await _sharedSerialPortService.WriteReadAsync(command, 14);
                _logger.LogInformation("[Rx] {Rx}", BitConverter.ToString(result));

                var deviceResponse = _protocolParser.ParseResponse(result);

                if (deviceResponse != null)
                {
                    await _hub.InvokeAsync("PublishStatus", deviceResponse, "jf/Колонка_1");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message, e.StackTrace);
            }
        }

        private async Task InitializeByColumns()
        {
            foreach (var column in Columns)
            {
                // Получает счетчик литров.
                await GetCounterLiterAsync(column);
                await Task.Delay(300);
            }
        }

        private async Task GetCounterLiterAsync(Column column)
        {
            try
            {
                // Формируем команду на получение статуса (Tx - передача)
                var command = _protocolParser.BuildRequest(Command.CounterLiter, Address, column.Address);
                _logger.LogInformation("[Tx] {Tx}", BitConverter.ToString(command));

                // Отправляем команду и ожидаем ответ (Rx - приём)
                var result = await _sharedSerialPortService.WriteReadAsync(command, 14);
                _logger.LogInformation("[Rx] {Rx}", BitConverter.ToString(result));

                var deviceResponse = _protocolParser.ParseResponse(result);

                if (deviceResponse != null)
                {
                    await _hub.InvokeAsync("PublishStatus", deviceResponse, $"{Controller.Name}/{column.Name}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message, e.StackTrace);
            }
        }

        #endregion
    }
}
