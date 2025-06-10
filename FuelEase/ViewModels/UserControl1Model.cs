using DevExpress.Mvvm.DataAnnotations;
using FuelEase.ViewModels.Base;
using System.Collections.ObjectModel;
using System.Linq;

namespace FuelEase.ViewModels
{
    public class UserControl1Model : BaseViewModel
    {
        private ObservableCollection<BaseViewModel> _baseViewModels = new();

        public ObservableCollection<BaseViewModel> BaseViewModels
        {
            get => _baseViewModels;
            set
            {
                _baseViewModels = value;
            }
        }


        [Command]
        public void CloseTabe()
        {
            BaseViewModel? baseViewModel = BaseViewModels.FirstOrDefault();
            if (baseViewModel != null)
            {
                BaseViewModels.Remove(baseViewModel);
            }
        }

        [Command]
        public void AddTab()
        {
            BaseViewModel baseViewModel = new() { Title = "Новый" };
            BaseViewModels.Add(baseViewModel);
        }

    }
}
