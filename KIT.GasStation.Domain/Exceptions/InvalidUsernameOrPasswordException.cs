using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Exceptions;

/// <summary>
/// Исключение при неверном имени пользователя или пароле.
/// </summary>
public class InvalidUsernameOrPasswordException : DomainException
{
    public string? Username { get; }
    public Shift? Shift { get; }

    public InvalidUsernameOrPasswordException(Shift shift)
        : base("Смена не найдена или уже закрыта.")
    {
        Shift = shift;
    }

    public InvalidUsernameOrPasswordException(Shift shift, string message) : base(message)
    {
        Shift = shift;
    }

    public InvalidUsernameOrPasswordException(string message, string username) : base(message)
    {
        Username = username;
    }

    /// <summary>
    /// Для обратной совместимости (password аргумент игнорируется — больше не хранится в исключениях).
    /// </summary>
    public InvalidUsernameOrPasswordException(string message, string username, string? password)
        : base(message)
    {
        Username = username;
    }
}
