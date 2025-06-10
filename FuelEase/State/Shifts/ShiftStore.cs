using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.State.Users;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FuelEase.State.Shifts
{
    public class ShiftStore : IShiftStore
    {
        #region Private Members

        private readonly IUserStore _userStore;
        private readonly IShiftService _shiftService;
        private Shift? _shift;

        #endregion

        #region Public Properties

        public Shift? CurrentShift
        {
            get => _shift;
            private set => _shift = value;
        }

        public ShiftState CurrentShiftState => CurrentShift != null ? CurrentShift.ShiftState : ShiftState.None;

        #endregion

        #region Actions

        public event Action<Shift> OnOpened;
        public event Action<Shift> OnClosed;
        public event Action<Shift> OnLogin;
        public event Action<Nozzle> OnNozzleSelectionChanged;

        #endregion

        #region Constructors

        public ShiftStore(IUserStore userStore,
            IShiftService shiftService)
        {
            _userStore = userStore;
            _shiftService = shiftService;

            _userStore.OnLogin += UserStoreOnLogin;
            _shiftService.OnCreated += ShiftService_OnCreated;
            _shiftService.OnUpdated += ShiftService_OnUpdated;
        }

        #endregion

        #region Public Voids

        public async Task CloseShiftAsync()
        {
            try
            {
                CurrentShift.ClosedDate = DateTime.Now;
                _ = await _shiftService.UpdateAsync(CurrentShift.Id, CurrentShift);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public async Task<bool> OpenShiftAsync()
        {
            try
            {
                CurrentShift = null;

                CurrentShift = await _shiftService.CreateAsync(new Shift
                {
                    OpeningDate = DateTime.Now,
                    UserId = _userStore.CurrentUser.Id
                });
                return CurrentShift != null;
            }
            catch (Exception)
            {
                //ignore
                return false;
            }
        }

        public async Task ReOpeningShiftAsync()
        {
            try
            {
                if (CurrentShift != null)
                {
                    await CloseShiftAsync();
                    await OpenShiftAsync();
                }
            }
            catch (Exception)
            {
                //ignore
            }
        }

        public void NozzleSelectionChanged(Nozzle nozzle)
        {
            OnNozzleSelectionChanged?.Invoke(nozzle);
        }

        #endregion

        #region Private Voids

        private void UserStoreOnLogin(User user)
        {
            _ = Task.Run(async () =>
            {
                CurrentShift = await _shiftService.GetOpenShiftAsync(user.Id);

                //if (CurrentShift == null)
                //{
                //    await OpenShiftAsync();
                //}

                //if (CurrentShiftState is ShiftState.None or ShiftState.Closed)
                //{
                //    await OpenShiftAsync();
                //}

                OnLogin?.Invoke(CurrentShift);
            });
        }

        private void ShiftService_OnUpdated(Shift shift)
        {
            CurrentShift = shift;
            if (shift.ShiftState == ShiftState.Closed)
            {
                OnClosed?.Invoke(shift);
            }
        }

        private void ShiftService_OnCreated(Shift shift)
        {
            CurrentShift = shift;
            OnOpened?.Invoke(shift);
        }

        #endregion

        #region Hosted Service

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            
        }

        #endregion
    }
}
