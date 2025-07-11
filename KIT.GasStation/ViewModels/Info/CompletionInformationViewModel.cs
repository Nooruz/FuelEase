using KIT.GasStation.Domain.Models;
using KIT.GasStation.ViewModels.Base;

namespace KIT.GasStation.ViewModels.Info
{
    public class CompletionInformationViewModel : BaseViewModel
    {
        #region Private Members

        private FuelSale _completedFuelSale;
        private decimal _return;
        private string _info;

        #endregion

        #region Public Properties

        public FuelSale CompletedFuelSale
        {
            get => _completedFuelSale;
            set
            {
                _completedFuelSale = value;
                OnPropertyChanged(nameof(CompletedFuelSale));
                Return = CompletedFuelSale.Sum - CompletedFuelSale.ReceivedSum;
                Info = $"Заявка:\n" +
                       $"{CompletedFuelSale.Tank.Fuel.Name} - {CompletedFuelSale.Quantity:N2} л - {CompletedFuelSale.Sum:N2} сом \n\n" +
                       $"Отпущено:\n" +
                       $"{CompletedFuelSale.Tank.Fuel.Name} - {CompletedFuelSale.ReceivedQuantity:N2} л - {CompletedFuelSale.ReceivedSum:N2} сом";
            }
        }
        public decimal Return
        {
            get => _return;
            set
            {
                _return = value;
                OnPropertyChanged(nameof(Return));
            }
        }
        public string Info
        {
            get => _info;
            set
            {
                _info = value;
                OnPropertyChanged(nameof(Info));
            }
        }
            

        #endregion

        #region Constructor

        public CompletionInformationViewModel(FuelSale fuelSale)
        {
            CompletedFuelSale = fuelSale;
        }

        #endregion
    }
}
