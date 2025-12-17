using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services
{
    public interface IFuelSaleService : IDataService<FuelSale>, IDisposable
    {
        event Action<FuelSale> OnResumeFueling;

        Task<FuelSale> GetFuelSaleWithPaymentType(int id);
        Task<decimal> GetReceivedQuantityAsync(int nozzleId, int shiftId);
        Task<FuelSale?> GetLastFuelSale(int nozzleId);
        Task<IEnumerable<FuelSale>> GetAllAsync(int shiftId);

        /// <summary>
        /// Получить незавершенные продажи
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<FuelSale>> GetUncompletedFuelSaleAsync(int shiftId);

        /// <summary>
        /// Получить завершенные продажи
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<FuelSale>> GetCompletedFuelSaleAsync(int shiftId);

        Task<FuelSale> GetForCompletionInfo(int id);

        Task<bool> DeleteAsync(IEnumerable<FuelSale> fuelSales);

        void ResumeFueling(FuelSale fuelSale);

        /// <summary>
        /// Ставим запись в очередь, не дожидаясь завершения
        /// </summary>
        /// <param name="sale"></param>
        /// <returns></returns>
        ValueTask EnqueueUpdateAsync(FuelSale sale);
    }
}
