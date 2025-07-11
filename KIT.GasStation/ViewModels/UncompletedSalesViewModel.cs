using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Helpers;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace KIT.GasStation.ViewModels
{
    public class UncompletedSalesViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly ILogger<UncompletedSalesViewModel> _logger;
        private readonly IShiftStore _shiftStore;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly ICashRegisterStore _cashRegisterStore;
        private ObservableCollection<FuelSale> _fuelSales = new();
        private FuelSale _selectedFuelSale;

        #endregion

        #region Public Properties

        public ObservableCollection<FuelSale> FuelSales
        {
            get => _fuelSales;
            set
            {
                _fuelSales = value;
                OnPropertyChanged(nameof(FuelSales));
            }
        }
        public FuelSale SelectedFuelSale
        {
            get => _selectedFuelSale;
            set
            {
                _selectedFuelSale = value;
                OnPropertyChanged(nameof(SelectedFuelSale));
            }
        }
        public List<KeyValuePair<PaymentType, string>> PaymentTypes => new(EnumHelper.GetLocalizedEnumValues<PaymentType>());
        public List<KeyValuePair<FuelSaleStatus, string>> FuelSaleStatuses => new(EnumHelper.GetLocalizedEnumValues<FuelSaleStatus>());

        #endregion

        #region Constructor

        public UncompletedSalesViewModel(ILogger<UncompletedSalesViewModel> logger,
            IShiftStore shiftStore,
            IFuelSaleService fuelSaleService,
            ICashRegisterStore cashRegisterStore)
        {
            _logger = logger;
            _shiftStore = shiftStore;
            _fuelSaleService = fuelSaleService;
            _cashRegisterStore = cashRegisterStore;

            _fuelSaleService.OnUpdated += FuelSaleService_OnUpdated;
            _fuelSaleService.OnDeleted += DeleteFuelSale;
            _cashRegisterStore.OnReturning += CashRegisterStore_OnReturning;
        }

        #endregion

        #region Public Voids

        /// <summary>
        /// Завершить
        /// </summary>
        /// <returns></returns>
        [Command]
        public async Task CompleteFuelSale()
        {
            if (SelectedFuelSale != null)
            {
                var result = MessageBoxService.ShowMessage("Сделать возврат ККМ?", "Завершение", MessageButton.YesNoCancel);
                if (result == MessageResult.Cancel)
                {
                    return;
                }
                if (result == MessageResult.Yes)
                {
                    await _cashRegisterStore.ReturnAndReceivedSaleAsync(SelectedFuelSale, SelectedFuelSale.Tank.Fuel);
                }
                else 
                {
                    SelectedFuelSale.FuelSaleStatus = FuelSaleStatus.Completed;
                    await _fuelSaleService.UpdateAsync(SelectedFuelSale.Id, SelectedFuelSale);
                }
            }
        }

        /// <summary>
        /// Продолжить
        /// </summary>
        /// <returns></returns>
        [Command]
        public void ContinueFilling()
        {
            if (SelectedFuelSale != null && SelectedFuelSale.Nozzle != null)
            {
                _fuelSaleService.ContinueFilling(SelectedFuelSale.Nozzle);
            }
        }

        public async Task StartAsync()
        {
            await GetData();
        }

        #endregion

        #region Private Voids

        private async Task GetData()
        {
            try
            {
                FuelSales = new(await _fuelSaleService.GetUncompletedFuelSaleAsync(_shiftStore.CurrentShift.Id));
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
            }
        }

        private void FuelSaleService_OnUpdated(FuelSale updatedFuelSale)
        {
            FuelSale? fuelSale = FuelSales.FirstOrDefault(f => f.Id == updatedFuelSale.Id);
            if (fuelSale != null)
            {
                fuelSale.Update(updatedFuelSale);
            }
            else
            {
                AddFuelSale(updatedFuelSale);
            }
            switch (updatedFuelSale.FuelSaleStatus)
            {
                case FuelSaleStatus.None:
                    break;
                case FuelSaleStatus.InProgress:
                    break;
                case FuelSaleStatus.Completed:
                    DeleteFuelSale(updatedFuelSale.Id);
                    break;
                case FuelSaleStatus.Uncompleted:
                    AddFuelSale(updatedFuelSale);
                    break;
                default:
                    break;
            }
        }

        private void AddFuelSale(FuelSale fuelSale)
        {
            if (!FuelSales.Any(f => f.Id == fuelSale.Id))
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    FuelSales.Add(fuelSale);
                });
            }
        }

        private void DeleteFuelSale(int id)
        {
            FuelSale? deletingFuelSale = FuelSales.FirstOrDefault(f => f.Id == id);
            if (deletingFuelSale != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _ = FuelSales.Remove(deletingFuelSale);
                });
            }
        }

        private void CashRegisterStore_OnReturning(FuelSale returnedFuelSale)
        {
            try
            {
                FuelSale? fuelSale = FuelSales.FirstOrDefault(f => f.Id == returnedFuelSale.Id);

                if (fuelSale == null) return;

                fuelSale.FuelSaleStatus = FuelSaleStatus.Completed;

                _ = Task.Run(async () =>
                {
                    _ = await _fuelSaleService.UpdateAsync(fuelSale.Id, fuelSale);
                });

                FuelSales.Remove(fuelSale);
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e);
            }
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fuelSaleService.OnUpdated -= FuelSaleService_OnUpdated;
                _fuelSaleService.OnDeleted -= DeleteFuelSale;
                _cashRegisterStore.OnReturning -= CashRegisterStore_OnReturning;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
