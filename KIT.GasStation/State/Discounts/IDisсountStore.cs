using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.State.Discounts
{
    public interface IDisсountStore
    {
        bool IsActiveDiscount {  get; }

        DiscountSale? CalculateDiscount(FuelSale fuelSale, int fuelId, bool isUpdatingSum);
    }
}
