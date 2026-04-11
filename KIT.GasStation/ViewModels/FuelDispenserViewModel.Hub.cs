using DevExpress.Mvvm;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Models;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels
{
    public partial class FuelDispenserViewModel
    {
        #region Hub Registration

        /// <summary>
        /// Подписывается на события соединения SignalR и регистрирует обработчики сообщений хаба.
        /// Защищена от повторного вызова через флаг <c>_hubHandlersRegistered</c>.
        /// </summary>
        private void RegisterHubHandlers()
        {
            if (_hubHandlersRegistered || _hub is null) return;

            _hub.Reconnecting += OnHubReconnecting;
            _hub.Reconnected += OnHubReconnected;
            _hub.Closed += OnHubClosed;
            _hubHandlersRegistered = true;

            _hubSubscriptions.Add(_hub.On<StatusResponse>("StatusChanged", OnStatusChanged));
            _hubSubscriptions.Add(_hub.On<string, bool>("ColumnLiftedChanged", OnColumnLifted));
            _hubSubscriptions.Add(_hub.On<WorkerStateNotification>("WorkerStateChanged",
                n => WorkerStateChanged(n)));
            _hubSubscriptions.Add(_hub.On<string, List<CounterData>>("CountersUpdated",
                (groupName, data) => CountersUpdated(data)));
            _hubSubscriptions.Add(_hub.On<CounterData>("CounterUpdated",
                data => CounterUpdated(data)));
            _hubSubscriptions.Add(_hub.On<string, decimal?>("CompletedFuelingAsync",
                (g, q) => CompletedFuelingAsync(g, q)));
            _hubSubscriptions.Add(_hub.On<string>("WaitingAsync",
                g => WaitingAsync(g)));
            _hubSubscriptions.Add(_hub.On<FuelingResponse>("PumpStopAsync",
                r => PumpStopAsync(r)));
            _hubSubscriptions.Add(_hub.On<FuelingResponse>("FuelingAsync", FuelingAsync));
        }

        /// <summary>
        /// Отписывается от всех событий SignalR и освобождает подписки на сообщения хаба.
        /// </summary>
        private void UnregisterHubHandlers()
        {
            if (_hub is null || !_hubHandlersRegistered) return;

            _hub.Reconnecting -= OnHubReconnecting;
            _hub.Reconnected -= OnHubReconnected;
            _hub.Closed -= OnHubClosed;

            foreach (var sub in _hubSubscriptions)
                sub.Dispose();

            _hubSubscriptions.Clear();
            _hubHandlersRegistered = false;
        }

        #endregion

        #region Hub Connection Events

        private Task OnHubReconnecting(Exception? error)
        {
            OnConnectionLost();
            MarkAllWorkersOffline();
            return Task.CompletedTask;
        }

        private async Task OnHubReconnected(string? connectionId)
        {
            Interlocked.Exchange(ref _connectionLostHandled, 0);
            await JoinAndSubscribeAsync();
        }

        private Task OnHubClosed(Exception? error)
        {
            OnConnectionLost();
            MarkAllWorkersOffline();
            return RestartHubConnectionLoopAsync();
        }

        /// <summary>
        /// Запускает фоновый цикл повторного подключения к SignalR.
        /// Гарантирует только одну запущенную копию через Interlocked.
        /// </summary>
        private Task RestartHubConnectionLoopAsync()
        {
            if (_hub is null) return Task.CompletedTask;
            if (Interlocked.CompareExchange(ref _hubReconnectLoop, 1, 0) != 0) return Task.CompletedTask;

            return Task.Run(async () =>
            {
                try
                {
                    while (_hub.State != HubConnectionState.Connected)
                    {
                        try
                        {
                            await _hubClient.EnsureStartedAsync();
                            await JoinAndSubscribeAsync();
                            break;
                        }
                        catch
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _hubReconnectLoop, 0);
                }
            });
        }

        #endregion

        #region Join and Subscribe

        /// <summary>
        /// Присоединяется ко всем группам пистолетов, инициализирует конфигурацию ТРК,
        /// отправляет цены и запрашивает текущие счётчики.
        /// </summary>
        private async Task JoinAndSubscribeAsync()
        {
            if (_hub is null || Nozzles is null || Nozzles.Count == 0) return;

            var groups = Nozzles
                .Select(n => n.Group)
                .Where(g => !string.IsNullOrWhiteSpace(g))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            foreach (var group in groups)
                await _hub.InvokeAsync("JoinController", group, false);

            var first = Nozzles.FirstOrDefault();
            if (first is not null)
            {
                await Task.Delay(2000);
                await _hub.InvokeAsync("InitializeConfigurationAsync", first.Group);

                var prices = Nozzles.Select(n => new PriceRequest
                {
                    GroupName = n.Group,
                    Value = n.Tank.Fuel.Price
                }).ToList();

                await _hub.InvokeAsync("SetPricesAsync", prices);
                await _hub.InvokeAsync("GetCountersAsync", first.Group);
            }

            await RequestWorkerStateSnapshotAsync(groups);
        }

        #endregion

        #region Worker State

        /// <summary>
        /// Переводит все пистолеты в состояние Unknown при обрыве связи.
        /// </summary>
        private void MarkAllWorkersOffline()
        {
            if (Nozzles is null) return;

            var now = DateTimeOffset.Now;
            foreach (var nozzle in Nozzles)
            {
                nozzle.Status = NozzleStatus.Unknown;
                nozzle.Lifted = false;
                nozzle.WorkerStateMessage = WorkerOfflineDueToHubMessage;
                nozzle.WorkerStateUpdatedAt = now;
            }
        }

        /// <summary>
        /// Запрашивает снимок состояния воркеров при подключении/переподключении.
        /// Ошибка намеренно проглатывается: события догонят актуальное состояние.
        /// </summary>
        private async Task RequestWorkerStateSnapshotAsync(string[] groups)
        {
            if (_hub is null || groups is null || groups.Length == 0) return;

            try
            {
                var snapshot = await _hub.InvokeAsync<IReadOnlyCollection<WorkerStateNotification>>(
                    "GetWorkerStatesSnapshot", groups);
                await ApplyWorkerStates(snapshot);
            }
            catch
            {
                // Намеренно: потеря снимка не критична — события SignalR догонят позже
            }
        }

        private async Task ApplyWorkerStates(IEnumerable<WorkerStateNotification>? states)
        {
            if (states is null) return;
            foreach (var state in states)
                await ApplyWorkerState(state);
        }

        private Task WorkerStateChanged(WorkerStateNotification? notification)
            => ApplyWorkerState(notification);

        private Task ApplyWorkerState(WorkerStateNotification? notification)
        {
            if (notification is null || Nozzles is null) return Task.CompletedTask;

            var nozzle = Nozzles.FirstOrDefault(n => n.Group == notification.GroupName);
            if (nozzle is null) return Task.CompletedTask;

            if (!notification.IsOnline)
            {
                nozzle.Status = NozzleStatus.Unknown;
                nozzle.Lifted = false;
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Hub Message Handlers

        /// <summary>
        /// Обновляет статус пистолета при получении события StatusChanged.
        /// Защищает незавершённую продажу: не сбрасывает PumpStop → Ready до завершения отпуска.
        /// </summary>
        private void OnStatusChanged(StatusResponse response)
        {
            var currentNozzle = Nozzles.FirstOrDefault(n => n.Group == response.GroupName);
            if (currentNozzle is null) return;

            var newStatus = response.Status;
            Status = newStatus;

            if (newStatus == NozzleStatus.Unknown) return;

            // Защита незавершённой продажи — не даём сбросить PumpStop в Ready
            bool isProtected = false;
            if (SelectedNozzle?.CurrentFuelSale is { FuelSaleStatus: FuelSaleStatus.Uncompleted } sale
                && !IsFuelingCompleted(sale))
            {
                SelectedNozzle.Status = NozzleStatus.PumpStop;
                sale.ResumeBaseQuantity = sale.ReceivedQuantity;
                sale.ResumeBaseSum = sale.ReceivedSum;
                isProtected = true;
            }

            if (!isProtected || currentNozzle.Id != SelectedNozzle?.Id)
                currentNozzle.Status = newStatus;

            foreach (var n in Nozzles)
            {
                if (n.Id == currentNozzle.Id) continue;
                if (isProtected && n.Id == SelectedNozzle?.Id) continue;
                n.Status = NozzleStatus.Ready;
            }

            if (newStatus != NozzleStatus.Ready)
                SelectedNozzle = currentNozzle;
        }

        /// <summary>
        /// Обновляет счётчики в реальном времени во время заправки.
        /// При каждом получении данных сохраняет FuelSale в БД, чтобы не потерять данные.
        /// </summary>
        private async void FuelingAsync(FuelingResponse response)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == response.GroupName);
            if (nozzle is null) return;

            if (nozzle.Status != NozzleStatus.PumpWorking)
                nozzle.Status = NozzleStatus.PumpWorking;

            if (response.Quantity == 0m && response.Sum == 0m) return;

            EnrichResponse(response, nozzle);
            ReceivedQuantity = response.Quantity;
            ReceivedSum = response.Sum;

            // Сохраняем промежуточные данные в БД, чтобы не потерять при сбое
            var sale = nozzle.CurrentFuelSale;
            if (sale is not null)
            {
                try
                {
                    await _fuelSaleService.EnqueueUpdateAsync(sale);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось сохранить промежуточные данные продажи {SaleId}", sale.Id);
                }
            }
        }

        /// <summary>
        /// Завершает цикл заправки: вычисляет недостающий показатель и вызывает CompleteFuelingAsync.
        /// </summary>
        private async Task PumpStopAsync(FuelingResponse response)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == response.GroupName);
            if (nozzle is null) return;

            if (response.Quantity == 0m && response.Sum == 0m) return;

            EnrichResponse(response, nozzle);
            ReceivedQuantity = response.Quantity;
            ReceivedSum = response.Sum;

            await _hub.InvokeAsync("CompleteFuelingAsync", nozzle.Group);
        }

        /// <summary>
        /// Фиксирует незавершённую заправку (пистолет опущен до завершения):
        /// сохраняет базовые показатели для последующего возобновления.
        /// </summary>
        private async Task WaitingAsync(string groupName)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == groupName);
            if (nozzle is null) return;

            var sale = nozzle.CurrentFuelSale;
            if (sale is null) return;

            sale.FuelSaleStatus = FuelSaleStatus.Uncompleted;
            sale.ResumeBaseQuantity = sale.ReceivedQuantity;
            sale.ResumeBaseSum = sale.ReceivedSum;

            await _fuelSaleService.UpdateAsync(sale.Id, sale);
        }

        /// <summary>
        /// Завершает продажу после получения сигнала от сервера: выполняет фискализацию и обновляет запись.
        /// Если фискализация не удалась — выводит сообщение, но всё равно сохраняет статус Completed.
        /// </summary>
        private async Task CompletedFuelingAsync(string groupName, decimal? quantity)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == groupName);
            if (nozzle is null) return;

            var sale = nozzle.CurrentFuelSale;
            if (sale is null) return;

            try
            {
                sale.FuelSaleStatus = FuelSaleStatus.Completed;

                // Сохраняем статус Completed в любом случае
                await _fuelSaleService.UpdateAsync(sale.Id, sale);

                // Пробуем выполнить фискализацию (ККМ чек)
                try
                {
                    await HandleFiscalizationAsync(sale, nozzle);
                    await _fuelSaleService.UpdateAsync(sale.Id, sale);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка фискализации (ККМ) для продажи {SaleId}. Продажа будет сохранена без чека.", sale.Id);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBoxService.ShowMessage(
                            "Продажа завершена, но произошла ошибка при фискализации (ККМ). Продажа будет сохранена без чека.\n\n" +
                            $"Ошибка: {ex.Message}", "Ошибка фискализации",
                            MessageButton.OK, MessageIcon.Error);
                    });
                }

                _logger.LogInformation("Продажа {SaleId} завершена", sale.Id);
                await _hub.InvokeAsync("GetCounterAsync", nozzle.Group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при завершении заправки для продажи {SaleId}", sale.Id);
            }
        }

        /// <summary>
        /// Помечает все пистолеты как Unknown при разрыве соединения.
        /// Использует Interlocked для предотвращения дублирования обработки.
        /// </summary>
        private void OnConnectionLost()
        {
            if (Interlocked.CompareExchange(ref _connectionLostHandled, 1, 0) != 0) return;

            try
            {
                if (Nozzles is not null)
                {
                    foreach (var nozzle in Nozzles)
                        nozzle.Status = NozzleStatus.Unknown;
                }
            }
            finally
            {
                Task.Delay(5000).ContinueWith(_ => Interlocked.Exchange(ref _connectionLostHandled, 0));
            }
        }

        /// <summary>
        /// Обновляет флаг поднятия пистолета. При опускании сбрасывает все пистолеты данной стороны.
        /// </summary>
        private void OnColumnLifted(string groupName, bool isLifted)
        {
            var nozzle = Nozzles.FirstOrDefault(n => n.Group == groupName);
            if (nozzle is null) return;

            if (isLifted)
            {
                nozzle.Lifted = true;
            }
            else
            {
                foreach (var n in Nozzles)
                    n.Lifted = false;
            }
        }

        #endregion

        #region Fiscalization

        /// <summary>
        /// Определяет сценарий фискализации: чек после заправки (режим "После")
        /// или возврат/приход при недоливе.
        /// </summary>
        private async Task HandleFiscalizationAsync(FuelSale sale, Nozzle nozzle)
        {
            if (Properties.Settings.Default.ReceiptPrintingMode == "After")
            {
                await EnsureSaleCheckAsync(sale, nozzle);
                return;
            }

            var isUnderfilling = sale.ReceivedQuantity + 0.0005m < sale.Quantity;
            if (!isUnderfilling) return;

            if (sale.PaymentType is not (PaymentType.Cash or PaymentType.Cashless)) return;

            await EnsureReturnAndReceivedAsync(sale, nozzle);
        }

        /// <summary>
        /// Печатает чек продажи на фактически отпущенное топливо (режим "После").
        /// </summary>
        private async Task EnsureSaleCheckAsync(FuelSale sale, Nozzle nozzle)
        {
            if (sale.Tank?.Fuel is null)
            {
                MessageBoxService.ShowMessage("Не найдено топливо для продажи.", "Ошибка",
                    MessageButton.OK, MessageIcon.Error);
                return;
            }

            var createdFiscalData = sale.AfterCreateFiscalData(OperationType.Sale);
            var fiscalData = await _cashRegisterStore.SaleAsync(createdFiscalData);

            if (fiscalData is null)
            {
                MessageBoxService.ShowMessage("Не удалось получить фискальные данные от ККМ.", "Ошибка",
                    MessageButton.OK, MessageIcon.Error);
                return;
            }

            fiscalData = sale.UpdateFiscalData(fiscalData, nozzle);
            await _fiscalDataService.CreateAsync(fiscalData);
        }

        /// <summary>
        /// При недоливе выполняет возврат исходного чека и создаёт новый на фактический объём.
        /// </summary>
        private async Task EnsureReturnAndReceivedAsync(FuelSale sale, Nozzle nozzle)
        {
            var fuel = SelectedNozzle?.Tank?.Fuel ?? sale.Tank?.Fuel;
            if (fuel is null)
            {
                MessageBoxService.ShowMessage("Не найдено топливо для возврата/прихода.", "Ошибка",
                    MessageButton.OK, MessageIcon.Error);
                return;
            }

            var originalFiscalData = sale.FiscalDatas?.FirstOrDefault(fd => fd.OperationType == OperationType.Sale);
            if (originalFiscalData is null) return;

            var returnFiscalData = sale.CreateReturnFiscalData(nozzle, originalFiscalData);
            var returnedFiscalData = await _cashRegisterStore.ReturnAsync(returnFiscalData);
            await _fiscalDataService.CreateAsync(returnedFiscalData);

            if (sale.ReceivedQuantity > 0)
            {
                var createFiscalData = sale.AfterCreateFiscalData(OperationType.Sale);
                var createdFiscalData = await _cashRegisterStore.SaleAsync(createFiscalData);
                await _fiscalDataService.CreateAsync(createdFiscalData);
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Вычисляет недостающий показатель (сумму или объём) из ответа ТРК.
        /// Если оба нулевые — ничего не делает.
        /// </summary>
        private static void EnrichResponse(FuelingResponse response, Nozzle nozzle)
        {
            if (response.Quantity != 0m && response.Sum == 0m)
            {
                response.Sum = response.Quantity * nozzle.Price;
            }
            else if (response.Sum != 0m && response.Quantity == 0m && nozzle.Price > 0m)
            {
                response.Quantity = response.Sum / nozzle.Price;
            }
        }

        #endregion
    }
}
