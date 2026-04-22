namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Роль пользователя
/// </summary>
public class UserRole : DomainObject
{
    /// <summary>Наименование роли</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Пользователи с данной ролью</summary>
    public ICollection<User> Users { get; set; } = new List<User>();
}
