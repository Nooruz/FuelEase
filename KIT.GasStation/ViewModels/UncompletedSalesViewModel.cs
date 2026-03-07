using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Helpers;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.State.Users;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels
{
    public class UncompletedSalesViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly ILogger<UncompletedSalesViewModel> _logger;
        private readonly IShiftStore _shiftStore;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly IFiscalDataService _fiscalDataService;
        private readonly ICashRegisterStore _cashRegisterStore;
        private readonly IUserStore _userStore;
        private ObservableCollection<FuelSale> _fuelSales = new();
        private ObservableCollection<FuelSale> _selectedFuelSales = new();
        private bool _showLoadingPanel;

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
        public ObservableCollection<FuelSale> SelectedFuelSales
        {
            get => _selectedFuelSales;
            set
            {
                _selectedFuelSales = value;
                OnPropertyChanged(nameof(SelectedFuelSales));
            }
        }
        public List<KeyValuePair<PaymentType, string>> PaymentTypes => new(EnumHelper.GetLocalizedEnumValues<PaymentType>());
        public List<KeyValuePair<FuelSaleStatus, string>> FuelSaleStatuses => new(EnumHelper.GetLocalizedEnumValues<FuelSaleStatus>());
        public bool ShowLoadingPanel
        {
            get => _showLoadingPanel;
            set
            {
                _showLoadingPanel = value;
                OnPropertyChanged(nameof(ShowLoadingPanel));
            }
        }

        #endregion

        #region Constructor

        public UncompletedSalesViewModel(ILogger<UncompletedSalesViewModel> logger,
            IShiftStore shiftStore,
            IFuelSaleService fuelSaleService,
            ICashRegisterStore cashRegisterStore,
            IUserStore userStore,
            IFiscalDataService fiscalDataService)
        {
            _logger = logger;
            _shiftStore = shiftStore;
            _fuelSaleService = fuelSaleService;
            _cashRegisterStore = cashRegisterStore;
            _userStore = userStore;
            _fiscalDataService = fiscalDataService;
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
            try
            {
                if (SelectedFuelSales.Any())
                {
                    var result = MessageBoxService.ShowMessage("Сделать возврат ККМ?", "Завершение", MessageButton.YesNoCancel);
                    if (result == MessageResult.Cancel)
                    {
                        return;
                    }
                    if (result == MessageResult.Yes)
                    {
                        ShowLoadingPanel = true;
                        foreach (var item in SelectedFuelSales)
                        {
                            var createdFiscalData = item.CreateFiscalData(OperationType.Return);

                            var fiscalData = await _cashRegisterStore.ReturnAsync(createdFiscalData);
                            if (fiscalData != null)
                            {
                                await _fiscalDataService.CreateAsync(fiscalData);
                            }
                            item.FuelSaleStatus = FuelSaleStatus.Completed;
                            await _fuelSaleService.UpdateAsync(item.Id, item);
                        }
                    }
                    else
                    {
                        ShowLoadingPanel = true;
                        foreach (var item in SelectedFuelSales)
                        {
                            item.FuelSaleStatus = FuelSaleStatus.Completed;
                            await _fuelSaleService.UpdateAsync(item.Id, item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK);
            }
            finally
            {
                ShowLoadingPanel = false;
                await GetData();
            }
        }

        /// <summary>
        /// Продолжить
        /// </summary>
        /// <returns></returns>
        [Command]
        public void ContinueFueling()
        {
            if (SelectedFuelSales.Any())
            {
                foreach (var item in SelectedFuelSales)
                {
                    _fuelSaleService.ResumeFueling(item);
                }
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

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #endregion
    }
}
