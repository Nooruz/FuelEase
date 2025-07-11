using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Pdf.Native.BouncyCastle.Asn1.BC;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.SplashScreen;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Discounts;
using KIT.GasStation.State.Navigators;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using KIT.GasStation.Views;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels
{
    public class UnregisteredSalePanelViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly IShiftStore _shiftStore;
        private readonly IDisсountStore _disсountStore;
        private readonly ICashRegisterStore _cashRegisterStore;
        private readonly ICustomSplashScreenService _customSplashScreenService;
        private readonly ILogger<UnregisteredSalePanelViewModel> _logger;
        private readonly ILogger<SaleManagerViewModel> _saleManagerViewModelLoger;
        private readonly IUnregisteredSaleService _unregisteredSaleService;
        private readonly IFuelSaleService _fuelSaleService;
        private readonly INavigator _navigation;
        private readonly IFuelService _fuelService;
        private UnregisteredSale _selectedUnregisteredSale = new();
        private ObservableCollection<UnregisteredSale> _unregisteredSales = new();
        private bool _showLoadingPanel;

        #endregion

        #region Public Properties

        public UnregisteredSale SelectedUnregisteredSale
        {
            get => _selectedUnregisteredSale;
            set
            {
                _selectedUnregisteredSale = value;
                OnPropertyChanged(nameof(SelectedUnregisteredSale));
            }
        }
        public ObservableCollection<UnregisteredSale> UnregisteredSales
        {
            get => _unregisteredSales;
            set
            {
                _unregisteredSales = value;
                OnPropertyChanged(nameof(UnregisteredSales));
            }
        }
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

        public UnregisteredSalePanelViewModel(ILogger<UnregisteredSalePanelViewModel> logger,
            ILogger<SaleManagerViewModel> saleManagerViewModelLoger,
            IFuelSaleService fuelSaleService,
            IUnregisteredSaleService unregisteredSaleService,
            IShiftStore shiftStore,
            INavigator navigation,
            ICustomSplashScreenService customSplashScreenService,
            IDisсountStore disсountStore,
            ICashRegisterStore cashRegisterStore,
            IFuelService fuelService)
        {
            _logger = logger;
            _unregisteredSaleService = unregisteredSaleService;
            _saleManagerViewModelLoger = saleManagerViewModelLoger;
            _fuelSaleService = fuelSaleService;
            _shiftStore = shiftStore;
            _navigation = navigation;
            _customSplashScreenService = customSplashScreenService;
            _disсountStore = disсountStore;
            _cashRegisterStore = cashRegisterStore;
            _fuelService = fuelService;

            _unregisteredSaleService.OnCreated += UnregisteredSaleService_OnCreated;
            _unregisteredSaleService.OnUpdated += UnregisteredSaleService_OnUpdated;
            _shiftStore.OnLogin += ShiftStore_OnLogin;
        }

        #endregion

        #region Public Voids

        [Command]
        public void Register()
        {
            try
            {
                _customSplashScreenService.Show();

                WindowService.Title = "Регистрация продаж";
                PayViewModel viewModel = new(_fuelSaleService, _disсountStore, _cashRegisterStore)
                {
                    CreateFuelSale = CreateFuelSale(SelectedUnregisteredSale),
                    SelectedNozzle = SelectedUnregisteredSale.Nozzle
                };
                WindowService.Show(nameof(PayView), viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при открытии окна регистрации продаж", ex);
            }
            finally
            {
                _customSplashScreenService.Close();
            }
        }

        [Command]
        public void Update()
        {
            _ = Task.Run(GetData);
        }

        [Command]
        public async Task SetCurrentPrice()
        {
            try
            {
                if (SelectedUnregisteredSale != null)
                {
                    Fuel? fuel = await _fuelService.GetAsync(SelectedUnregisteredSale.Nozzle.Tank.FuelId);

                    if (fuel != null)
                    {
                        SelectedUnregisteredSale.Sum = SelectedUnregisteredSale.Quantity * fuel.Price;

                        await _unregisteredSaleService.UpdateAsync(SelectedUnregisteredSale.Id, SelectedUnregisteredSale);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при установке цен незарегистрированного отпуска", ex);
            }
        }

        [Command]
        public async Task Delete()
        {
            try
            {
                var result = MessageBoxService
                    .ShowMessage("Вы уверены, что хотите удалить незарегистрированного отпуска?", "Удаление незарегистрированного отпуска", 
                    MessageButton.YesNo, MessageIcon.Question);

                if (result == MessageResult.Yes)
                {
                    SelectedUnregisteredSale.State = UnregisteredSaleState.Deleted;
                    await _unregisteredSaleService.UpdateAsync(SelectedUnregisteredSale.Id, SelectedUnregisteredSale);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError("Ошибка при удалении незарегистрированного отпуска", ex);
            }
        }

        #endregion

        #region Private Voids

        private async Task GetData()
        {
            try
            {
                ShowLoading();
                if (_shiftStore.CurrentShift != null)
                {
                    UnregisteredSales = new(await _unregisteredSaleService.GetUnregisteredSales());
                }
            }
            catch (Exception e)
            {
                _logger.LogError("Ошибка при загрузки данных в OnLoaded", e);
            }
            finally
            {
                CloseLoading();
            }
        }

        private void ShiftStore_OnLogin(Shift shift)
        {
            _ = Task.Run(GetData);
        }

        private void UnregisteredSaleService_OnCreated(UnregisteredSale unregisteredSale)
        {
            try
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    UnregisteredSales.Add(unregisteredSale);
                });
            }
            catch (Exception e)
            {
                _logger.LogError("Ошибка при добавлении незарегистрированного отпуска в UnregisteredSaleService_OnCreated", e);
            }
        }

        private void UnregisteredSaleService_OnUpdated(UnregisteredSale updatedUnregisteredSale)
        {
            try
            {
                UnregisteredSale unregisteredSale = UnregisteredSales
                    .FirstOrDefault(us => us.Id == updatedUnregisteredSale.Id);

                UnregisteredSales.Remove(unregisteredSale);
            }
            catch (Exception e)
            {
                _logger.LogError("Ошибка при изменении незарегистрированного отпуска в UnregisteredSaleService_OnUpdated", e);
            }
        }

        private FuelSale CreateFuelSale(UnregisteredSale unregisteredSale)
        {
            return new()
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

        public async Task StartAsync()
        {
            await GetData();
        }

        private void ShowLoading()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ShowLoadingPanel = true;
            });
        }

        private void CloseLoading()
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                ShowLoadingPanel = false;
            });
        }

        #endregion
    }
}
