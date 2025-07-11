namespace KIT.GasStation.ViewModels.Base
{
    public class PanelViewModel : BaseViewModel
    {
        #region Private Members

        private string _title;

        #endregion

        #region Public Properties

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        #endregion

        public PanelViewModel()
        {
        }
    }
}
