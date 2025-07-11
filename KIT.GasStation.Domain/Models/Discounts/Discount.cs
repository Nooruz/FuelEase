using KIT.GasStation.Domain.Models.Discounts;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KIT.GasStation.Domain.Models
{
    /// <summary>
    /// Скидка
    /// </summary>
    [Display(Name = "Скидка")]
    public class Discount : DomainObject
    {
        #region Private Members

        private string _name;
        private DateTime _startDate;
        private DateTime _endDate;
        private ObservableCollection<DiscountTariffPlan> _discountTariffPlans = new();
        private ObservableCollection<DiscountFuel> _discountFuels = new();

        #endregion

        #region Public Properties

        /// <summary>
        /// Наименование
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// Дата начало скидки
        /// </summary>
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged(nameof(StartDate));
            }
        }

        /// <summary>
        /// Дата окончании скидки
        /// </summary>
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged(nameof(EndDate));
            }
        }

        /// <summary>
        /// Тарифные планы
        /// </summary>
        public ObservableCollection<DiscountTariffPlan> DiscountTariffPlans
        {
            get => _discountTariffPlans;
            set
            {
                _discountTariffPlans = value;
                OnPropertyChanged(nameof(DiscountTariffPlans));
            }
        }

        /// <summary>
        /// Скидки на топливо
        /// </summary>
        public ObservableCollection<DiscountFuel> DiscountFuels
        {
            get => _discountFuels;
            set
            {
                _discountFuels = value;
                OnPropertyChanged(nameof(DiscountFuels));
            }
        }

        /// <summary>
        /// Продажи скидок
        /// </summary>
        public ICollection<DiscountSale> DiscountSales { get; set; }

        [NotMapped]
        public string Period => $"{StartDate:dd.MM.yyyy} - {EndDate:dd.MM.yyyy}";

        #endregion

        #region Public Voids

        public override void Update(DomainObject updatedItem)
        {
            if (updatedItem is Discount discount)
            {
                Name = discount.Name;
                StartDate = discount.StartDate;
                EndDate = discount.EndDate;
                OnPropertyChanged(nameof(Period));
            }
        }

        #endregion
    }
}
