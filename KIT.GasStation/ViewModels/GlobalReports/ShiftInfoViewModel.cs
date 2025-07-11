using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Mvvm.POCO;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Domain.Views;
using KIT.GasStation.State.Nozzles;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.ViewModels.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels.GlobalReports
{
    public class ShiftInfoViewModel : ModuleViewModel
    {
        #region Private Members

        private readonly ILogger<ShiftInfoViewModel> _logger;
        private readonly IShiftService _shiftService;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly INozzleService _nozzleService;
        private readonly IShiftCounterService _nozzleCounterService;
        private readonly ITankService _tankService;
        private readonly IFuelIntakeService _fuelIntakeService;
        private readonly ITankShiftCounterService _tankShiftCounterService;
        private readonly IShiftStore _shiftStore;
        private readonly INozzleStore _nozzleStore;
        private readonly IViewService<TankFuelQuantityView> _tankFuelQuantityView;
        private ObservableCollection<Shift> _shifts = new();
        private ObservableCollection<CashTurnover> _cashTurnovers = new();
        private ObservableCollection<NozzleCashTurnover> _nozzleCashTurnovers = new();
        private ObservableCollection<FuelSale> _fuelSales = new();
        private ObservableCollection<TankTurnover> _tankTurnovers = new();
        private Shift _selectedShift;

        #endregion

        #region Public Properties

        public ObservableCollection<Shift> Shifts
        {
            get => _shifts;
            set
            {
                _shifts = value;
                OnPropertyChanged(nameof(Shifts));
                ShiftSelectLastRow();
            }
        }
        public ObservableCollection<CashTurnover> CashTurnovers
        {
            get => _cashTurnovers;
            set
            {
                _cashTurnovers = value;
                OnPropertyChanged(nameof(CashTurnovers));
            }
        }
        public ObservableCollection<NozzleCashTurnover> NozzleCashTurnovers
        {
            get => _nozzleCashTurnovers;
            set
            {
                _nozzleCashTurnovers = value;
                OnPropertyChanged(nameof(NozzleCashTurnovers));
            }
        }
        public ObservableCollection<FuelSale> FuelSales
        {
            get => _fuelSales;
            set
            {
                _fuelSales = value;
                OnPropertyChanged(nameof(FuelSales));
            }
        }
        public ObservableCollection<TankTurnover> TankTurnovers
        {
            get => _tankTurnovers;
            set
            {
                _tankTurnovers = value;
                OnPropertyChanged(nameof(TankTurnovers));
            }
        }
        public Shift SelectedShift
        {
            get => _selectedShift;
            set
            {
                _selectedShift = value;
                OnPropertyChanged(nameof(SelectedShift));
                OnPropertyChanged(nameof(EndShiftTitle));
                OnPropertyChanged(nameof(EndCounterTitle));
                ChangeReport();
            }
        }
        public string EndShiftTitle
        {
            get
            {
                if (_shiftStore.CurrentShiftState is not ShiftState.None && 
                    SelectedShift.Id == _shiftStore.CurrentShift.Id)
                {
                    return "Текущее значение";
                }
                else
                {
                    return "На конец смены";
                }
            }
        }
        public string EndCounterTitle
        {
            get
            {
                if (_shiftStore.CurrentShiftState is not ShiftState.None &&
                    SelectedShift.Id == _shiftStore.CurrentShift.Id)
                {
                    return "Текущий счетчик";
                }
                else
                {
                    return "Счетчик на конец";
                }
            }
        }

        #endregion

        #region Constructor

        public ShiftInfoViewModel(string type, object parent, string title) : base(type, parent, title)
        {
            ISupportServices supportServices = (ISupportServices)parent;

            _logger = supportServices.ServiceContainer.GetRequiredService<ILogger<ShiftInfoViewModel>>();
            _shiftService = supportServices.ServiceContainer.GetRequiredService<IShiftService>();
            _fuelSaleService = supportServices.ServiceContainer.GetRequiredService<IFuelSaleService>();
            _nozzleService = supportServices.ServiceContainer.GetService<INozzleService>();
            _nozzleCounterService = supportServices.ServiceContainer.GetService<IShiftCounterService>();
            _tankService = supportServices.ServiceContainer.GetService<ITankService>();
            _fuelIntakeService = supportServices.ServiceContainer.GetService<IFuelIntakeService>();
            _tankShiftCounterService = supportServices.ServiceContainer.GetService<ITankShiftCounterService>();
            _shiftStore = supportServices.ServiceContainer.GetService<IShiftStore>();
            _nozzleStore = supportServices.ServiceContainer.GetService<INozzleStore>();
            _tankFuelQuantityView = supportServices.ServiceContainer.GetService<IViewService<TankFuelQuantityView>>();

            _fuelSaleService.OnUpdated += FuelSaleService_OnUpdated;
            _shiftService.OnUpdated += ShiftService_OnUpdated;
            _shiftService.OnCreated += ShiftService_OnCreated;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task ShiftSelectedItemChanged()
        {
            try
            {
                if (SelectedShift != null)
                {
                    NozzleCashTurnovers.Clear();
                    TankTurnovers.Clear();

                    var nozzles = await _nozzleService.GetAllAsync();
                    var tanks = await _tankService.GetAllAsync();

                    FuelSales = new(await _fuelSaleService.GetAllAsync(SelectedShift.Id));
                    var fuelIntakes = await _fuelIntakeService.GetAllAsync(SelectedShift.Id);

                    var tankViews = await _tankFuelQuantityView.GetAllAsync();

                    foreach (var nozzle in nozzles.OrderBy(n => n.Tube))
                    {
                        ShiftCounter? nozzleCounter = await _nozzleCounterService.GetAsync(nozzle.Id, SelectedShift.Id);

                        NozzleCashTurnover nozzleCashTurnover = new()
                        {
                            Side = nozzle.Tube,
                            FuelName = nozzle.Tank.Fuel.Name,
                            BeginCount = nozzleCounter != null ? nozzleCounter.BeginNozzleCounter : 0,
                            Cash = FuelSales.Where(f => f.NozzleId == nozzle.Id && f.PaymentType == PaymentType.Cash && f.FuelSaleStatus == FuelSaleStatus.Completed).Sum(f => f.ReceivedSum),
                            CashQuantity = FuelSales.Where(f => f.NozzleId == nozzle.Id && f.PaymentType == PaymentType.Cash && f.FuelSaleStatus == FuelSaleStatus.Completed).Sum(f => f.ReceivedQuantity),
                            Cashless = FuelSales.Where(f => f.NozzleId == nozzle.Id && f.PaymentType == PaymentType.Cashless && f.FuelSaleStatus == FuelSaleStatus.Completed).Sum(f => f.ReceivedSum),
                            CashlessQuantity = FuelSales.Where(f => f.NozzleId == nozzle.Id && f.PaymentType == PaymentType.Cashless && f.FuelSaleStatus == FuelSaleStatus.Completed).Sum(f => f.ReceivedQuantity),
                            Ticket = FuelSales.Where(f => f.NozzleId == nozzle.Id && f.PaymentType == PaymentType.Ticket && f.FuelSaleStatus == FuelSaleStatus.Completed).Sum(f => f.ReceivedSum),
                            TicketQuantity = FuelSales.Where(f => f.NozzleId == nozzle.Id && f.PaymentType == PaymentType.Ticket && f.FuelSaleStatus == FuelSaleStatus.Completed).Sum(f => f.ReceivedQuantity),
                            Statement = FuelSales.Where(f => f.NozzleId == nozzle.Id && f.PaymentType == PaymentType.Statement && f.FuelSaleStatus == FuelSaleStatus.Completed).Sum(f => f.ReceivedSum),
                            StatementQuantity = FuelSales.Where(f => f.NozzleId == nozzle.Id && f.PaymentType == PaymentType.Statement && f.FuelSaleStatus == FuelSaleStatus.Completed).Sum(f => f.ReceivedQuantity)
                        };

                        if (_shiftStore.CurrentShiftState is not ShiftState.None && 
                            SelectedShift.Id == _shiftStore.CurrentShift.Id)
                        {
                            Nozzle? nozzleStore = _nozzleStore.Nozzles.FirstOrDefault(f => f.Id == nozzle.Id);
                            if (nozzleStore != null)
                            {
                                nozzleCashTurnover.EndCount = nozzle.LastCounter;
                            }
                        }
                        else
                        {
                            nozzleCashTurnover.EndCount = nozzleCounter != null ? nozzleCounter.EndNozzleCounter : 0;
                        }

                        NozzleCashTurnovers.Add(nozzleCashTurnover);
                    }

                    foreach (var item in tanks)
                    {
                        var tankShiftCounter = await _tankShiftCounterService.GetAsync(item.Id, SelectedShift.Id);

                        var tankTurnover = new TankTurnover
                        {
                            Number = item.Number,
                            FuelName = item.Fuel.Name,
                            BeginShiftQuantity = tankShiftCounter != null ? tankShiftCounter.BeginCount : 0,
                            SaleQauntity = FuelSales.Where(f => f.TankId == item.Id).Sum(f => f.ReceivedQuantity),
                            Sale = FuelSales.Where(f => f.TankId == item.Id).Sum(f => f.ReceivedSum),
                        };

                        if (fuelIntakes != null && fuelIntakes.Any())
                        {
                            tankTurnover.IntakeQuantity = fuelIntakes.Where(f => f.TankId == item.Id).Sum(f => f.Quantity);
                        }

                        if (_shiftStore.CurrentShiftState is not ShiftState.None &&
                            SelectedShift.Id == _shiftStore.CurrentShift.Id)
                        {
                            TankFuelQuantityView? tankFuelQuantityView = tankViews.FirstOrDefault(f => f.Id == item.Id);
                            
                            if (tankFuelQuantityView != null)
                            {
                                tankTurnover.EndShiftQuantity = tankFuelQuantityView.CurrentFuelQuantity;
                            }
                        }
                        else
                        {
                            tankTurnover.EndShiftQuantity = tankShiftCounter != null ? tankShiftCounter.EndCount : 0;
                        }

                        TankTurnovers.Add(tankTurnover);
                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }

        public async Task StartAsync()
        {
            await GetDataAsync();
        }

        #endregion

        #region Private Voids

        private async Task GetDataAsync()
        {
            try
            {
                Shifts = new(await _shiftService.GetAllAsync());
                CashTurnovers = new() {
                    new CashTurnover
                    {
                        Id = 1,
                        Title = "Остаток на начало смены",
                        Sum = 0
                    },
                    new CashTurnover
                    {
                        Id = 2,
                        Title = "Наличные",
                        Sum = 0
                    },
                    new CashTurnover
                    {
                        Id = 3,
                        Title = "Безналичные",
                        Sum = 0
                    },
                    new CashTurnover
                    {
                        Id = 4,
                        Title = "Ведомость",
                        Sum = 0
                    },
                    new CashTurnover
                    {
                        Id = 5,
                        Title = "Талон",
                        Sum = 0
                    },
                    new CashTurnover
                    {
                        Id = 6,
                        Title = "Итого",
                        Sum = 0
                    },
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private void ChangeReport()
        {
            try
            {
                if (SelectedShift.FuelSales != null && SelectedShift.FuelSales.Count != 0)
                {
                    ///Наличные
                    CashTurnover cash = CashTurnovers.First(c => c.Id == 2);
                    cash.Sum = SelectedShift.FuelSales
                        .Where(f => f.FuelSaleStatus == FuelSaleStatus.Completed && f.PaymentType == PaymentType.Cash)
                        .Sum(c => c.ReceivedSum);

                    //Безналичные
                    CashTurnover cashless = CashTurnovers.First(c => c.Id == 3);
                    cashless.Sum = SelectedShift.FuelSales
                        .Where(f => f.FuelSaleStatus == FuelSaleStatus.Completed && f.PaymentType == PaymentType.Cashless)
                        .Sum(c => c.ReceivedSum);

                    //Ведомость
                    CashTurnover statement = CashTurnovers.First(c => c.Id == 4);
                    statement.Sum = SelectedShift.FuelSales
                        .Where(f => f.FuelSaleStatus == FuelSaleStatus.Completed && f.PaymentType == PaymentType.Statement)
                        .Sum(c => c.ReceivedSum);

                    //Талон
                    CashTurnover talon = CashTurnovers.First(c => c.Id == 5);
                    talon.Sum = SelectedShift.FuelSales
                        .Where(f => f.FuelSaleStatus == FuelSaleStatus.Completed && f.PaymentType == PaymentType.Ticket)
                        .Sum(c => c.ReceivedSum);

                    //Итого
                    CashTurnover amount = CashTurnovers.First(c => c.Id == 6);
                    amount.Sum = SelectedShift.FuelSales
                        .Where(f => f.FuelSaleStatus == FuelSaleStatus.Completed)
                        .Sum(c => c.ReceivedSum);

                }
                else
                {
                    foreach (var item in CashTurnovers)
                    {
                        item.Sum = 0;
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private void FuelSaleService_OnUpdated(FuelSale fuelSale)
        {
            try
            {
                if (fuelSale.FuelSaleStatus == FuelSaleStatus.Completed)
                {
                    Shift? shift = Shifts.FirstOrDefault(s => s.Id == fuelSale.ShiftId);
                    shift?.FuelSales.Add(fuelSale);
                    if (SelectedShift != null)
                    {
                        ChangeReport();
                        _ = Task.Run(UpdateSelectedShift);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private async Task UpdateSelectedShift()
        {
            try
            {
                if (SelectedShift != null)
                {
                    Shift shift = await _shiftService.GetAsync(SelectedShift.Id);
                    SelectedShift.SetUpdates(shift);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private void ShiftService_OnCreated(Shift createdShift)
        {
            try
            {
                Shifts.Add(createdShift);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private void ShiftService_OnUpdated(Shift updatedShift)
        
        {
            try
            {
                Shift? shift = Shifts.FirstOrDefault(s => s.Equals(updatedShift));
                shift?.SetUpdates(updatedShift);
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private void ShiftSelectLastRow()
        {
            if (Shifts != null && Shifts.Count > 0)
            {
                SelectedShift = Shifts.Last();
            }
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fuelSaleService.OnUpdated -= FuelSaleService_OnUpdated;
                _shiftService.OnUpdated -= ShiftService_OnUpdated;
                _shiftService.OnCreated -= ShiftService_OnCreated;
            }

            base.Dispose(disposing);
        }

        #endregion
    }

    /// <summary>
    /// Оборот наличных для отчета
    /// </summary>
    public class CashTurnover : DomainObject
    {
        #region Private Members

        private string _title;
        private decimal _sum;

        #endregion

        #region Public Properties

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }
        public decimal Sum
        {
            get => _sum;
            set
            {
                _sum = value;
                OnPropertyChanged(nameof(Sum));
            }
        }

        public override void Update(DomainObject updatedItem)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class NozzleCashTurnover : DomainObject
    {
        #region Private Members

        private int _side;
        private string _fuelName;
        private decimal _beginCount;
        private decimal _endCount;
        private decimal _cash;
        private decimal _cashQuantity;
        private decimal _cashless;
        private decimal _cashlessQuantity;
        private decimal _ticket;
        private decimal _ticketQuantity;
        private decimal _statement;
        private decimal _statementQuantity;

        #endregion

        #region Public Proreties

        public int Side
        {
            get => _side;
            set
            {
                _side = value;
                OnPropertyChanged(nameof(Side));
            }
        }

        public string FuelName
        {
            get => _fuelName;
            set
            {
                _fuelName = value;
                OnPropertyChanged(nameof(FuelName));
            }
        }

        public decimal BeginCount
        {
            get => _beginCount;
            set
            {
                _beginCount = value;
                OnPropertyChanged(nameof(BeginCount));
                OnPropertyChanged(nameof(Balance));
            }
        }

        public decimal EndCount
        {
            get => _endCount;
            set
            {
                _endCount = value;
                OnPropertyChanged(nameof(EndCount));
                OnPropertyChanged(nameof(Balance));
            }
        }

        public decimal Cash
        {
            get => _cash;
            set
            {
                _cash = value;
                OnPropertyChanged(nameof(Cash));
                OnPropertyChanged(nameof(Total));
            }
        }

        public decimal CashQuantity
        {
            get => _cashQuantity;
            set
            {
                _cashQuantity = value;
                OnPropertyChanged(nameof(CashQuantity));
                OnPropertyChanged(nameof(TotalQuantity));
                OnPropertyChanged(nameof(Balance));
            }
        }

        public decimal Cashless
        {
            get => _cashless;
            set
            {
                _cashless = value;
                OnPropertyChanged(nameof(Cashless));
                OnPropertyChanged(nameof(Total));
            }
        }

        public decimal CashlessQuantity
        {
            get => _cashlessQuantity;
            set
            {
                _cashlessQuantity = value;
                OnPropertyChanged(nameof(CashlessQuantity));
                OnPropertyChanged(nameof(TotalQuantity));
                OnPropertyChanged(nameof(Balance));
            }
        }

        public decimal Ticket
        {
            get => _ticket;
            set
            {
                _ticket = value;
                OnPropertyChanged(nameof(Ticket));
                OnPropertyChanged(nameof(Total));
            }
        }

        public decimal TicketQuantity
        {
            get => _ticketQuantity;
            set
            {
                _ticketQuantity = value;
                OnPropertyChanged(nameof(TicketQuantity));
                OnPropertyChanged(nameof(TotalQuantity));
                OnPropertyChanged(nameof(Balance));
            }
        }

        public decimal Statement
        {
            get => _statement;
            set
            {
                _statement = value;
                OnPropertyChanged(nameof(Statement));
                OnPropertyChanged(nameof(Total));
            }
        }

        public decimal StatementQuantity
        {
            get => _statementQuantity;
            set
            {
                _statementQuantity = value;
                OnPropertyChanged(nameof(StatementQuantity));
                OnPropertyChanged(nameof(TotalQuantity));
                OnPropertyChanged(nameof(Balance));
            }
        }

        public decimal Total => Cash + Cashless + Ticket + Statement;

        public decimal TotalQuantity => CashQuantity + CashlessQuantity + TicketQuantity + StatementQuantity;

        public decimal Balance => BeginCount + TotalQuantity - EndCount;

        #endregion


        public override void Update(DomainObject updatedItem)
        {
            throw new NotImplementedException();
        }
    }

    public class TankTurnover : DomainObject
    {
        #region Private Members

        private int _number;
        private string _fuelName = string.Empty;
        private decimal _beginShiftQuantity;
        private decimal _endShiftQuantity;
        private decimal _saleQauntity;
        private decimal _sale;
        private decimal _intakeQuantity;
        private decimal _refundQuantity;
        private decimal _checkingQuantity;

        #endregion

        #region Public Properties
        
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
        /// Наименование топлива
        /// </summary>
        public string FuelName
        {
            get => _fuelName;
            set
            {
                _fuelName = value;
                OnPropertyChanged(nameof(FuelName));
            }
        }

        /// <summary>
        /// На начало смены количество топливы в резервуаре
        /// </summary>
        public decimal BeginShiftQuantity
        {
            get => _beginShiftQuantity;
            set
            {
                _beginShiftQuantity = value;
                OnPropertyChanged(nameof(BeginShiftQuantity));
                OnPropertyChanged(nameof(Balance));
            }
        }

        /// <summary>
        /// На конец смены количество топливы в резервуаре
        /// </summary>
        public decimal EndShiftQuantity
        {
            get => _endShiftQuantity;
            set
            {
                _endShiftQuantity = value;
                OnPropertyChanged(nameof(EndShiftQuantity));
                OnPropertyChanged(nameof(Balance));
            }
        }

        /// <summary>
        /// Отпущено количество
        /// </summary>
        public decimal SaleQauntity
        {
            get => _saleQauntity;
            set
            {
                _saleQauntity = value;
                OnPropertyChanged(nameof(SaleQauntity));
                OnPropertyChanged(nameof(Balance));
            }
        }

        /// <summary>
        /// Отпущено сумма
        /// </summary>
        public decimal Sale
        {
            get => _sale;
            set
            {
                _sale = value;
                OnPropertyChanged(nameof(Sale));
                OnPropertyChanged(nameof(Balance));
            }
        }

        /// <summary>
        /// Количество приема топлива
        /// </summary>
        public decimal IntakeQuantity
        {
            get => _intakeQuantity;
            set
            {
                _intakeQuantity = value;
                OnPropertyChanged(nameof(IntakeQuantity));
                OnPropertyChanged(nameof(Balance));
            }
        }

        /// <summary>
        /// Количество возврата топлива
        /// </summary>
        public decimal RefundQuantity
        {
            get => _refundQuantity;
            set
            {
                _refundQuantity = value;
                OnPropertyChanged(nameof(RefundQuantity));
                OnPropertyChanged(nameof(Balance));
            }
        }

        /// <summary>
        /// Количество поверок
        /// </summary>
        public decimal CheckingQuantity
        {
            get => _checkingQuantity;
            set
            {
                _checkingQuantity = value;
                OnPropertyChanged(nameof(CheckingQuantity));
                OnPropertyChanged(nameof(Balance));
            }
        }

        public decimal Balance => BeginShiftQuantity - SaleQauntity + IntakeQuantity + RefundQuantity - EndShiftQuantity;

        #endregion

        public override void Update(DomainObject updatedItem)
        {
            throw new NotImplementedException();
        }
    }

}
