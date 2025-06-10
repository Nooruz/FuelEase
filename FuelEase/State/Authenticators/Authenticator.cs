using FuelEase.Domain.Models;
using FuelEase.Domain.Services.AuthenticationServices;
using FuelEase.State.Navigators;
using FuelEase.State.Users;
using FuelEase.ViewModels.Factories;
using System;
using System.Threading.Tasks;

namespace FuelEase.State.Authenticators
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
