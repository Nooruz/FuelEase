using FuelEase.Domain.Models;

namespace FuelEase.State.Discounts
{
    public interface IDisсountStore
    {
        bool IsActiveDiscount {  get; }

        DiscountSale? CalculateDiscount(FuelSale fuelSale, int fuelId, bool isUpdatingSum);
    }
}
