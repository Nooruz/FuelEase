namespace FuelEase.Domain.Models
{
    public class DiscountSale : DomainObject
    {
        #region Private Members

        private int _fuelSaleId;
        private int _discountId;
        private decimal _discountPrice;
        private decimal _discountSum;
        private decimal _discountQuantity;

        #endregion

        #region Public Properties

        public int FuelSaleId
        {
            get => _fuelSaleId;
            set
            {
                _fuelSaleId = value;
                OnPropertyChanged(nameof(FuelSaleId));
            }
        }

        /// <summary>
        /// Id скидки
        /// </summary>
        public int DiscountId
        {
            get => _discountId;
            set
            {
                _discountId = value;
                OnPropertyChanged(nameof(DiscountId));
            }
        }

        /// <summary>
        /// Скидочная цена
        /// </summary>
        public decimal DiscountPrice
        {
            get => _discountPrice;
            set
            {
                _discountPrice = value;
                OnPropertyChanged(nameof(DiscountPrice));
            }
        }

        /// <summary>
        /// Сумма скидки
        /// </summary>
        public decimal DiscountSum
        {
            get => _discountSum;
            set
            {
                _discountSum = value;
                OnPropertyChanged(nameof(DiscountSum));
            }
        }

        /// <summary>
        /// Количество скидки
        /// </summary>
        public decimal DiscountQuantity
        {
            get => _discountQuantity;
            set
            {
                _discountQuantity = value;
                OnPropertyChanged(nameof(DiscountQuantity));
            }
        }

        public FuelSale FuelSale { get; set; }

        /// <summary>
        /// Скидка
        /// </summary>
        public Discount Discount { get; set; }

        #endregion

        public override void Update(DomainObject updatedItem)
        {
            
        }
    }
}
