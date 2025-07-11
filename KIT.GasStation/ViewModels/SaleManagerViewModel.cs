using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Helpers;
using KIT.GasStation.ViewModels.Base;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels
{
    public class SaleManagerViewModel : BaseViewModel
    {
        #region Private Members

        private readonly ILogger<SaleManagerViewModel> _logger;
        private readonly IFuelSaleService _fuelSaleService;
        private UnregisteredSale _selectedUnregisteredSale = new();
        private FuelSale _createdFuelSale = new();

        #endregion

        #region Public Properties

        public List<KeyValuePair<PaymentType, string>> PaymentTypes => new(EnumHelper.GetLocalizedEnumValues<PaymentType>());
        public UnregisteredSale SelectedUnregisteredSale
        {
            get => _selectedUnregisteredSale;
            set
            {
                _selectedUnregisteredSale = value;
                OnPropertyChanged(nameof(SelectedUnregisteredSale));
                CreateFuelSale(SelectedUnregisteredSale);
            }
        }
        public FuelSale CreatedFuelSale
        {
            get => _createdFuelSale;
            set
            {
                _createdFuelSale = value;
                OnPropertyChanged(nameof(CreatedFuelSale));
            }
        }

        #endregion

        #region Constructor

        public SaleManagerViewModel(ILogger<SaleManagerViewModel> logger,
            IFuelSaleService fuelSaleService)
        {
            _logger = logger;
            _fuelSaleService = fuelSaleService;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task Sale()
        {
            try
            {
                await _fuelSaleService.CreateAsync(CreatedFuelSale);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при продаже", ex);
            }
        }

        #endregion

        #region Private Voids

        private void CreateFuelSale(UnregisteredSale unregisteredSale)
        {
            try
            {
                CreatedFuelSale = new FuelSale
                {
                    PaymentType = PaymentType.Cash,
                    TankId = unregisteredSale.Nozzle.TankId,
                    NozzleId = unregisteredSale.NozzleId,
                    CreateDate = DateTime.Now,
                    Price = unregisteredSale.Nozzle.Tank.Fuel.Price,
                    Sum = unregisteredSale.Sum,
                    Quantity = unregisteredSale.Quantity,
                    ReceivedSum = unregisteredSale.Sum,
                    ReceivedQuantity = unregisteredSale.Quantity,
                    ShiftId = unregisteredSale.ShiftId,
                    FuelSaleStatus = FuelSaleStatus.Completed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при создании продажи", ex);
            }
        }

        #endregion
    }
}
