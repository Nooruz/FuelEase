using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class UserService : IUserService
    {
        private readonly FuelEaseDbContextFactory _contextFactory;
        private readonly NonQueryDataService<User> _nonQueryDataService;

        public UserService(FuelEaseDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<User>(_contextFactory);
        }

        public event Action<User> OnCreated;
        public event Action<User> OnUpdated;
        public event Action<int> OnDeleted;

        public Task<bool> AnyAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<User> CreateAsync(User entity)
        {
            var result = await _nonQueryDataService.Create(entity);
            if (result != null)
            {
                OnCreated?.Invoke(await GetAsync(result.Id));
            }
            return result;
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

        public Task<IEnumerable<User>> GetAdminAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Users
                    .Include(u => u.UserRole)
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<User> GetAsync(int id)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Users.Include(u => u.UserRole).FirstOrDefaultAsync(u => u.Id == id);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<User> GetByUsername(string username)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Users.FirstOrDefaultAsync(u => u.FullName == username);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public IEnumerable<User> GetCashiers()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<User>> GetCashiersAsync()
        {
            throw new NotImplementedException();
        }

        public Task<bool> MarkingForDeletion(User user)
        {
            throw new NotImplementedException();
        }

        public async Task<User> UpdateAsync(int id, User entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(await GetAsync(result.Id));
            return result;
        }
    }
}
