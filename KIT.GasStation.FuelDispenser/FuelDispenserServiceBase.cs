using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Serilog;

namespace KIT.GasStation.FuelDispenser
{
    public abstract class FuelDispenserServiceBase : IFuelDispenserService
    {
        protected readonly Controller Controller;
        protected readonly int Address;
        protected readonly IReadOnlyList<Column> Columns;
        protected ISharedSerialPortService _sharedSerialPortService;
        protected IHubClient _hubClient;
        protected ILogger _logger;

        public string DispenserName => throw new NotImplementedException();

        public string Version => throw new NotImplementedException();

        public Guid ControllerId => throw new NotImplementedException();

        public NozzleStatus Status {  get; set; }
        

        protected FuelDispenserServiceBase(Controller controller,
            int address,
            ISharedSerialPortService sharedSerialPortService,
            IHubClient hubClient)
        {
            Controller = controller;
            Columns = Controller.Columns.Where(c => c.Address == address).ToList();
            Address = address;
            _sharedSerialPortService = sharedSerialPortService;
            _hubClient = hubClient;
        }

        protected FuelDispenserServiceBase(Controller controller,
            ISharedSerialPortService sharedSerialPortService,
            IHubClient hubClient)
        {
            Controller = controller;
            Columns = Controller.Columns.ToList();
            _sharedSerialPortService = sharedSerialPortService;
            _hubClient = hubClient;
        }

        public async Task RunAsync(CancellationToken token)
        {
            var opened = false;

            try
            {
                await OnOpenAsync(token);
                opened = true;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await OnTickAsync(token);
                        // Краткая задержка между тиками, если OnTickAsync завершается быстро
                        await Task.Delay(TimeSpan.FromMilliseconds(100), token);
                    }
                    catch (OperationCanceledException) when (token.IsCancellationRequested)
                    {
                        // Ожидаем штатное завершение по токену отмены
                        break;
                    }
                    catch (Exception ex)
                    {
                        // Логируем ошибку, но продолжаем цикл
                        // Реализуйте ILogger в базовом классе или передайте его через конструктор
                        //_logger.Error(ex, "Ошибка в OnTickAsync");
                        await Task.Delay(TimeSpan.FromSeconds(1), token);
                    }
                }

                await Task.Delay(Timeout.Infinite, token);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                _logger.Information("Цикл ТРК отменен по токену");
            }
            finally
            {
                if (opened)
                {
                    await OnCloseAsync();
                    _logger.Information("Цикл ТРК завершен");
                }
            }
        }

        protected virtual Task OnOpenAsync(CancellationToken token) => Task.CompletedTask;
        protected virtual Task OnCloseAsync() => Task.CompletedTask;

        // Обязательный шаг: тик опроса/обработки
        protected abstract Task OnTickAsync(CancellationToken token);

        public virtual ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
