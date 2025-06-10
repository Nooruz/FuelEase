using FuelEase.Domain.Exceptions;
using FuelEase.Domain.Models;

namespace FuelEase.Domain.Services.AuthenticationServices
{
    public class AuthenticationService : IAuthenticationService
    {
        #region Private Members

        private readonly IUserService _userService;

        #endregion

        #region Constructor

        public AuthenticationService(IUserService userService)
        {
            _userService = userService;
        }

        #endregion

        public async Task<User> Login(string username, string? password)
        {
            User storedUser = await _userService.GetByUsername(username) ?? throw new InvalidUsernameOrPasswordException("Неверное имя или пароль.", username, password);

            // Если storedUser.Password равен null, то он становится пустой строкой и сравнение будет корректным
            if ((storedUser.Password ?? string.Empty) != (password ?? string.Empty))
            {
                throw new InvalidUsernameOrPasswordException("Неверное имя или пароль.", username, password);
            }

            return storedUser;
        }

        public async Task<RegistrationResult> Register(User user, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                return RegistrationResult.PasswordsDoNotMatch;
            }

            try
            {
                User editingUser = await _userService.GetByUsername(user.FullName);
                if (editingUser != null)
                {
                    return RegistrationResult.UsernameAlreadyExists;
                }

                user.Password = password;

                _ = await _userService.CreateAsync(user);
                return RegistrationResult.Success;
            }
            catch
            {
                return RegistrationResult.OtherError;
            }
        }

        public async Task<RegistrationResult> Update(User user, string password, string confirmPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(password))
                {
                    _ = await _userService.UpdateAsync(user.Id, user);
                }
                else
                {
                    if (password != confirmPassword)
                    {
                        return RegistrationResult.PasswordsDoNotMatch;
                    }

                    user.Password = password;

                    _ = await _userService.UpdateAsync(user.Id, user);
                }
                return RegistrationResult.Success;
            }
            catch
            {
                return RegistrationResult.OtherError;
            }
        }
    }
}
