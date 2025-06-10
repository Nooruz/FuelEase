using FuelEase.Domain.Models;
using FuelEase.Domain.Models.Discounts;
using FuelEase.Domain.Services;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FuelEase.State.Discounts
{
    public class DisсountStore : IDisсountStore, IHostedService
    {
        #region Private Members

        private readonly IDiscountService _discountService;
        private Discount _activeDiscount;
        private System.Timers.Timer _timer;

        #endregion

        #region Public Properties

        public bool IsActiveDiscount { get; private set; }

        #endregion

        #region Constructors

        public DisсountStore(IDiscountService discountService)
        {
            _discountService = discountService;

            _discountService.OnDeleted += DiscountService_OnDeleted;
            _discountService.OnUpdated += DiscountService_OnUpdated;
            _discountService.OnCreated += DiscountService_OnCreated;
        }

        #endregion

        #region Public Voids

        public DiscountSale? CalculateDiscount(FuelSale fuelSale, int fuelId, bool isUpdatingSum)
        {
            if (!IsActiveDiscount || _activeDiscount == null || !_activeDiscount.DiscountFuels.Any(f => f.FuelId == fuelId))
                return null;

            var applicableTariffPlan = GetApplicableTariffPlan(fuelSale);
            if (applicableTariffPlan == null)
                return null;

            decimal discountPrice = fuelSale.Price - (decimal)applicableTariffPlan.DiscountValue;
            if (discountPrice <= 0) return null; // Избегаем некорректных значений

            return new DiscountSale
            {
                DiscountId = _activeDiscount.Id,
                DiscountPrice = discountPrice,
                DiscountQuantity = isUpdatingSum ?
                    Math.Round((fuelSale.Sum / discountPrice) - fuelSale.Quantity, 6) : 0,
                DiscountSum = !isUpdatingSum ?
                    fuelSale.Sum - Math.Round((decimal)fuelSale.Quantity * discountPrice, 2) : 0
            };
        }

        #endregion

        #region Private Voids

        private DiscountTariffPlan? GetApplicableTariffPlan(FuelSale fuelSale)
        {
            return _activeDiscount?.DiscountTariffPlans?.FirstOrDefault(plan =>
                fuelSale.Sum >= (decimal)plan.MinimumValue && fuelSale.Sum <= (decimal)plan.MaximumValue);
        }

        private async Task GetActiveDiscount()
        {
            try
            {
                Discount? activeDiscount = await _discountService.GetActiveDiscountAsync();

                if (activeDiscount == null)
                {
                    IsActiveDiscount = false;
                }
                else
                {
                    _activeDiscount = activeDiscount;
                    InitializeDiscountTimer();
                    IsActiveDiscount = true;
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private void InitializeDiscountTimer()
        {
            try
            {
                TimeSpan timeToGo = _activeDiscount.EndDate - DateTime.Now;
                if (timeToGo > TimeSpan.Zero)
                {
                    if (timeToGo.TotalMilliseconds > Int32.MaxValue) // Ограничение таймера в миллисекундах
                    {
                        timeToGo = TimeSpan.FromMilliseconds(Int32.MaxValue); // Устанавливаем на максимум возможное время
                    }

                    _timer = new System.Timers.Timer(timeToGo.TotalMilliseconds);
                    _timer.Elapsed += Timer_Elapsed;
                    _timer.AutoReset = false; // Однократное выполнение
                    _timer.Start();
                }
                else
                {
                    IsActiveDiscount = false;
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                TimeSpan remainingTime = _activeDiscount.EndDate - DateTime.Now;
                if (remainingTime > TimeSpan.Zero)
                {
                    InitializeDiscountTimer(); // Перезапускаем таймер с новым оставшимся временем
                }
                else
                {
                    IsActiveDiscount = false;
                    _timer?.Dispose();
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private void DiscountService_OnCreated(Discount createdDiscount)
        {
            Task.Run(GetActiveDiscount);
        }

        private void DiscountService_OnUpdated(Discount updatedDiscount)
        {
            Task.Run(GetActiveDiscount);
        }

        private void DiscountService_OnDeleted(int id)
        {
            Task.Run(GetActiveDiscount);
        }

        #endregion

        #region HostedService

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await GetActiveDiscount();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            
        }

        #endregion
    }
}
