using KIT.GasStation.Domain.Models.Discounts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;

namespace KIT.GasStation.Domain.Models
{
    /// <summary>
    /// Топлива
    /// </summary>
    [Display(Name = "Топлива")]
    public class Fuel : DomainObject
    {
        #region Private Members

        private string _name;
        private decimal _price;
        private int _unitOfMeasurementId;
        private string? _tnved;
        private bool _deleted;
        private bool _valueAddedTax;
        private decimal _salesTax;
        private string _colorHex;
        private bool _isDeleted;

        #endregion

        #region Constructors

        public Fuel()
        {
            
        }

        public Fuel(Fuel fuel)
        {
            Id = fuel.Id;
            Name = fuel.Name;
            Price = fuel.Price;
            UnitOfMeasurementId = fuel.UnitOfMeasurementId;
            TNVED = fuel.TNVED;
            Deleted = fuel.Deleted;
            ValueAddedTax = fuel.ValueAddedTax;
            SalesTax = fuel.SalesTax;
            ColorHex = fuel.ColorHex;
        }

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
        /// Цена
        /// </summary>
        public decimal Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
            }
        }

        /// <summary>
        /// Единица измерения код
        /// </summary>
        public int UnitOfMeasurementId
        {
            get => _unitOfMeasurementId;
            set
            {
                _unitOfMeasurementId = value;
                OnPropertyChanged(nameof(UnitOfMeasurementId));
            }
        }

        /// <summary>
        /// ТНВЭД
        /// </summary>
        public string? TNVED
        {
            get => _tnved;
            set
            {
                _tnved = value;
                OnPropertyChanged(nameof(TNVED));
            }
        }

        public bool Deleted
        {
            get => _deleted;
            set
            {
                _deleted = value;
                OnPropertyChanged(nameof(Deleted));
            }
        }

        /// <summary>
        /// Налоги НДС
        /// </summary>
        public bool ValueAddedTax
        {
            get => _valueAddedTax;
            set
            {
                _valueAddedTax = value;
                OnPropertyChanged(nameof(ValueAddedTax));
            }
        }

        /// <summary>
        /// Налог с продаж НСП
        /// </summary>
        public decimal SalesTax
        {
            get => _salesTax;
            set
            {
                _salesTax = value;
                OnPropertyChanged(nameof(SalesTax));
            }
        }

        /// <summary>
        /// Цвет топлива
        /// </summary>
        public string ColorHex
        {
            get => _colorHex;
            set
            {
                _colorHex = value;
                OnPropertyChanged(nameof(ColorHex));
                OnPropertyChanged(nameof(ColorBrush));
            }
        }

        /// <summary>
        /// Признак удаления
        /// </summary>
        public bool IsDeleted
        {
            get => _isDeleted;
            set
            {
                _isDeleted = value;
                OnPropertyChanged(nameof(IsDeleted));
            }
        }

        /// <summary>
        /// Создано в
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Редактировано в
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Удалено в
        /// </summary>
        public DateTime? DeletedAt { get; set; }

        public UnitOfMeasurement UnitOfMeasurement { get; set; }
        public ICollection<Tank> Tanks { get; set; }
        public ICollection<FuelRevaluation> FuelRevaluations { get; set; }
        public ICollection<DiscountFuel> DiscountFuels { get; set; }

        public override void Update(DomainObject updatedItem)
        {
            if (updatedItem is Fuel fuel)
            {
                Name = fuel.Name;
                Price = fuel.Price;
                Tanks = fuel.Tanks;
                UnitOfMeasurementId = fuel.UnitOfMeasurementId;
                Deleted = fuel.Deleted;
                ValueAddedTax = fuel.ValueAddedTax;
                SalesTax = fuel.SalesTax;
                TNVED = fuel.TNVED;
                ColorHex = fuel.ColorHex;
                IsDeleted = fuel.IsDeleted;
                CreatedAt = fuel.CreatedAt;
                UpdatedAt = fuel.UpdatedAt;
                DeletedAt = fuel.DeletedAt;
            }
        }

        #endregion

        #region NotMapped

        [NotMapped]
        public Color ColorBrush
        {
            get => ColorTranslator.FromHtml(ColorHex);
            set => ColorHex = ColorToHex(value);
        }

        #endregion

        #region Private Voids

        private static string ColorToHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        #endregion
    }
}