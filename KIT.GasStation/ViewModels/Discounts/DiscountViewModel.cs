using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.EntityFramework.Services;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Factories;
using KIT.GasStation.Views.Discounts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace KIT.GasStation.ViewModels.Discounts
{
    public class DiscountViewModel : BaseViewModel, IAsyncInitializable
    {
        #region Private Members

        private readonly IDiscountService _discountService;
        private readonly IFuelService _fuelService;
        private readonly ILogger<DiscountViewModel> _logger;
        private ObservableCollection<Discount> _discounts = new();
        private Discount _selectedDiscount;

        #endregion

        #region Public Properties

        public ObservableCollection<Discount> Discounts
        {
            get => _discounts;
            set
            {
                _discounts = value;
                OnPropertyChanged(nameof(Discounts));
            }
        }
        public Discount SelectedDiscount
        {
            get => _selectedDiscount;
            set
            {
                _selectedDiscount = value;
                OnPropertyChanged(nameof(SelectedDiscount));
            }
        }

        #endregion

        #region Constructors

        public DiscountViewModel(ILogger<DiscountViewModel> logger,
            IDiscountService discountService,
            IFuelService fuelService)
        {
            _logger = logger;
            _discountService = discountService;
            _fuelService = fuelService;

            Title = "Скидки";

            _discountService.OnDeleted += DiscountService_OnDeleted;
            _discountService.OnCreated += DiscountService_OnCreated;
            _discountService.OnUpdated += DiscountService_OnUpdated;
        }

        #endregion

        #region Public Voids

        [Command]
        public void Create()
        {
            WindowService.Title = "Создание";
            WindowService.Show(nameof(DiscountDetailView), new DiscountDetailViewModel(_discountService, _fuelService));
        }

        [Command]
        public void Edit()
        {
            WindowService.Title = $"Редактирование \"{SelectedDiscount.Name}\"";
            WindowService.Show(nameof(DiscountDetailView), 
                new DiscountDetailViewModel(_discountService, _fuelService) { SelectedDiscount = SelectedDiscount } );
        }

        [Command]
        public async Task Delete()
        {
            if (SelectedDiscount != null)
            {
                var result = MessageBoxService.ShowMessage("Удалить выбранный элемент?", "Внимание", MessageButton.YesNo, MessageIcon.Question);
                if (result == MessageResult.Yes)
                {
                    await _discountService.DeleteAsync(SelectedDiscount.Id);
                }
            }
        }

        public async Task StartAsync()
        {
            Discounts = new(await _discountService.GetAllAsync());
        }

        #endregion

        #region Private Members

        private void DiscountService_OnUpdated(Discount updatedDiscount)
        {
            Discount? discount = Discounts.FirstOrDefault(d => d.Id == updatedDiscount.Id);
            discount?.Update(updatedDiscount);
        }

        private void DiscountService_OnCreated(Discount createdDiscount)
        {
            Discounts.Add(createdDiscount);
        }

        private void DiscountService_OnDeleted(int id)
        {
            Discount? discount = Discounts.FirstOrDefault(d => d.Id == id);

            if (discount != null)
            {
                Discounts.Remove(discount);
            }
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _discountService.OnDeleted -= DiscountService_OnDeleted;
                _discountService.OnCreated -= DiscountService_OnCreated;
                _discountService.OnUpdated -= DiscountService_OnUpdated;
            }

            base.Dispose(disposing);
        }

        #endregion
    }
}
