using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FuelEase.Domain.Models
{
    /// <summary>
    /// Смена
    /// </summary>
    public class Shift : DomainObject
    {
        #region Private Members

        private int _userId;
        private DateTime _openingDate;
        private DateTime? _closingDate;

        #endregion

        #region Public Properties

        /// <summary>
        /// Id пользователя
        /// </summary>
        public int UserId
        {
            get => _userId;
            set
            {
                _userId = value;
                OnPropertyChanged(nameof(UserId));
            }
        }

        /// <summary>
        /// Дата открытия
        /// </summary>
        public DateTime OpeningDate
        {
            get => _openingDate;
            set
            {
                _openingDate = value;
                OnPropertyChanged(nameof(OpeningDate));
            }
        }

        /// <summary>
        /// Дата закрытия
        /// </summary>
        public DateTime? ClosedDate
        {
            get => _closingDate;
            set
            {
                _closingDate = value;
                OnPropertyChanged(nameof(ClosedDate));
            }
        }

        /// <summary>
        /// Пользователь
        /// </summary>
        public User User { get; set; }

        /// <summary>
        /// Продажи топлива
        /// </summary>
        public ICollection<FuelSale> FuelSales { get; set; }
        public ICollection<UnregisteredSale> UnregisteredSales { get; set; }
        public ICollection<EventPanel> EventsPanel { get; set; }
        public ICollection<ShiftCounter> NozzleShiftCounters { get; set; }
        public ICollection<TankShiftCounter> TankShiftCounters { get; set; }
        public ICollection<FuelIntake> FuelIntakes { get; set; }


        [NotMapped]
        public ShiftState ShiftState
        {
            get
            {
                if (Id == 0)
                {
                    return ShiftState.None;
                }
                if (ClosedDate == null)
                {
                    if ((DateTime.Now - OpeningDate).TotalHours < 24)
                    {
                        return ShiftState.Open;
                    }
                    else
                    {
                        return ShiftState.Exceeded24Hours;
                    }
                }
                else
                {
                    return ShiftState.Closed;
                };
            }
        }

        [NotMapped]
        public decimal Sum
        {
            get
            {
                if (FuelSales != null && FuelSales.Count != 0)
                {
                    return FuelSales
                        .Where(f => f.FuelSaleStatus == FuelSaleStatus.Completed)
                        .Sum(f => f.ReceivedSum);
                }
                return 0;
            }
        }

        #endregion

        #region Public Voids

        public void SetUpdates(Shift shift)
        {
            ClosedDate = shift.ClosedDate;
            FuelSales = shift.FuelSales;
            OnPropertyChanged(nameof(ShiftState));
            OnPropertyChanged(nameof(Sum));
        }

        public override void Update(DomainObject updatedItem)
        {

        }

        #endregion
    }

    /// <summary>
    /// Состояние смены
    /// </summary>
    public enum ShiftState
    {
        None,

        /// <summary>
        /// Смена открыта
        /// </summary>
        [Display(Name = "Открыта")]
        Open,

        /// <summary>
        /// Смена закрыта
        /// </summary>
        [Display(Name = "Закрыта")]
        Closed,

        /// <summary>
        /// Смена превысила 24 часа
        /// </summary>
        [Display(Name = "Превысила 24 часа")]
        Exceeded24Hours
    }
}
