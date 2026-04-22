using KIT.GasStation.Domain.Exceptions;
using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services.AuthenticationServices;

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
        User storedUser = await _userService.GetByUsername(username)
            ?? throw new InvalidUsernameOrPasswordException("Неверное имя или пароль.", username);

        // Проверяем через domain method — поддерживает и хеш, и legacy plain-text
        if (!storedUser.VerifyPassword(password ?? string.Empty))
        {
            throw new InvalidUsernameOrPasswordException("Неверное имя или пароль.", username);
        }

        // Миграция: если пользователь ещё хранит plain-text пароль — хешируем при успешном входе
#pragma warning disable CS0618
        if (!string.IsNullOrEmpty(storedUser.Password) &&
            string.IsNullOrEmpty(storedUser.PasswordHash))
        {
            storedUser.SetPassword(password!);
            await _userService.UpdateAsync(storedUser.Id, storedUser);
        }
#pragma warning restore CS0618

        return storedUser;
    }

    public async Task<RegistrationResult> Register(User user, string password, string confirmPassword)
    {
        if (password != confirmPassword)
            return RegistrationResult.PasswordsDoNotMatch;

        try
        {
            User? existing = await _userService.GetByUsername(user.FullName);
            if (existing != null)
                return RegistrationResult.UsernameAlreadyExists;

            user.SetPassword(password);
            user.CreatedDate = DateTime.Now;
            user.CreatedAt = DateTime.UtcNow;

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
            if (!string.IsNullOrEmpty(password))
            {
                if (password != confirmPassword)
                    return RegistrationResult.PasswordsDoNotMatch;

                user.SetPassword(password);
            }

            user.UpdatedAt = DateTime.UtcNow;
            _ = await _userService.UpdateAsync(user.Id, user);
            return RegistrationResult.Success;
        }
        catch
        {
            return RegistrationResult.OtherError;
        }
    }
}
