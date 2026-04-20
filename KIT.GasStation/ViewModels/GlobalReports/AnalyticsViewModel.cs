using DevExpress.Mvvm;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.ViewModels.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels.GlobalReports
{
    public class AnalyticsViewModel : ModuleViewModel
    {
        #region Private Members

        private readonly IFuelSaleService _fuelSaleService;
        private readonly IFuelIntakeService _fuelIntakeService;
        private readonly ILogger<AnalyticsViewModel> _logger;

        private PeriodOption _selectedPeriod;
        private bool _isLoading;

        // Sales
        private List<DateValuePoint> _revenueByDay = new();
        private List<LabelValuePoint> _revenueByPaymentType = new();
        private List<LabelValuePoint> _volumeByFuel = new();
        private List<HourPoint> _salesByHour = new();
        private List<ShiftRevenuePoint> _revenueByShift = new();

        // Intake
        private List<DateValuePoint> _intakeTotalByDay = new();
        private List<LabelValuePoint> _intakeByFuel = new();
        private List<IntakeDatePoint> _intakeSeries = new();

        // KPIs
        private decimal _totalRevenue;
        private decimal _totalVolume;
        private int _totalTransactions;
        private decimal _totalIntake;
        private decimal _avgRevenuePerShift;
        private decimal _avgIntakePerEvent;

        #endregion

        #region Period

        public List<PeriodOption> Periods { get; } = new()
        {
            new PeriodOption("7 дней",  7),
            new PeriodOption("30 дней", 30),
            new PeriodOption("90 дней", 90),
            new PeriodOption("Год",     365),
        };

        public PeriodOption SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                if (_selectedPeriod == value) return;
                _selectedPeriod = value;
                OnPropertyChanged(nameof(SelectedPeriod));
                _ = LoadDataAsync();
            }
        }

        #endregion

        #region State

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(nameof(IsLoading)); }
        }

        #endregion

        #region Sales Chart Data

        /// <summary>Выручка по дням — area chart.</summary>
        public List<DateValuePoint> RevenueByDay
        {
            get => _revenueByDay;
            set { _revenueByDay = value; OnPropertyChanged(nameof(RevenueByDay)); }
        }

        /// <summary>Структура оплат — donut.</summary>
        public List<LabelValuePoint> RevenueByPaymentType
        {
            get => _revenueByPaymentType;
            set { _revenueByPaymentType = value; OnPropertyChanged(nameof(RevenueByPaymentType)); }
        }

        /// <summary>Объём по видам топлива — donut.</summary>
        public List<LabelValuePoint> VolumeByFuel
        {
            get => _volumeByFuel;
            set { _volumeByFuel = value; OnPropertyChanged(nameof(VolumeByFuel)); }
        }

        /// <summary>Продажи по часам суток — bar chart.</summary>
        public List<HourPoint> SalesByHour
        {
            get => _salesByHour;
            set { _salesByHour = value; OnPropertyChanged(nameof(SalesByHour)); }
        }

        /// <summary>Выручка по последним сменам — stacked bar.</summary>
        public List<ShiftRevenuePoint> RevenueByShift
        {
            get => _revenueByShift;
            set { _revenueByShift = value; OnPropertyChanged(nameof(RevenueByShift)); }
        }

        #endregion

        #region Intake Chart Data

        /// <summary>Суммарный приём по дням — area chart.</summary>
        public List<DateValuePoint> IntakeTotalByDay
        {
            get => _intakeTotalByDay;
            set { _intakeTotalByDay = value; OnPropertyChanged(nameof(IntakeTotalByDay)); }
        }

        /// <summary>Приём по видам топлива — donut.</summary>
        public List<LabelValuePoint> IntakeByFuel
        {
            get => _intakeByFuel;
            set { _intakeByFuel = value; OnPropertyChanged(nameof(IntakeByFuel)); }
        }

        /// <summary>
        /// Плоский список для multi-series chart (один ряд на каждый вид топлива).
        /// DevExpress строит серии автоматически по полю FuelName.
        /// </summary>
        public List<IntakeDatePoint> IntakeSeries
        {
            get => _intakeSeries;
            set { _intakeSeries = value; OnPropertyChanged(nameof(IntakeSeries)); }
        }

        #endregion

        #region KPI Summary

        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set { _totalRevenue = value; OnPropertyChanged(nameof(TotalRevenue)); }
        }

        public decimal TotalVolume
        {
            get => _totalVolume;
            set { _totalVolume = value; OnPropertyChanged(nameof(TotalVolume)); }
        }

        public int TotalTransactions
        {
            get => _totalTransactions;
            set { _totalTransactions = value; OnPropertyChanged(nameof(TotalTransactions)); }
        }

        public decimal TotalIntake
        {
            get => _totalIntake;
            set { _totalIntake = value; OnPropertyChanged(nameof(TotalIntake)); }
        }

        public decimal AvgRevenuePerShift
        {
            get => _avgRevenuePerShift;
            set { _avgRevenuePerShift = value; OnPropertyChanged(nameof(AvgRevenuePerShift)); }
        }

        public decimal AvgIntakePerEvent
        {
            get => _avgIntakePerEvent;
            set { _avgIntakePerEvent = value; OnPropertyChanged(nameof(AvgIntakePerEvent)); }
        }

        #endregion

        #region Constructor

        public AnalyticsViewModel(string type, object parent, string title) : base(type, parent, title)
        {
            var services = (DevExpress.Mvvm.ISupportServices)parent;

            _logger = services.ServiceContainer.GetRequiredService<ILogger<AnalyticsViewModel>>();
            _fuelSaleService = services.ServiceContainer.GetRequiredService<IFuelSaleService>();
            _fuelIntakeService = services.ServiceContainer.GetRequiredService<IFuelIntakeService>();

            _selectedPeriod = Periods[1]; // 30 дней по умолчанию
        }

        #endregion

        #region Initialization

        public async Task StartAsync()
        {
            await LoadDataAsync();
        }

        #endregion

        #region Data Loading

        private async Task LoadDataAsync()
        {
            try
            {
                IsLoading = true;

                var cutoff = DateTime.Today.AddDays(-SelectedPeriod.Days);

                var allSales = (await _fuelSaleService.GetAllAsync())
                    .Where(s => s.FuelSaleStatus == FuelSaleStatus.Completed
                             && s.CreateDate >= cutoff)
                    .ToList();

                var allIntakes = (await _fuelIntakeService.GetAllAsync())
                    .Where(i => i.CreateDate >= cutoff)
                    .ToList();

                BuildSalesCharts(allSales);
                BuildIntakeCharts(allIntakes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка загрузки данных аналитики");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region Sales Charts

        private void BuildSalesCharts(List<FuelSale> sales)
        {
            // ── KPI ──────────────────────────────────────────────────────────
            TotalRevenue = sales.Sum(s => s.ReceivedSum);
            TotalVolume = sales.Sum(s => s.ReceivedQuantity);
            TotalTransactions = sales.Count;

            // ── Выручка по дням ──────────────────────────────────────────────
            RevenueByDay = sales
                .GroupBy(s => s.CreateDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DateValuePoint(g.Key, g.Sum(s => s.ReceivedSum)))
                .ToList();

            // ── Структура оплат ───────────────────────────────────────────────
            RevenueByPaymentType = sales
                .GroupBy(s => s.PaymentType)
                .Where(g => g.Sum(s => s.ReceivedSum) > 0)
                .Select(g => new LabelValuePoint(PaymentTypeLabel(g.Key), g.Sum(s => s.ReceivedSum)))
                .ToList();

            // ── Объём по видам топлива ────────────────────────────────────────
            VolumeByFuel = sales
                .Where(s => s.Tank?.Fuel != null)
                .GroupBy(s => s.Tank!.Fuel.Name)
                .Select(g => new LabelValuePoint(g.Key, g.Sum(s => s.ReceivedQuantity)))
                .ToList();

            // ── Продажи по часам суток ────────────────────────────────────────
            SalesByHour = Enumerable.Range(0, 24)
                .Select(h =>
                {
                    var hourSales = sales.Where(s => s.CreateDate.Hour == h).ToList();
                    return new HourPoint(h, hourSales.Count, hourSales.Sum(s => s.ReceivedSum));
                })
                .ToList();

            // ── Выручка по последним 20 сменам ────────────────────────────────
            RevenueByShift = sales
                .GroupBy(s => new { s.ShiftId, s.Shift?.Number, s.Shift?.OpeningDate })
                .OrderBy(g => g.Key.ShiftId)
                .TakeLast(20)
                .Select(g => new ShiftRevenuePoint(
                    label: FormatShiftLabel(g.Key.Number, g.Key.OpeningDate),
                    cash: g.Where(s => s.PaymentType == PaymentType.Cash).Sum(s => s.ReceivedSum),
                    cashless: g.Where(s => s.PaymentType == PaymentType.Cashless).Sum(s => s.ReceivedSum),
                    ticket: g.Where(s => s.PaymentType == PaymentType.Ticket).Sum(s => s.ReceivedSum),
                    statement: g.Where(s => s.PaymentType == PaymentType.Statement).Sum(s => s.ReceivedSum)))
                .ToList();

            var shiftCount = RevenueByShift.Count;
            AvgRevenuePerShift = shiftCount > 0
                ? RevenueByShift.Average(p => p.Cash + p.Cashless + p.Ticket + p.Statement)
                : 0;
        }

        #endregion

        #region Intake Charts

        private void BuildIntakeCharts(List<FuelIntake> intakes)
        {
            // ── KPI ──────────────────────────────────────────────────────────
            TotalIntake = intakes.Sum(i => i.Quantity);
            AvgIntakePerEvent = intakes.Count > 0
                ? intakes.Average(i => i.Quantity)
                : 0;

            // ── Суммарный приём по дням ───────────────────────────────────────
            IntakeTotalByDay = intakes
                .GroupBy(i => i.CreateDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DateValuePoint(g.Key, g.Sum(i => i.Quantity)))
                .ToList();

            // ── Приём по видам топлива — donut ────────────────────────────────
            IntakeByFuel = intakes
                .Where(i => i.Tank?.Fuel != null)
                .GroupBy(i => i.Tank!.Fuel.Name)
                .Select(g => new LabelValuePoint(g.Key, g.Sum(i => i.Quantity)))
                .ToList();

            // ── Multi-series: каждый вид топлива — отдельная линия ────────────
            IntakeSeries = intakes
                .Where(i => i.Tank?.Fuel != null)
                .Select(i => new IntakeDatePoint(i.CreateDate.Date, i.Tank!.Fuel.Name, i.Quantity))
                .GroupBy(p => new { p.Date, p.FuelName })
                .OrderBy(g => g.Key.Date)
                .Select(g => new IntakeDatePoint(g.Key.Date, g.Key.FuelName, g.Sum(p => p.Quantity)))
                .ToList();
        }

        #endregion

        #region Helpers

        private static string PaymentTypeLabel(PaymentType type) => type switch
        {
            PaymentType.Cash => "Наличные",
            PaymentType.Cashless => "Безналичные",
            PaymentType.Ticket => "Талон",
            PaymentType.Statement => "Ведомость",
            _ => type.ToString()
        };

        private static string FormatShiftLabel(int? number, DateTime? date) =>
            number.HasValue && date.HasValue
                ? $"№{number} {date.Value:dd.MM}"
                : "–";

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion
    }

    // ─── Chart data records ───────────────────────────────────────────────────

    /// <summary>Точка временного ряда.</summary>
    public class DateValuePoint
    {
        public DateValuePoint(DateTime date, decimal value)
        {
            Date = date;
            Value = value;
        }
        public DateTime Date { get; }
        public decimal Value { get; }
    }

    /// <summary>Категорийная точка (для donut / bar с подписями).</summary>
    public class LabelValuePoint
    {
        public LabelValuePoint(string label, decimal value)
        {
            Label = label;
            Value = value;
        }
        public string Label { get; }
        public decimal Value { get; }
    }

    /// <summary>Точка распределения по часам суток.</summary>
    public class HourPoint
    {
        public HourPoint(int hour, int count, decimal revenue)
        {
            HourLabel = $"{hour:00}:00";
            Count = count;
            Revenue = revenue;
        }
        public string HourLabel { get; }
        public int Count { get; }
        public decimal Revenue { get; }
    }

    /// <summary>Точка stacked-bar выручки по смене.</summary>
    public class ShiftRevenuePoint
    {
        public ShiftRevenuePoint(string label, decimal cash, decimal cashless, decimal ticket, decimal statement)
        {
            Label = label;
            Cash = cash;
            Cashless = cashless;
            Ticket = ticket;
            Statement = statement;
        }
        public string Label { get; }
        public decimal Cash { get; }
        public decimal Cashless { get; }
        public decimal Ticket { get; }
        public decimal Statement { get; }
    }

    /// <summary>Точка приёма топлива для multi-series chart.</summary>
    public class IntakeDatePoint
    {
        public IntakeDatePoint(DateTime date, string fuelName, decimal quantity)
        {
            Date = date;
            FuelName = fuelName;
            Quantity = quantity;
        }
        public DateTime Date { get; }
        public string FuelName { get; }
        public decimal Quantity { get; }
    }

    /// <summary>Опция периода для фильтра.</summary>
    public class PeriodOption
    {
        public PeriodOption(string display, int days)
        {
            Display = display;
            Days = days;
        }
        public string Display { get; }
        public int Days { get; }
        public override string ToString() => Display;
    }
}
