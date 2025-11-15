using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;

namespace KIT.GasStation.Web.Services.Api
{
    public class ApiDataService<T> : IDataService<T> where T : DomainObject
    {
        private readonly HttpClient _httpClient;
        private readonly string _resourcePath;

        public event Action<T>? OnCreated;
        public event Action<T>? OnUpdated;
        public event Action<int>? OnDeleted;

        public ApiDataService(HttpClient httpClient, string resourcePath)
        {
            _httpClient = httpClient;
            _resourcePath = resourcePath.Trim('/');
        }

        public async Task<IEnumerable<T>> GetAllAsync() =>
           await _httpClient.GetFromJsonAsync<IEnumerable<T>>($"/api/{_resourcePath}")
           ?? Enumerable.Empty<T>();

        public async Task<T?> GetAsync(int id) =>
            await _httpClient.GetFromJsonAsync<T>($"/api/{_resourcePath}/{id}");

        public async Task<T?> CreateAsync(T entity)
        {
            var response = await _httpClient.PostAsJsonAsync($"/api/{_resourcePath}", entity);
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<T>();
            if (created != null)
            {
                OnCreated?.Invoke(created);
            }
            return created;
        }

        public async Task<T?> UpdateAsync(int id, T entity)
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/{_resourcePath}/{id}", entity);
            response.EnsureSuccessStatusCode();

            var updated = await response.Content.ReadFromJsonAsync<T>();
            if (updated != null)
            {
                OnUpdated?.Invoke(updated);
            }
            return updated;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"/api/{_resourcePath}/{id}");
            if (response.IsSuccessStatusCode)
            {
                OnDeleted?.Invoke(id);
                return true;
            }
            return false;
        }

    }
}
