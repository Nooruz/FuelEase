using DevExpress.Mvvm.DataAnnotations;
using FuelEase.Domain.Models;
using FuelEase.SplashScreen;
using FuelEase.State.Shifts;
using FuelEase.ViewModels.Base;
using System.Threading.Tasks;

namespace FuelEase.ViewModels
{
    public class OpenShiftViewModel : BaseViewModel
    {
        #region Private Members

        private readonly IShiftStore _shiftStore;
        private readonly ICustomSplashScreenService _customSplashScreenService;

        #endregion

        #region Constructors

        public OpenShiftViewModel(IShiftStore shiftStore,
            ICustomSplashScreenService customSplashScreenService)
        {
            _shiftStore = shiftStore;
            _customSplashScreenService = customSplashScreenService;

            _shiftStore.OnOpened += ShiftStore_OnOpened;
        }

        #endregion

        #region Public Voids

        [Command]
        public async Task OpenShift()
        {
            _customSplashScreenService.Show("Открытие смены", "Подождите...");
            await _shiftStore.OpenShiftAsync();
        }

        [Command]
        public async Task CloseShift()
        {
            //await _cashRegisterManager.CloseShiftAsync();
        }

        [Command]
        public void OpenGlobalReport()
        {

        }

        [Command]
        public void CloseApp()
        {

        }

        #endregion

        #region Private Voids

        private void ShiftStore_OnOpened(Shift obj)
        {
            _customSplashScreenService.Close();
            CurrentWindowService?.Close();
        }

        #endregion
    }
}
