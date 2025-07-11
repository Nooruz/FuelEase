namespace KIT.GasStation.Domain.Services
{
    public interface IDataService<T> 
    {
        Task<IEnumerable<T>> GetAllAsync();

        Task<T> GetAsync(int id);

        Task<T> CreateAsync(T entity);

        Task<T> UpdateAsync(int id, T entity);

        Task<bool> DeleteAsync(int id);

        event Action<T> OnCreated;
        event Action<T> OnUpdated;
        event Action<int> OnDeleted;
    }
}
