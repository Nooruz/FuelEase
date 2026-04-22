using KIT.GasStation.Domain.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.EntityFramework.Services.Common;

/// <summary>
/// Общий сервис для CRUD-операций над <see cref="Entity"/>.
/// Constraint изменён с DomainObject на Entity — доменные объекты не обязаны иметь WPF-зависимости.
/// </summary>
public class NonQueryDataService<T> where T : Entity
{
    private readonly GasStationDbContextFactory _contextFactory;

    public NonQueryDataService(GasStationDbContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<T?> Create(T entity)
    {
        try
        {
            await using var context = _contextFactory.CreateDbContext();
            var createdResult = await context.Set<T>().AddAsync(entity);
            await context.SaveChangesAsync();
            return createdResult.Entity;
        }
        catch (Exception)
        {
            // ignore
        }
        return null;
    }

    public async Task<T?> Update(int id, T entity)
    {
        try
        {
            await using var context = _contextFactory.CreateDbContext();
            entity.Id = id;
            context.Set<T>().Update(entity);
            var result = await context.SaveChangesAsync();
            if (result != 0)
                return entity;
        }
        catch (Exception)
        {
            // ignore
        }
        return null;
    }

    public async Task<bool> Delete(int id)
    {
        try
        {
            await using var context = _contextFactory.CreateDbContext();
            var entity = await context.Set<T>().FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null) return false;
            context.Set<T>().Remove(entity);
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception)
        {
            // ignore
        }
        return false;
    }
}
