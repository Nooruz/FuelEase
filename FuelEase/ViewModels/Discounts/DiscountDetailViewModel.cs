using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using FuelEase.Domain.Models;
using FuelEase.Domain.Models.Discounts;
using FuelEase.Domain.Services;
using FuelEase.ViewModels.Base;
using FuelEase.ViewModels.Factories;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FuelEase.ViewModels.Discounts
{
    public class DiscountDetailViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly IDiscountService _discountService;
        private readonly IFuelService _fuelService;
        private Discount _selectedDiscount = new() { StartDate = DateTime.Now, EndDate = DateTime.Now.AddDays(30) };
        private DiscountFuel _selectedDiscountFuel;
        private DiscountTariffPlan _selectedDiscountTariffPlan;
        private ObservableCollection<Fuel> _fuels = new();

        #endregion

        #region Public Properties

        public Discount SelectedDiscount
        {
            get => _selectedDiscount;
            set
            {
                _selectedDiscount = value;
                OnPropertyChanged(nameof(SelectedDiscount));
            }
        }
        public ObservableCollection<Fuel> Fuels
        {
            get => _fuels;
            set
            {
                _fuels = value;
                OnPropertyChanged(nameof(Fuels));
            }
        }
        public DiscountFuel SelectedDiscountFuel
        {
            get => _selectedDiscountFuel;
            set
            {
                _selectedDiscountFuel = value;
                OnPropertyChanged(nameof(SelectedDiscountFuel));
            }
        }
        public DiscountTariffPlan SelectedDiscountTariffPlan
        {
            get => _selectedDiscountTariffPlan;
            set
            {
                _selectedDiscountTariffPlan = value;
                OnPropertyChanged(nameof(SelectedDiscountTariffPlan));
            }
        }

        #endregion

        #region Constructors

        public DiscountDetailViewModel(IDiscountService discountService,
            IFuelService fuelService)
        {
            _discountService = discountService;
            _fuelService = fuelService;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task Save()
        {
            try
            {
                if (CheckValidation())
                {
                    if (SelectedDiscount.Id == 0)
                    {
                        await _discountService.CreateAsync(SelectedDiscount);
                        if (SelectedDiscount.Id != 0)
                        {
                            //ShowNotification("Создание скидки", $"Скидка \"{SelectedDiscount.Name}\" успешно создана.");
                        }
                    }
                    else
                    {
                        var updatedEntity = await _discountService.UpdateAsync(SelectedDiscount.Id, SelectedDiscount);
                        if (updatedEntity != null)
                        {
                            //ShowNotification("Обновление скидки", $"Скидка \"{SelectedDiscount.Name}\" успешно обновлена.");
                        }
                        else
                        {
                            //ShowNotification("Обновление скидки", $"Ошибка при обновлении скидки \"{SelectedDiscount.Name}\".");
                        }
                    }
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        [Command]
        public void DeleteFuel()
        {
            try
            {
                SelectedDiscount.DiscountFuels.Remove(SelectedDiscountFuel);
            }
            catch (Exception)
            {

            }
        }

        [Command]
        public void DeleteDiscountTariffPlan()
        {
            try
            {
                SelectedDiscount.DiscountTariffPlans.Remove(SelectedDiscountTariffPlan);
            }
            catch (Exception)
            {

            }
        }

        public async Task StartAsync()
        {
            Fuels = new(await _fuelService.GetAllAsync());
        }

        #endregion

        #region Private Voids

        private bool CheckValidation()
        {
            if (string.IsNullOrEmpty(SelectedDiscount.Name))
            {
                _ = MessageBoxService.ShowMessage("Введите наименование!", "Внимание", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }

            if (SelectedDiscount.EndDate < SelectedDiscount.StartDate)
            {
                
                _ = MessageBoxService.ShowMessage("Дата окончания должна быть после даты начала!", "Внимание", MessageButton.OK, MessageIcon.Exclamation);
                return false;
            }

            return true;
        }

        #endregion
    }
}
