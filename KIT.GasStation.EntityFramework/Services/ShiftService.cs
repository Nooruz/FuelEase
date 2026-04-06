using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.EntityFramework.Services
{
    public class ShiftService : IShiftService
    {
        private readonly GasStationDbContextFactory _contextFactory;
        private readonly NonQueryDataService<Shift> _nonQueryDataService;

        public ShiftService(GasStationDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<Shift>(_contextFactory);
        }

        public event Action<Shift> OnCreated;
        public event Action<Shift> OnUpdated;
        public event Action<int> OnDeleted;

        public async Task<Shift> CreateAsync(Shift entity)
        {
            await using var context = _contextFactory.CreateDbContext();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var year = entity.OpeningDate.Year;
                var periodKey = year.ToString();

                // Атомарно получаем следующий номер через DocumentCounters с блокировкой строки
                await context.Database.ExecuteSqlRawAsync(
                    @"MERGE INTO DocumentCounters WITH (HOLDLOCK) AS target
                      USING (SELECT @p0 AS DocumentType, @p1 AS PeriodKey) AS source
                      ON target.DocumentType = source.DocumentType AND target.PeriodKey = source.PeriodKey
                      WHEN MATCHED THEN UPDATE SET CurrentValue = target.CurrentValue + 1
                      WHEN NOT MATCHED THEN INSERT (DocumentType, PeriodKey, CurrentValue) 
                                           VALUES (source.DocumentType, source.PeriodKey, 1);",
                    "Shift", periodKey);

                // Считываем текущее значение счётчика (уже обновлённое)
                var counterValue = await context.DocumentCounters
                    .Where(c => c.DocumentType == "Shift" && c.PeriodKey == periodKey)
                    .Select(c => c.CurrentValue)
                    .FirstAsync();

                entity.Number = counterValue;
                entity.Year = year;

                await context.Shifts.AddAsync(entity);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                OnCreated?.Invoke(await GetAsync(entity.Id));
                return entity;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _nonQueryDataService.Delete(id);
            if (result)
            {
                OnDeleted?.Invoke(id);
            }
            return result;
        }

        public async Task<IEnumerable<Shift>> GetAllAsync()
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Shifts
                    .Include(s => s.User)
                    .Include(s => s.FuelSales)
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<IEnumerable<Shift>> GetAllAsync(int userId)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Shifts
                    .Where(s => s.UserId == userId)
                    .Include(s => s.FuelSales)
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<Shift> GetAsync(int id)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Shifts
                    .Include(s => s.User)
                    .Include(s => s.FuelSales)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<Shift> GetOpenShiftAsync()
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Shifts.Include(s => s.User).FirstOrDefaultAsync(s => s.ClosedDate == null);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<Shift> GetOpenShiftAsync(int userId)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Shifts.Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.OpeningDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<Shift> UpdateAsync(int id, Shift entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(await GetAsync(result.Id));
            return result;
        }
    }
}
