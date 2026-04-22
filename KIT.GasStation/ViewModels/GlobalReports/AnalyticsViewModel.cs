using DevExpress.Mvvm;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.ViewModels.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
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
        private readonly ObservableCollection<DateValuePoint> _revenueByDay = new();
        private readonly ObservableCollection<LabelValuePoint> _revenueByPaymentType = new();
        private readonly ObservableCollection<LabelValuePoint> _volumeByFuel = new();
        private readonly ObservableCollection<HourPoint> _salesByHour = new();
        private readonly ObservableCollection<ShiftRevenuePoint> _revenueByShift = new();

        // Intake
        private readonly ObservableCollection<DateValuePoint> _intakeTotalByDay = new();
        private readonly ObservableCollection<LabelValuePoint> _intakeByFuel = new();
        private readonly ObservableCollection<IntakeDatePoint> _intakeSeries = new();

        private CancellationTokenSource? _loadDataCts;

        // KPIs
        private decimal _totalRevenue;
        private decimal _totalVolume;
        private int _totalTransactions;
        private decimal _totalIntake;
        private decimal _avgRevenuePerShift;
        private decimal _avgIntakePerEvent;

        #endregion

        #region Period

        public IReadOnlyList<PeriodOption> Periods { get; } = new List<PeriodOption>
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
        public ObservableCollection<DateValuePoint> RevenueByDay => _revenueByDay;

        /// <summary>Структура оплат — donut.</summary>
        public ObservableCollection<LabelValuePoint> RevenueByPaymentType => _revenueByPaymentType;

        /// <summary>Объём по видам топлива — donut.</summary>
        public ObservableCollection<LabelValuePoint> VolumeByFuel => _volumeByFuel;

        /// <summary>Продажи по часам суток — bar chart.</summary>
        public ObservableCollection<HourPoint> SalesByHour => _salesByHour;

        /// <summary>Выручка по последним сменам — stacked bar.</summary>
        public ObservableCollection<ShiftRevenuePoint> RevenueByShift => _revenueByShift;

        #endregion

        #region Intake Chart Data

        /// <summary>Суммарный приём по дням — area chart.</summary>
        public ObservableCollection<DateValuePoint> IntakeTotalByDay => _intakeTotalByDay;

        /// <summary>Приём по видам топлива — donut.</summary>
        public ObservableCollection<LabelValuePoint> IntakeByFuel => _intakeByFuel;

        /// <summary>
        /// Плоский список для multi-series chart (один ряд на каждый вид топлива).
        /// DevExpress строит серии автоматически по полю FuelName.
        /// </summary>
        public ObservableCollection<IntakeDatePoint> IntakeSeries => _intakeSeries;

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
            _loadDataCts?.Cancel();
            _loadDataCts?.Dispose();
            _loadDataCts = new CancellationTokenSource();
            var token = _loadDataCts.Token;

            try
            {
                IsLoading = true;

                var cutoff = DateTime.Today.AddDays(-SelectedPeriod.Days);

                var salesTask = _fuelSaleService.GetAllAsync();
                var intakesTask = _fuelIntakeService.GetAllAsync();

                await Task.WhenAll(salesTask, intakesTask);
                token.ThrowIfCancellationRequested();

                var allSales = salesTask.Result
                    .Where(s => s.FuelSaleStatus == FuelSaleStatus.Completed && s.CreateDate >= cutoff)
                    .ToList();

                var allIntakes = intakesTask.Result
                    .Where(i => i.CreateDate >= cutoff)
                    .ToList();

                BuildSalesCharts(allSales);
                BuildIntakeCharts(allIntakes);
            }
            catch (OperationCanceledException)
            {
                // Ignore intermediate refresh requests (e.g., quick period switching).
            }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    IsLoading = false;
                }
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
            ReplaceCollection(RevenueByDay, sales
                .GroupBy(s => s.CreateDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DateValuePoint(g.Key, g.Sum(s => s.ReceivedSum))));

            // ── Структура оплат ───────────────────────────────────────────────
            ReplaceCollection(RevenueByPaymentType, sales
                .GroupBy(s => s.PaymentType)
                .Where(g => g.Sum(s => s.ReceivedSum) > 0)
                .Select(g => new LabelValuePoint(PaymentTypeLabel(g.Key), g.Sum(s => s.ReceivedSum))));

            // ── Объём по видам топлива ────────────────────────────────────────
            ReplaceCollection(VolumeByFuel, sales
                .Where(s => s.Tank?.Fuel != null)
                .GroupBy(s => s.Tank!.Fuel.Name)
                .Select(g => new LabelValuePoint(g.Key, g.Sum(s => s.ReceivedQuantity))));

            // ── Продажи по часам суток ────────────────────────────────────────
            ReplaceCollection(SalesByHour, Enumerable.Range(0, 24)
                .Select(h =>
                {
                    var hourSales = sales.Where(s => s.CreateDate.Hour == h).ToList();
                    return new HourPoint(h, hourSales.Count, hourSales.Sum(s => s.ReceivedSum));
                }));

            // ── Выручка по последним 20 сменам ────────────────────────────────
            ReplaceCollection(RevenueByShift, sales
                .GroupBy(s => new { s.ShiftId, s.Shift?.Number, s.Shift?.OpeningDate })
                .OrderBy(g => g.Key.ShiftId)
                .TakeLast(20)
                .Select(g => new ShiftRevenuePoint(
                    label: FormatShiftLabel(g.Key.Number, g.Key.OpeningDate),
                    cash: g.Where(s => s.PaymentType == PaymentType.Cash).Sum(s => s.ReceivedSum),
                    cashless: g.Where(s => s.PaymentType == PaymentType.Cashless).Sum(s => s.ReceivedSum),
                    ticket: g.Where(s => s.PaymentType == PaymentType.Ticket).Sum(s => s.ReceivedSum),
                    statement: g.Where(s => s.PaymentType == PaymentType.Statement).Sum(s => s.ReceivedSum))));

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
            ReplaceCollection(IntakeTotalByDay, intakes
                .GroupBy(i => i.CreateDate.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DateValuePoint(g.Key, g.Sum(i => i.Quantity))));

            // ── Приём по видам топлива — donut ────────────────────────────────
            ReplaceCollection(IntakeByFuel, intakes
                .Where(i => i.Tank?.Fuel != null)
                .GroupBy(i => i.Tank!.Fuel.Name)
                .Select(g => new LabelValuePoint(g.Key, g.Sum(i => i.Quantity))));

            // ── Multi-series: каждый вид топлива — отдельная линия ────────────
            ReplaceCollection(IntakeSeries, intakes
                .Where(i => i.Tank?.Fuel != null)
                .Select(i => new IntakeDatePoint(i.CreateDate.Date, i.Tank!.Fuel.Name, i.Quantity))
                .GroupBy(p => new { p.Date, p.FuelName })
                .OrderBy(g => g.Key.Date)
                .ThenBy(g => g.Key.FuelName)
                .Select(g => new IntakeDatePoint(g.Key.Date, g.Key.FuelName, g.Sum(p => p.Quantity))));
        }

        #endregion

        #region Helpers

        private static void ReplaceCollection<T>(ObservableCollection<T> target, IEnumerable<T> items)
        {
            target.Clear();
            foreach (var item in items)
            {
                target.Add(item);
            }
        }

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
            if (disposing)
            {
                _loadDataCts?.Cancel();
                _loadDataCts?.Dispose();
            }
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
