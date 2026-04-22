using KIT.GasStation.Domain.Exceptions;
using KIT.GasStation.Domain.Utilities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Пользователь системы (кассир / администратор)
/// </summary>
[Display(Name = "Пользователь")]
public class User : DomainObject
{
    #region Persisted Properties

    /// <summary>ФИО пользователя</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Хеш пароля (PBKDF2-SHA256, Base64). Используется вместо Password.
    /// </summary>
    public string? PasswordHash { get; set; }

    /// <summary>
    /// Соль для хеша пароля (Base64). Используется вместо Password.
    /// </summary>
    public string? PasswordSalt { get; set; }

    /// <summary>
    /// Устаревшее поле — plain-text пароль. Оставлено для совместимости и миграции.
    /// После миграции обнуляется. Не использовать в новом коде.
    /// </summary>
    [Obsolete("Используйте PasswordHash + PasswordSalt. Это поле будет удалено в следующем релизе.")]
    public string? Password { get; set; }

    /// <summary>Дата создания учётной записи</summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>Id роли</summary>
    public int UserRoleId { get; set; }

    /// <summary>Признак мягкого удаления</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Дата создания записи</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Дата последнего изменения</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Дата мягкого удаления</summary>
    public DateTime? DeletedAt { get; set; }

    #endregion

    #region Computed / Not Mapped

    [NotMapped]
    public UserType UserType => UserRoleId switch
    {
        1 => UserType.Admin,
        2 => UserType.Cashier,
        _ => UserType.None
    };

    [NotMapped]
    public bool IsAdmin => UserType == UserType.Admin;

    #endregion

    #region Navigation

    public UserRole? UserRole { get; set; }
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();

    #endregion

    #region Business Methods

    /// <summary>
    /// Установить новый пароль. Хеширует пароль через PBKDF2.
    /// </summary>
    public void SetPassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new DomainException("Пароль не может быть пустым.");

        (PasswordHash, PasswordSalt) = PasswordHasher.Hash(newPassword);

#pragma warning disable CS0618
        Password = null; // очищаем устаревшее поле
#pragma warning restore CS0618

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Проверить пароль. Поддерживает как новый хеш, так и устаревший plain-text (для миграции).
    /// </summary>
    public bool VerifyPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return false;

        // Новый способ — хеш
        if (!string.IsNullOrEmpty(PasswordHash) && !string.IsNullOrEmpty(PasswordSalt))
            return PasswordHasher.Verify(password, PasswordHash, PasswordSalt);

        // Устаревший способ — plain-text (только для миграции старых аккаунтов)
#pragma warning disable CS0618
        if (!string.IsNullOrEmpty(Password))
            return (Password ?? string.Empty) == password;
#pragma warning restore CS0618

        return false;
    }

    /// <summary>Мягкое удаление пользователя</summary>
    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }

    #endregion
}

/// <summary>Тип пользователя</summary>
public enum UserType
{
    None,
    Admin,
    Cashier
}
