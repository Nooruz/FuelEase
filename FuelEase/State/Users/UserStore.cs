using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using System;

namespace FuelEase.State.Users
{
    public class UserStore : IUserStore
    {
        private readonly IUserService _userService;
        private User? _currentUser;

        public UserStore(IUserService userService)
        {
            _userService = userService;

            _userService.OnUpdated += UserService_OnUpdated;
        }

        private void UserService_OnUpdated(User updatedUser)
        {
            try
            {
                if (CurrentUser != null && CurrentUser.Equals(updatedUser))
                {
                    CurrentUser.UpdateFrom(updatedUser);
                }
            }
            catch (Exception)
            {

            }
        }

        

        public User? CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                if (CurrentUser == null)
                {
                    OnLogout?.Invoke();
                }
                else
                {
                    OnLogin?.Invoke(CurrentUser);
                }
            }
        }

        public event Action<User> OnLogin;
        public event Action OnLogout;
    }
}
