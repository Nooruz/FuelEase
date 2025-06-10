using DevExpress.Mvvm;
using DevExpress.Mvvm.DataAnnotations;

namespace FuelEase.Hardware.ViewModels
{
    public class ColumnCountViewModel : BaseViewModel
    {
        #region Private Members

        private int _count = 1;

        #endregion

        #region Public Properties

        public int Count
        {
            get => _count;
            set
            {
                _count = value;
                OnPropertyChanged(nameof(Count));
            }
        }

        #endregion

        #region Public Voids

        [Command]
        public void Create()
        {
            if (Count <= 0)
            {
                MessageBoxService.ShowMessage("Введите количество колонок", "Ошибка", MessageButton.OK, MessageIcon.Error);
                return;
            }
            CurrentWindowService?.Close();
        }

        [Command]
        public void Close()
        {
            Count = 0;
            CurrentWindowService?.Close();
        }

        #endregion
    }
}
