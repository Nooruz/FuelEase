using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using DevExpress.Xpf.Editors;
using KIT.GasStation.CashRegisters.Exceptions;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Models.CashRegisters;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.SplashScreen;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Discounts;
using KIT.GasStation.ViewModels.Base;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace KIT.GasStation.ViewModels
{
    /// <summary>
    /// Представляет модель представления для обработки платежей на ККМ.
    /// </summary>
    public class PayViewModel : BaseViewModel
    {
        #region Private Members

        private readonly IFuelSaleService _fuelSaleService;
        private readonly IFiscalDataService _fiscalDataService;
        private readonly IDisсountStore _disсountStore;
        private readonly ICashRegisterStore _cashRegisterStore;
        private readonly ICustomSplashScreenService _splashScreenService;
        private FuelSale _createFuelSale;
        private Nozzle _selectedNozzle;
        private decimal _paySum;
        private decimal _change;

        #endregion

        #region Public Properties

        /// <summary>
        /// Продажа топлива, которую необходимо создать.
        /// </summary>
        public FuelSale CreateFuelSale
        {
            get => _createFuelSale;
            set
            {
                _createFuelSale = value;
                OnPropertyChanged(nameof(CreateFuelSale));
                PaySum = Amount;
            }
        }

        /// <summary>
        /// Выбранный пистолет для продажи топлива.
        /// </summary>
        public Nozzle SelectedNozzle
        {
            get => _selectedNozzle;
            set
            {
                _selectedNozzle = value;
                OnPropertyChanged(nameof(SelectedNozzle));
            }
        }

        /// <summary>
        /// Строка, представляющая цену с учетом скидки, если применимо.
        /// </summary>
        public string PriceCaption => CreateFuelSale.DiscountSale != null ? $"{CreateFuelSale.Price:N2} | Скидка: {CreateFuelSale.Price - CreateFuelSale.DiscountSale.DiscountPrice:N2} | Итого: {CreateFuelSale.DiscountSale.DiscountPrice:N2}" : $"{CreateFuelSale.Price:N2}";

        /// <summary>
        /// Строка, представляющая количество с учетом скидки, если применимо.
        /// </summary>
        public string QuantityCaption => CreateFuelSale.DiscountSale != null ? $"{CreateFuelSale.Quantity:N3} | Скидка: {CreateFuelSale.DiscountSale.DiscountQuantity:N3} | Итого: {CreateFuelSale.Quantity + CreateFuelSale.DiscountSale.DiscountQuantity:N3}" : $"{CreateFuelSale.Quantity:N3}";

        /// <summary>
        /// Строка, представляющая итоговую стоимость с учетом скидки, если применимо.
        /// </summary>
        public string CoustCaption => CreateFuelSale.DiscountSale != null ? $"{CreateFuelSale.Sum} | Скидка: {CreateFuelSale.DiscountSale.DiscountSum:N2} | Итого: {CreateFuelSale.Sum - CreateFuelSale.DiscountSale.DiscountSum:N2}" : $"{CreateFuelSale.Sum:N2}";

        /// <summary>
        /// Сумма клиента.
        /// </summary>
        public decimal PaySum
        {
            get => _paySum;
            set
            {
                _paySum = value;
                OnPropertyChanged(nameof(PaySum));
                OnPropertyChanged(nameof(Change));// Обновляем сдачу при изменении суммы оплаты
            }
        }

        /// <summary>
        /// Сдача, которая должна быть возвращена покупателю.
        /// </summary>
        public decimal Change => PaySum - Amount;

        /// <summary>
        /// Итоговая сумма, с учетом всех скидок.
        /// </summary>
        public decimal Amount => CreateFuelSale.DiscountSale != null ? CreateFuelSale.Sum - CreateFuelSale.DiscountSale.DiscountSum : CreateFuelSale.Sum;

        #endregion

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр класса <see cref="PayViewModel"/>.
        /// </summary>
        public PayViewModel(IFuelSaleService fuelSaleService,
            IDisсountStore disсountStore,
            ICashRegisterStore cashRegisterStore,
            IFiscalDataService fiscalDataService,
            ICustomSplashScreenService splashScreenService)
        {
            _fuelSaleService = fuelSaleService;
            _disсountStore = disсountStore;
            _cashRegisterStore = cashRegisterStore;
            _fiscalDataService = fiscalDataService;
            _splashScreenService = splashScreenService;
        }

        #endregion

        #region Public Voids

        /// <summary>
        /// Создает запись о продаже топлива и, если необходимо, обрабатывает оплату через ККМ.
        /// </summary>
        [Command]
        public async Task Create()
        {
            try
            {
                // Скрываем текущее окно, чтобы предотвратить взаимодействие пользователя во время обработки продажи
                CurrentWindowService.Hide();

                _splashScreenService.Show("Обработка продажи...");

                // Устанавливаем дату создания продажи
                CreateFuelSale.CreateDate = DateTime.Now;

                if (Properties.Settings.Default.ReceiptPrintingMode == "Before")
                {
                    var newFilscalData = CreateFuelSale.CreateFiscalData();

                    if (CreateFuelSale.PaymentType is PaymentType.Cash or PaymentType.Cashless)
                    {
                        newFilscalData.Discount = null;
                    }
                    else
                    {
                        newFilscalData.Discount = new FiscalDiscount
                        {
                            Amount = CreateFuelSale.Sum
                        };
                    }

                    var fiscalData = await _cashRegisterStore.SaleAsync(newFilscalData);

                    if (fiscalData is not null)
                    {
                        await _fuelSaleService.CreateAsync(CreateFuelSale);

                        // Сохраняем фискальные данные, связанные с продажей топлива. Id FuelSale будет присвоен после сохранения продажи,
                        // поэтому сохраняем его после создания записи о продаже.
                        fiscalData.FuelSaleId = CreateFuelSale.Id;
                        await _fiscalDataService.CreateAsync(fiscalData);
                    }
                    else
                    {
                        MessageBoxService.ShowMessage("Не удалось получить фискальные данные от ККМ.", "Ошибка", MessageButton.OK, MessageIcon.Error);
                    }
                }
                else
                {
                    await _fuelSaleService.CreateAsync(CreateFuelSale);
                }
            }
            catch (CashRegisterException e)
            {
                MessageBoxService.ShowMessage(e.Message, "Информация");
            }
            catch (Exception e)
            {
                MessageBoxService.ShowMessage(e.Message, "Ошибка", MessageButton.OK, MessageIcon.Error);
            }
            finally
            {
                CurrentWindowService.Close();
                _splashScreenService.Close();
            }
        }

        [Command]
        public void PaySumLoaded(RoutedEventArgs args)
        {
            if (args.Source is TextEdit textEdit)
            {
                textEdit.Focus();
                textEdit.SelectAll();
            }
        }

        #endregion

        #region Private Voids

        //private void CashRegisterStore_OnStatusChanged(CashRegisterStatus status)
        //{
        //    string? message = status switch
        //    {
        //        CashRegisterStatus.Exceeded24Hours => "Смена на ККМ открыта более 24 часов. Пожалуйста, закройте смену и откройте новую.",
        //        CashRegisterStatus.Close => "Смена на ККМ закрыта. Пожалуйста, откройте новую смену перед началом работы.",
        //        CashRegisterStatus.Error => "Ошибка ККМ. Проверьте соединение с сервером или настройки кассы.",
        //        CashRegisterStatus.Unknown => "Статус ККМ неизвестен. Проверьте работу ККМ.",
        //        CashRegisterStatus.NoOpenedShift => "Смена на ККМ не открыта. Откройте смену перед началом работы.",
        //        _ => null
        //    };

        //    if (message != null)
        //    {
        //        _ = MessageBoxService.ShowMessage(message, "Внимание!", MessageButton.OK, MessageIcon.Warning);
        //    }
        //}

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {

            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
