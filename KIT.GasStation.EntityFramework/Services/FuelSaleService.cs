using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Models.CashRegisters;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace KIT.GasStation.EntityFramework.Services
{
    public class FuelSaleService : IFuelSaleService
    {
        #region Private Members

        // Канал для буферизации запросов на обновление
        private readonly Channel<FuelSale> _updateChannel = Channel.CreateUnbounded<FuelSale>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );
        private readonly Task _processorTask;
        private GasStationDbContextFactory _contextFactory;
        private readonly NonQueryDataService<FuelSale> _nonQueryFuelSaleDataService;
        private readonly NonQueryDataService<FiscalData> _nonQueryFiscalDataDataService;

        #endregion

        #region Constructors

        public FuelSaleService(GasStationDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryFuelSaleDataService = new NonQueryDataService<FuelSale>(_contextFactory);
            _nonQueryFiscalDataDataService = new NonQueryDataService<FiscalData>(_contextFactory);

            // Запускаем фоновый процесс «потребления» канала
            _processorTask = Task.Run(ProcessUpdateQueueAsync);
        }

        #endregion

        #region Events

        public event Action<FuelSale> OnCreated;
        public event Action<FuelSale> OnUpdated;
        public event Action<int> OnDeleted;
        public event Action<FuelSale> OnResumeFueling;

        #endregion

        #region Public Voids

        public ValueTask EnqueueUpdateAsync(FuelSale sale)
        {
            // Всегда пишем текущее состояние
            _updateChannel.Writer.TryWrite(sale);

            // Если статус Completed — больше не ждём новых обновлений, «дожимаем» и закрываем канал
            if (sale.FuelSaleStatus == FuelSaleStatus.Completed)
            {
                _updateChannel.Writer.Complete();
            }

            return default;
        }

        public async Task<FuelSale> CreateAsync(FuelSale fuelSale)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                await using var transaction = await context.Database.BeginTransactionAsync();

                try
                {
                    var periodKey = $"SHIFT-{fuelSale.ShiftId}";

                    // Атомарно получаем следующий номер через DocumentCounters с блокировкой строки
                    await context.Database.ExecuteSqlRawAsync(
                        @"MERGE INTO DocumentCounters WITH (HOLDLOCK) AS target
                          USING (SELECT @p0 AS DocumentType, @p1 AS PeriodKey) AS source
                          ON target.DocumentType = source.DocumentType AND target.PeriodKey = source.PeriodKey
                          WHEN MATCHED THEN UPDATE SET CurrentValue = target.CurrentValue + 1
                          WHEN NOT MATCHED THEN INSERT (DocumentType, PeriodKey, CurrentValue) 
                                               VALUES (source.DocumentType, source.PeriodKey, 1);",
                        "FuelSale", periodKey);

                    // Считываем текущее значение счётчика (уже обновлённое)
                    var counterValue = await context.DocumentCounters
                        .Where(c => c.DocumentType == "FuelSale" && c.PeriodKey == periodKey)
                        .Select(c => c.CurrentValue)
                        .FirstAsync();

                    fuelSale.Number = counterValue;
                    fuelSale.Tank = null;

                    await context.FuelSales.AddAsync(fuelSale);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    OnCreated?.Invoke(await GetFuelSaleWithPaymentType(fuelSale.Id));
                    return fuelSale;
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var result = await _nonQueryFuelSaleDataService.Delete(id);
                if (result)
                {
                    OnDeleted?.Invoke(id);
                }
                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(IEnumerable<FuelSale> fuelSales)
        {
            try
            {
                bool result = false;
                foreach (var item in fuelSales)
                {
                    if (item.ReceivedQuantity == 0)
                    {
                        result = await _nonQueryFuelSaleDataService.Delete(item.Id);
                        if (result)
                        {
                            OnDeleted?.Invoke(item.Id);
                        }
                    }
                }
                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<IEnumerable<FuelSale>> GetAllAsync()
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.FuelSales
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
                return null;
            }
        }

        public async Task<IEnumerable<FuelSale>> GetAllAsync(int shiftId)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.FuelSales
                    .Where(f => f.ShiftId == shiftId)
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
                return null;
            }
        }

        public async Task<FuelSale> GetAsync(int id)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.FuelSales
                    .Include(f => f.Tank)
                    .ThenInclude(t => t.Fuel)
                    .FirstOrDefaultAsync(f => f.Id == id);
            }
            catch (Exception)
            {
                //ignore
                return null;
            }
        }

        public async Task<FuelSale> GetFuelSaleWithPaymentType(int id)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.FuelSales
                    .Include(f => f.DiscountSale)
                    .Include(f => f.FiscalDatas)
                    .Include(f => f.Tank)
                    .ThenInclude(t => t.Fuel)
                    .ThenInclude(f => f.UnitOfMeasurement)
                    .FirstOrDefaultAsync(f => f.Id == id);
            }
            catch (Exception)
            {
                //ignore
                return null;
            }
        }

        public async Task<FuelSale> UpdateAsync(int id, FuelSale entity)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                var result = await _nonQueryFuelSaleDataService.Update(id, entity);
                if (result != null)
                {
                    var full = await GetFuelSaleWithPaymentType(result.Id);
                    OnUpdated?.Invoke(full);
                }
                return result;
            }
            catch (Exception)
            {
                //ignore
                return null;
            }
        }

        public async Task<decimal> GetReceivedQuantityAsync(int nozzleId, int shiftId)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.FuelSales
                    .Where(f => f.NozzleId == nozzleId && f.ShiftId == shiftId && f.FuelSaleStatus != FuelSaleStatus.None)
                    .SumAsync(f => f.ReceivedQuantity);
            }
            catch (Exception)
            {
                return 0;
                //ignore
            }
        }

        public async Task<FuelSale?> GetLastFuelSale(int nozzleId)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.FuelSales.Where(f => f.NozzleId == nozzleId)
                    .Include(f => f.Nozzle)
                    .Include(f => f.FiscalDatas)
                    .Include(f => f.DiscountSale)
                    .Include(f => f.Tank)
                    .ThenInclude(t => t.Fuel)
                    .ThenInclude(t => t.UnitOfMeasurement)
                    .OrderByDescending(f => f.Id)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                return null;
                //ignore
            }
        }

        public async Task<IEnumerable<FuelSale>> GetUncompletedFuelSaleAsync(int shiftId)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.FuelSales.Where(f => f.ShiftId == shiftId && f.FuelSaleStatus != FuelSaleStatus.Completed)
                    .Include(f => f.Nozzle)
                    .Include(f => f.FiscalDatas)
                    .ThenInclude(t => t.Discount)
                    .Include(f => f.Tank)
                    .ThenInclude(t => t.Fuel)
                    .ThenInclude(f => f.UnitOfMeasurement)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<FuelSale?> GetUncompletedFuelSaleAsync(int nozzleId, int shiftId)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.FuelSales.Where(f => f.NozzleId == nozzleId && f.ShiftId == shiftId && f.FuelSaleStatus != FuelSaleStatus.Completed)
                    .Include(f => f.Nozzle)
                    .Include(f => f.FiscalDatas)
                    .Include(f => f.Tank)
                    .ThenInclude(t => t.Fuel)
                    .ThenInclude(f => f.UnitOfMeasurement)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<IEnumerable<FuelSale>> GetCompletedFuelSaleAsync(int shiftId)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.FuelSales.Where(f => f.ShiftId == shiftId && f.FuelSaleStatus != FuelSaleStatus.Uncompleted)
                    .Include(f => f.Nozzle)
                    .Include(f => f.FiscalDatas)
                    .ThenInclude(s => s.Discount)
                    .Include(f => f.DiscountSale)
                    .Include(f => f.Shift)
                    .ThenInclude(s => s.User)
                    .Include(f => f.Tank)
                    .ThenInclude(t => t.Fuel)
                    .ThenInclude(t => t.UnitOfMeasurement)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<FuelSale> GetForCompletionInfo(int id)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.FuelSales.Where(f => f.Id == id)
                    .Include(f => f.Tank)
                    .ThenInclude(t => t.Fuel)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                return null;
                //ignore
            }
        }

        public void ResumeFueling(FuelSale fuelSale)
        {
            OnResumeFueling?.Invoke(fuelSale);
        }

        public void Dispose()
        {
            _updateChannel.Writer.Complete();
            _processorTask.Wait(); // или await, если позволено
        }

        #endregion

        #region Private Voids

        private async Task ProcessUpdateQueueAsync()
        {
            var reader = _updateChannel.Reader;
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var sale))
                {
                    try
                    {
                        // здесь действительно вызываем UpdateAsync
                        await UpdateAsync(sale.Id, sale);
                    }
                    catch (Exception)
                    {
                        // логируем ex, или помещаем обратно в канал
                    }
                }
            }
        }

        #endregion
    }
}
