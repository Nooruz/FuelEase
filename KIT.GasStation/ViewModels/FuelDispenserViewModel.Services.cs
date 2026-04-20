using DevExpress.Mvvm;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Models.CashRegisters;
using KIT.GasStation.FuelDispenser.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels
{
    public partial class FuelDispenserViewModel
    {
        #region FuelSale Service Events

        /// <summary>
        /// При создании новой продажи: привязывает её к пистолету и отправляет команду запуска заправки на хаб.
        /// </summary>
        private async void FuelSaleService_OnCreated(FuelSale fuelSale)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Id == fuelSale.NozzleId);
            if (nozzle is null) return;

            nozzle.CurrentFuelSale = fuelSale;

            try
            {
                if (fuelSale.DiscountSale is not null)
                {
                    // Логика скидочной продажи — команды через хаб пока не используются
                    return;
                }

                await _hub.InvokeAsync("SetPriceAsync", new PriceRequest
                {
                    GroupName = nozzle.Group,
                    Value = nozzle.Price
                });

                var startMode = fuelSale.IsForSum
                        ? FuelingStartMode.ByAmount
                        : FuelingStartMode.ByVolume;

                var fuelingRequest = new FuelingRequest
                {
                    GroupName = nozzle.Group,
                    Quantity = fuelSale.Quantity,
                    Sum = fuelSale.Sum,
                    FuelingStartMode = startMode
                };

                await _hub.InvokeAsync("StartFuelingAsync", fuelingRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при запуске заправки для пистолета {Group}", nozzle.Group);
            }
        }

        /// <summary>
        /// При обновлении продажи: синхронизирует статус и пересчитывает итоговую выручку пистолета.
        /// </summary>
        private void FuelSaleService_OnUpdated(FuelSale fuelSale)
        {
            if (fuelSale.FuelSaleStatus != FuelSaleStatus.Completed) return;

            if (SelectedNozzle?.CurrentFuelSale?.Id == fuelSale.Id)
                SelectedNozzle.CurrentFuelSale.FuelSaleStatus = fuelSale.FuelSaleStatus;

            var nozzle = Nozzles.FirstOrDefault(n => n.Id == fuelSale.NozzleId);
            if (nozzle is null) return;

            _ = Task.Run(async () =>
            {
                nozzle.SalesSum = await _fuelSaleService.GetReceivedQuantityAsync(
                    nozzle.Id, _shiftStore.CurrentShift.Id);
            });
        }

        /// <summary>
        /// При возобновлении прерванной заправки: проверяет остаток топлива и запускает
        /// заправку на оставшуюся сумму.
        /// </summary>
        private async void FuelSaleService_OnResumeFueling(FuelSale fuelSale)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Id == fuelSale.NozzleId);
            if (nozzle is null) return;

            nozzle.CurrentFuelSale = fuelSale;

            try
            {
                if (!await ValidateFuelQuantity()) return;

                var fuelingRequest = new FuelingRequest
                {
                    GroupName = nozzle.Group,
                    Quantity = fuelSale.Quantity - fuelSale.ReceivedQuantity,
                    Sum = fuelSale.Sum - fuelSale.ReceivedSum,
                    FuelingStartMode = FuelingStartMode.ByAmount
                };

                await _hub.InvokeAsync("StartFuelingAsync", fuelingRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при возобновлении заправки для пистолета {Group}", nozzle.Group);
            }
        }

        #endregion

        #region FiscalData Service Events

        /// <summary>
        /// Добавляет созданный фискальный документ к текущей продаже выбранного пистолета.
        /// </summary>
        private void FiscalDataService_OnCreated(FiscalData fiscalData)
        {
            if (SelectedNozzle?.CurrentFuelSale is null) return;
            if (SelectedNozzle.CurrentFuelSale.Id != fiscalData.FuelSaleId) return;

            SelectedNozzle.CurrentFuelSale.FiscalDatas.Add(fiscalData);
        }

        #endregion

        #region Counter Events

        /// <summary>
        /// Обрабатывает пакетное обновление счётчиков нескольких пистолетов (событие GetCountersAsync).
        /// Защищён мьютексом для последовательной записи незарегистрированных продаж.
        /// </summary>
        private async Task CountersUpdated(List<CounterData> counterDatas)
        {
            await _countersGate.WaitAsync();
            try
            {
                foreach (var item in counterDatas)
                    await ProcessNozzleCounterAsync(item.GroupName, item.Counter);
            }
            finally
            {
                _countersGate.Release();
            }
        }

        /// <summary>
        /// Обрабатывает обновление счётчика одного пистолета (событие GetCounterAsync).
        /// Защищён тем же мьютексом, что и пакетный вариант — предотвращает дублирование записей
        /// при частом опросе (каждые 300 мс от Lanfeng/Gilbarco).
        /// </summary>
        private async Task CounterUpdated(CounterData counterData)
        {
            await _countersGate.WaitAsync();
            try
            {
                await ProcessNozzleCounterAsync(counterData.GroupName, counterData.Counter);
            }
            finally
            {
                _countersGate.Release();
            }
        }

        /// <summary>
        /// Обрабатывает обновление счётчика одного пистолета (устаревший формат FuelingResponse).
        /// </summary>
        private Task OnCounterReceived(FuelingResponse response)
            => ProcessNozzleCounterAsync(response.GroupName, response.Quantity);

        /// <summary>
        /// Ядро обработки счётчика: обновляет LastCounter пистолета, проверяет незавершённые продажи
        /// и регистрирует незарегистрированный отпуск, если счётчик вышел за пределы ожидаемого.
        /// </summary>
        private async Task ProcessNozzleCounterAsync(string groupName, decimal counter)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == groupName);
            if (nozzle is null) return;

            nozzle.LastCounter = counter;

            var shift = _shiftStore.CurrentShift;
            if (shift is null) return;
            if (_shiftStore.CurrentShiftState is ShiftState.Closed) return;

            await CheckUncompletedSales();

            var shiftCounter = await _shiftCounterService.GetAsync(nozzle.Id, shift.Id);
            if (shiftCounter is null) return;

            var totalSales = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, shift.Id);
            var unregisteredSales = await _unregisteredSaleService.GetAllAsync(nozzle.Id, shift.Id);

            decimal unregisteredSalesQty = unregisteredSales?.Sum(u => u.Quantity) ?? 0m;
            decimal expectedCounter = shiftCounter.BeginSaleCounter + totalSales + unregisteredSalesQty;
            decimal unregisteredQty = nozzle.LastCounter - (shiftCounter.BeginNozzleCounter + expectedCounter);

            // <= 0: нулевое расхождение — всё сошлось; отрицательное — ошибка данных/переполнение счётчика
            if (unregisteredQty <= 0m) return;

            var unregisteredSale = new UnregisteredSale
            {
                NozzleId = nozzle.Id,
                ShiftId = shift.Id,
                CreateDate = DateTime.Now,
                State = UnregisteredSaleState.Waiting,
                Quantity = unregisteredQty,
                Sum = unregisteredQty * nozzle.Price
            };

            await _unregisteredSaleService.CreateAsync(unregisteredSale);
        }

        #endregion

        #region Shift Events

        /// <summary>
        /// При авторизации оператора: запускает задачу подключения к хабу.
        /// </summary>
        private void ShiftStore_OnLogin(Shift shift)
        {
            _cts = new System.Threading.CancellationTokenSource();
            _startTask = StartAsync(_cts.Token);
        }

        /// <summary>
        /// При открытии смены: запрашивает счётчики и создаёт начальные записи ShiftCounter для каждого пистолета.
        /// </summary>
        private async void ShiftStore_OnOpened(Shift shift)
        {
            try
            {
                foreach (var nozzle in Nozzles)
                    await _hub.InvokeAsync("GetCountersAsync", nozzle.Group);

                await Task.Delay(1500);

                foreach (var nozzle in Nozzles)
                {
                    var existing = await _shiftCounterService.GetAsync(nozzle.Id, shift.Id);
                    if (existing is not null) continue;

                    var shiftCounter = new ShiftCounter
                    {
                        NozzleId = nozzle.Id,
                        ShiftId = _shiftStore.CurrentShift.Id,
                        BeginNozzleCounter = nozzle.LastCounter,
                        BeginSaleCounter = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, shift.Id)
                    };

                    await _shiftCounterService.CreateAsync(shiftCounter);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при открытии смены для стороны {Side}", Side);
            }
        }

        /// <summary>
        /// При закрытии смены: запрашивает актуальные счётчики всех пистолетов (для финальной
        /// проверки незарегистрированных продаж), затем фиксирует конечные показания в ShiftCounter.
        /// </summary>
        private async void ShiftStore_OnClosed(Shift shift)
        {
            try
            {
                // Запрашиваем счётчики для ВСЕХ пистолетов, чтобы обнаружить незарегистрированные
                // продажи, совершённые в конце смены. Используем distinct-группы, как при открытии.
                var groups = Nozzles
                    .Select(n => n.Group)
                    .Where(g => !string.IsNullOrWhiteSpace(g))
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                foreach (var group in groups)
                    await _hub.InvokeAsync("GetCountersAsync", group);

                // Даём время на приход ответов и обработку ProcessNozzleCounterAsync
                await Task.Delay(1500);

                foreach (var nozzle in Nozzles)
                {
                    var nozzleCounter = await _shiftCounterService.GetAsync(nozzle.Id, shift.Id);
                    if (nozzleCounter is null) continue;

                    nozzleCounter.EndNozzleCounter = nozzle.LastCounter;
                    nozzleCounter.EndSaleCounter = await _fuelSaleService.GetReceivedQuantityAsync(nozzle.Id, shift.Id);
                    await _shiftCounterService.UpdateAsync(nozzleCounter.Id, nozzleCounter);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при закрытии смены для стороны {Side}", Side);
            }
        }

        /// <summary>
        /// Проверяет и автоматически завершает продажи, у которых фактический объём равен запрошенному.
        /// </summary>
        private async Task CheckUncompletedSales()
        {
            try
            {
                var sales = await _fuelSaleService.GetUncompletedFuelSaleAsync(_shiftStore.CurrentShift.Id);
                if (sales is null) return;

                foreach (var sale in sales.Where(u => u.ReceivedQuantity > 0))
                {
                    if (sale.ReceivedQuantity == sale.Quantity && sale.FuelSaleStatus != FuelSaleStatus.Completed)
                    {
                        sale.FuelSaleStatus = FuelSaleStatus.Completed;
                        await _fuelSaleService.UpdateAsync(sale.Id, sale);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке незавершённых продаж (сторона {Side})", Side);
            }
        }

        #endregion

        #region Fuel Service Events

        /// <summary>
        /// При изменении данных топлива (цены, названия): обновляет каждый пистолет и отправляет новую цену на хаб.
        /// </summary>
        private async void FuelService_OnUpdated(Fuel fuel)
        {
            try
            {
                var affected = Nozzles.Where(n => n.Tank.FuelId == fuel.Id).ToList();
                foreach (var nozzle in affected)
                {
                    nozzle.Tank?.Fuel.Update(fuel);
                    await _hub.InvokeAsync("SetPriceAsync", new PriceRequest
                    {
                        GroupName = nozzle.Group,
                        Value = fuel.Price
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении цены топлива (FuelId={FuelId})", fuel.Id);
            }
        }

        #endregion

        #region User Store Events

        /// <summary>
        /// При выходе оператора из системы: останавливает ViewModel и отписывается от хаба.
        /// </summary>
        private async void UserStore_OnLogout()
        {
            await StopAsync();
        }

        #endregion

        #region Nozzle Events

        /// <summary>
        /// При выборе пистолета через общий NozzleStore: переключает SelectedNozzle.
        /// </summary>
        private void OnNozzleSelected(int tube)
        {
            if (SelectedNozzle is null || SelectedNozzle.Tube != tube)
                SelectNozzle(tube);
        }

        /// <summary>
        /// Запрашивает обновление счётчиков по событию из NozzleStore.
        /// </summary>
        private async void OnNozzleCountersRequested()
        {
            var nozzle = Nozzles.FirstOrDefault();
            if (nozzle is null) return;
            await _hub.InvokeAsync("GetCountersAsync", nozzle.Group);
        }

        /// <summary>
        /// Устанавливает <see cref="SelectedNozzle"/> по номеру трубки.
        /// </summary>
        private void SelectNozzle(int tube)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Tube == tube);
            if (nozzle is not null)
                SelectedNozzle = nozzle;
        }

        /// <summary>
        /// Загружает последнюю продажу для каждого пистолета и выбирает самую свежую.
        /// </summary>
        private async Task GetNozzleLastFuelSale()
        {
            if (_shiftStore.CurrentShiftState == ShiftState.None) return;

            FuelSale? globalLastSale = null;
            Nozzle? globalLastNozzle = null;

            foreach (var nozzle in Nozzles)
            {
                nozzle.SalesSum = await _fuelSaleService.GetReceivedQuantityAsync(
                    nozzle.Id, _shiftStore.CurrentShift.Id);

                var fuelSale = await _fuelSaleService.GetLastFuelSale(nozzle.Id);
                if (fuelSale is null) continue;

                nozzle.CurrentFuelSale = fuelSale;

                if (globalLastSale is null || fuelSale.CreateDate > globalLastSale.CreateDate)
                {
                    globalLastSale = fuelSale;
                    globalLastNozzle = nozzle;
                }
            }

            SelectedNozzle = globalLastNozzle ?? (Nozzles.Count > 0 ? Nozzles[0] : null);

            if (SelectedNozzle?.CurrentFuelSale is { } lastSale && IsFuelingCompleted(lastSale))
            {
                lastSale.ResumeBaseQuantity = lastSale.ReceivedQuantity;
                lastSale.ResumeBaseSum = lastSale.ReceivedSum;
            }
        }

        /// <summary>
        /// Добавляет новый пистолет к стороне, если он принадлежит данной стороне.
        /// </summary>
        private void NozzleService_OnCreated(Nozzle createdNozzle)
        {
            if (createdNozzle.Side == Side)
                Nozzles.Add(createdNozzle);
        }

        /// <summary>
        /// Обновляет данные существующего пистолета и синхронизирует номер стороны.
        /// </summary>
        private void NozzleService_OnUpdated(Nozzle updatedNozzle)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Id == updatedNozzle.Id);
            if (nozzle is null) return;

            nozzle.Update(updatedNozzle);
            Side = updatedNozzle.Side;
        }

        /// <summary>
        /// Удаляет пистолет и покидает его SignalR-группу, если пистолетов больше нет.
        /// </summary>
        private async void NozzleService_OnDeleted(int id)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Id == id);
            if (nozzle is null) return;

            Nozzles.Remove(nozzle);

            if (!Nozzles.Any())
                await _hub.InvokeAsync("LeaveController", nozzle.Group);
        }

        #endregion

        #region Hot Keys

        /// <summary>
        /// При нажатии цифровой клавиши: выбирает пистолет с соответствующим номером трубки,
        /// если заправка в данный момент не идёт.
        /// </summary>
        private void HotKeysService_OnNumberKeyPressed(int number)
        {
            var isBusy = Status switch
            {
                NozzleStatus.PumpWorking => true,
                NozzleStatus.WaitingStop => true,
                NozzleStatus.WaitingRemoved => true,
                _ => false
            };

            if (isBusy) return;
            if (Nozzles is null) return;

            var nozzle = Nozzles.FirstOrDefault(n => n.Tube == number);
            if (nozzle is null) return;

            SelectedNozzle = nozzle;
            _nozzleStore.SelectNozzle(number);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Совокупная проверка перед началом продажи: выбран пистолет, смена открыта,
        /// ККМ настроено и топлива достаточно.
        /// </summary>
        private async Task<bool> CanFuelSale(FuelSale fuelSale)
        {
            return ValidateNozzleSelection()
                && await ValidateShift()
                && ValidateCashRegisterShift()
                && await ValidateFuelQuantity();
        }

        /// <summary>
        /// Проверяет состояние смены: при необходимости предлагает открыть новую или закрыть превышенную.
        /// </summary>
        private async Task<bool> ValidateShift()
        {
            MessageResult result = MessageResult.None;

            if (_shiftStore.CurrentShift is null)
            {
                result = MessageBoxService.ShowMessage(
                    "Смена не открыта. Открыть новую смену?", "Внимание",
                    MessageButton.YesNo, MessageIcon.Question);
            }
            else
            {
                switch (_shiftStore.CurrentShiftState)
                {
                    case ShiftState.Closed:
                        result = MessageBoxService.ShowMessage(
                            "Смена закрыта. Открыть новую смену?", "Внимание",
                            MessageButton.YesNo, MessageIcon.Question);
                        break;

                    case ShiftState.Exceeded24Hours:
                        result = MessageBoxService.ShowMessage(
                            "Смена работает более 24 часов. Закрыть текущую и открыть новую?", "Внимание",
                            MessageButton.YesNo, MessageIcon.Question);
                        if (result == MessageResult.Yes)
                            await _shiftStore.CloseShiftAsync();
                        break;
                }
            }

            if (result == MessageResult.Yes)
                return await _shiftStore.OpenShiftAsync();

            return result != MessageResult.No;
        }

        /// <summary>
        /// Проверяет наличие настроенного ККМ.
        /// </summary>
        private bool ValidateCashRegisterShift()
        {
            if (_cashRegisterStore.CashRegister is not null) return true;

            MessageBoxService.ShowMessage(
                "ККМ не настроено. Проверьте настройки в Конфигураторе оборудования.",
                "Ошибка конфигурации!", MessageButton.OK, MessageIcon.Error);
            return false;
        }

        /// <summary>
        /// Проверяет, что в резервуаре достаточно топлива для выполнения запрошенной заправки.
        /// </summary>
        private async Task<bool> ValidateFuelQuantity()
        {
            var tanks = await _tankFuelQuantityView.GetAllAsync();
            var tank = tanks.First(t => t.Id == SelectedNozzle.TankId);
            var requestedQty = SelectedNozzle.CurrentFuelSale?.Quantity ?? 0m;

            if (tank.MinimumSize > 0)
            {
                if (tank.CurrentFuelQuantity - requestedQty <= tank.MinimumSize)
                {
                    MessageBoxService.ShowMessage(
                        "Недостаточно топлива в резервуаре с учетом мертвого остатка",
                        "Внимание!", MessageButton.OK, MessageIcon.Exclamation);
                    return false;
                }
            }
            else
            {
                if (tank.CurrentFuelQuantity - requestedQty < 0m)
                {
                    MessageBoxService.ShowMessage(
                        "Недостаточно топлива в резервуаре!",
                        "Внимание!", MessageButton.OK, MessageIcon.Exclamation);
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Проверяет, что пистолет выбран и находится в состоянии Ready.
        /// </summary>
        private bool ValidateNozzleSelection()
        {
            if (SelectedNozzle is null)
            {
                MessageBoxService.ShowMessage(
                    "Выберите ТРК!", "Внимание", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }

            if (SelectedNozzle.Status != NozzleStatus.Ready)
            {
                MessageBoxService.ShowMessage(
                    $"{SelectedNozzle.Name} занята или заблокирована.",
                    "Внимание", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }

            return true;
        }

        #endregion
    }
}
