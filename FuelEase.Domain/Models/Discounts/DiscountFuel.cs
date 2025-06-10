namespace FuelEase.Domain.Models.Discounts
{
    public class DiscountFuel
    {
        public int DiscountId { get; set; }
        public Discount Discount {  get; set; }


        public int FuelId { get; set; }
        public Fuel Fuel { get; set; }
    }
}
