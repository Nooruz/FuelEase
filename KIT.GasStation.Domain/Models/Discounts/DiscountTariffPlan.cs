using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIT.GasStation.Domain.Models.Discounts
{
    /// <summary>
    /// Тарифный план для скидки
    /// </summary>
    public class DiscountTariffPlan : DomainObject
    {
        #region Private Members

        private int _discountId;
        private decimal _minimumValue;
        private decimal _maximumValue;
        private decimal _discountValue;

        #endregion

        #region Public Properties

        /// <summary>
        /// Минимальное значение
        /// </summary>
        public decimal MinimumValue
        {
            get => _minimumValue;
            set
            {
                _minimumValue = value;
                OnPropertyChanged(nameof(MinimumValue));
            }
        }

        /// <summary>
        /// Максимальное значение
        /// </summary>
        public decimal MaximumValue
        {
            get => _maximumValue;
            set
            {
                _maximumValue = value;
                OnPropertyChanged(nameof(MaximumValue));
            }
        }

        /// <summary>
        /// Скидка
        /// </summary>
        public decimal DiscountValue
        {
            get => _discountValue;
            set
            {
                _discountValue = value;
                OnPropertyChanged(nameof(DiscountValue));
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

        public Discount Discount { get; set; }

        #endregion

        #region Public Voids

        public override void Update(DomainObject updatedItem)
        {

        }

        #endregion
    }
}
