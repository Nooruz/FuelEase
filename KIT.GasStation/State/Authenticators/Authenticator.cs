using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services.AuthenticationServices;
using KIT.GasStation.State.Navigators;
using KIT.GasStation.State.Users;
using KIT.GasStation.ViewModels.Factories;
using System;
using System.Threading.Tasks;

namespace KIT.GasStation.State.Authenticators
{
    public class Authenticator : IAuthenticator
    {
        #region Private Members

        private readonly IAuthenticationService _authenticationService;
        private readonly IUserStore _userStore;

        #endregion

        #region Constructor

        public Authenticator(IAuthenticationService authenticationService,
            IUserStore userStore)
        {
            _authenticationService = authenticationService;
            _userStore = userStore;
        }

        #endregion

        #region Public Properties

        public User? CurrentUser
        {
            get => _userStore.CurrentUser;
            private set
            {
                _userStore.CurrentUser = value;
                StateChanged?.Invoke();
            }
        }

        public event Action StateChanged;

        #endregion

        public async Task Login(string username, string? password)
        {
            CurrentUser = await _authenticationService.Login(username, password);
        }

        public void Logout()
        {
            CurrentUser = null;
        }

        public async Task<RegistrationResult> Register(User user, string password, string confirmPassword)
        {
            return await _authenticationService.Register(user, password, confirmPassword);
        }

        public async Task<RegistrationResult> Update(User user, string password, string confirmPassword)
        {
            return await _authenticationService.Update(user, password, confirmPassword);
        }
    }
}
