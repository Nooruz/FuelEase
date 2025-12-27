using DevExpress.Mvvm.DataAnnotations;

namespace KIT.GasStation.Hardware.ViewModels
{
    public class KITViewModel : BaseViewModel
    {
        #region Private Members

        private string _url = @"http://77.235.29.171:32156/";

        #endregion

        #region Public Properties



        #endregion

        #region Constructors

        public KITViewModel()
        {
            
        }

        #endregion

        #region Public Commands

        [Command]
        public void Test()
        {

        }

        #endregion
    }
}
