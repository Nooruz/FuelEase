using System.ComponentModel.DataAnnotations;

namespace FuelEase.Domain.Models
{
    /// <summary>
    /// Резервуар
    /// </summary>
    [Display(Name = "Резервуар")]
    public class Tank : DomainObject
    {
        #region Private Members

        private string _name;
        private int _number;
        private int _fuelId;
        private decimal _size;
        private decimal _minimumSize;
        private Fuel _fuel;
        private bool _deleted;

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
        /// Код резервуара
        /// </summary>
        public int Number
        {
            get => _number;
            set
            {
                _number = value;
                OnPropertyChanged(nameof(Number));
            }
        }

        /// <summary>
        /// Код топливы
        /// </summary>
        public int FuelId
        {
            get => _fuelId;
            set
            {
                _fuelId = value;
                OnPropertyChanged(nameof(FuelId));
            }
        }

        /// <summary>
        /// Объем резервуара
        /// </summary>
        public decimal Size
        {
            get => _size; 
            set
            {
                _size = value;
                OnPropertyChanged(nameof(Size));
            }
        }

        /// <summary>
        /// Удалено?
        /// </summary>
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
        /// Мертвый остаток
        /// </summary>
        public decimal MinimumSize
        {
            get => _minimumSize;
            set
            {
                _minimumSize = value;
                OnPropertyChanged(nameof(MinimumSize));
            }
        }

        public Fuel Fuel
        {
            get => _fuel;
            set
            {
                _fuel = value;
                OnPropertyChanged(nameof(Fuel));
            }
        }

        public ICollection<FuelIntake> FuelIntakes { get; set; }
        public ICollection<FuelSale> FuelSales { get; set; }
        public ICollection<Nozzle> Nozzle { get; set; }
        public ICollection<TankShiftCounter> TankShiftCounters { get; set; }

        #endregion

        #region Constructors

        public Tank()
        {
            
        }

        public Tank(Tank tank)
        {
            Id = tank.Id;
            Size = tank.Size;
            MinimumSize = tank.MinimumSize;
            Name = tank.Name;
            FuelId = tank.FuelId;
        }

        #endregion

        public override void Update(DomainObject updatedItem)
        {
            if (updatedItem is Tank tank)
            {
                Name = tank.Name;
                Size = tank.Size;
                MinimumSize = tank.MinimumSize;
                FuelId = tank.FuelId;
            }
        }
    }
}