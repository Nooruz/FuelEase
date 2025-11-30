using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KIT.GasStation.Domain.Models
{
    /// <summary>
    /// ТРК
    /// </summary>
    [Display(Name = "ТРК")]
    public class Nozzle : DomainObject
    {
        #region Private Members

        private string _name;
        private int _tube;
        private int _side;
        private int _tankId;
        private string _group;
        private NozzleStatus _status;
        private bool _isProgramControl;
        private decimal _lastCounter;
        private decimal _salesSum;
        private decimal _receivedSum;
        private decimal _receivedQuantity;
        private decimal _sum;
        private decimal _quantity;
        private bool _lifted;
        private FuelSale _fuelSale;
        private int _number;
        private string? _workerStateMessage;
        private DateTimeOffset _workerStateUpdatedAt;

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
        /// Шланг
        /// </summary>
        public int Tube
        {
            get => _tube;
            set
            {
                _tube = value;
                OnPropertyChanged(nameof(Tube));
            }
        }

        /// <summary>
        /// Сторона
        /// </summary>
        public int Side
        {
            get => _side;
            set
            {
                _side = value;
                OnPropertyChanged(nameof(Side));
            }
        }

        /// <summary>
        /// Получает или задает код резервуара.
        /// </summary>
        public int TankId
        {
            get => _tankId;
            set
            {
                _tankId = value;
                OnPropertyChanged(nameof(TankId));
            }
        }

        /// <summary>
        /// Группа хаб
        /// </summary>
        public string Group
        {
            get => _group;
            set
            {
                _group = value;
                OnPropertyChanged(nameof(Group));
            }
        }

        public Tank? Tank { get; set; }

        public ICollection<ShiftCounter> NozzleShiftCounters { get; set; }

        [NotMapped]
        public string? WorkerStateMessage
        {
            get => _workerStateMessage;
            set
            {
                _workerStateMessage = value;
                OnPropertyChanged(nameof(WorkerStateMessage));
            }
        }

        [NotMapped]
        public DateTimeOffset WorkerStateUpdatedAt
        {
            get => _workerStateUpdatedAt;
            set
            {
                _workerStateUpdatedAt = value;
                OnPropertyChanged(nameof(WorkerStateUpdatedAt));
            }
        }

        /// <summary>
        /// Статус
        /// </summary>
        [NotMapped]
        public NozzleStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }

        /// <summary>
        /// Режим управления
        /// </summary>
        [NotMapped]
        public bool IsProgramControl
        {
            get => _isProgramControl;
            set
            {
                _isProgramControl = value;
                OnPropertyChanged(nameof(IsProgramControl));
            }
        }

        /// <summary>
        /// Последний счетчик ТРК
        /// </summary>
        [NotMapped]
        public decimal LastCounter
        {
            get => _lastCounter;
            set
            {
                _lastCounter = value;
                OnPropertyChanged(nameof(LastCounter));
            }
        }

        /// <summary>
        /// Счетчик БД
        /// </summary>
        [NotMapped]
        public decimal SalesSum
        {
            get => _salesSum;
            set
            {
                _salesSum = value;
                OnPropertyChanged(nameof(SalesSum));
            }
        }

        /// <summary>
        /// Цена
        /// </summary>
        [NotMapped]
        public decimal Price => Tank?.Fuel?.Price ?? 0;

        /// <summary>
        /// Поднят ли пистолет
        /// </summary>
        [NotMapped]
        public bool Lifted
        {
            get => _lifted;
            set
            {
                _lifted = value;
                OnPropertyChanged(nameof(Lifted));
            }
        }

        [NotMapped]
        public decimal ReceivedQuantity
        {
            get => _receivedQuantity;
            set
            {
                _receivedQuantity = value;
                OnPropertyChanged(nameof(ReceivedQuantity));
            }
        }

        [NotMapped]
        public decimal ReceivedSum
        {
            get => _receivedSum;
            set
            {
                _receivedSum = value;
                OnPropertyChanged(nameof(ReceivedSum));
            }
        }

        [NotMapped]
        public decimal Sum
        {
            get => _sum;
            set
            {
                _sum = value;
                OnPropertyChanged(nameof(Sum));
            }
        }

        [NotMapped]
        public decimal Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                OnPropertyChanged(nameof(Quantity));
            }
        }

        [NotMapped]
        public FuelSale FuelSale
        {
            get => _fuelSale;
            set
            {
                _fuelSale = value;
                OnPropertyChanged(nameof(FuelSale));
            }
        }

        [NotMapped]
        public int Number
        {
            get => _number;
            set
            {
                _number = value;
                OnPropertyChanged(nameof(Number));
            }
        }

        #endregion

        #region Construtctors

        public Nozzle()
        {
            
        }

        public Nozzle(Nozzle nozzle)
        {
            Id = nozzle.Id;
            Name = nozzle.Name;
            Tube = nozzle.Tube;
            Side = nozzle.Side;
            TankId = nozzle.TankId;
            Group = nozzle.Group;
        }

        #endregion

        public override void Update(DomainObject updatedItem)
        {
            if (updatedItem is Nozzle updatedNozzle)
            {
                Name = updatedNozzle.Name;
                Tube = updatedNozzle.Tube;
                Side = updatedNozzle.Side;
                TankId = updatedNozzle.TankId;
                Group = updatedNozzle.Group;
            }
        }
    }

    public enum NozzleStatus
    {
        /// <summary>
        /// Неизвестно
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Готов
        /// </summary>
        Ready = 1,

        /// <summary>
        /// Насос работает
        /// </summary>
        PumpWorking = 2,

        /// <summary>
        /// Ожидание остановки
        /// </summary>
        WaitingStop = 3,

        /// <summary>
        /// Насос остановлен
        /// </summary>
        PumpStop = 4,

        /// <summary>
        /// Ожидание снятия пистолета
        /// </summary>
        WaitingRemoved = 5,

        /// <summary>
        /// Блокировка
        /// </summary>
        Blocking = 6
    }

    public enum NozzleControlMode
    {
        /// <summary>
        /// Неизвестно
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Программное управление
        /// </summary>
        Program = 1,
        
        /// <summary>
        /// Клавиатура
        /// </summary>
        Keyboard
    }
}
