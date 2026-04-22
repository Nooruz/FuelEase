namespace KIT.GasStation.Domain.Exceptions;

/// <summary>
/// Базовое исключение домена. Выбрасывается при нарушении бизнес-правил или инвариантов.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException) : base(message, innerException) { }
}
