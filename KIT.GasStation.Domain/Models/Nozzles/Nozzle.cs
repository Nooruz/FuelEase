using KIT.GasStation.Domain.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KIT.GasStation.Domain.Models;

/// <summary>
/// ТРК (топливораздаточная колонка / пистолет)
/// </summary>
[Display(Name = "ТРК")]
public class Nozzle : DomainObject
{
    #region Constructors

    public Nozzle() { }

    /// <summary>Копирующий конструктор (только конфигурация)</summary>
    public Nozzle(Nozzle source)
    {
        Id = source.Id;
        Name = source.Name;
        Tube = source.Tube;
        Side = source.Side;
        TankId = source.TankId;
        Group = source.Group;
        IsDeleted = source.IsDeleted;
        CreatedAt = source.CreatedAt;
        UpdatedAt = source.UpdatedAt;
        DeletedAt = source.DeletedAt;
    }

    #endregion

    // ── Персистируемая конфигурация ──────────────────────────────────────────

    /// <summary>Наименование (например «Пистолет 1А»)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Номер шланга</summary>
    public int Tube { get; set; }

    /// <summary>Сторона колонки</summary>
    public int Side { get; set; }

    /// <summary>Id резервуара</summary>
    public int TankId { get; set; }

    /// <summary>Группа (Hub/контроллер)</summary>
    public string Group { get; set; } = string.Empty;

    /// <summary>Признак мягкого удаления</summary>
    public bool IsDeleted { get; set; }

    /// <summary>Дата создания записи</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Дата последнего изменения</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Дата мягкого удаления</summary>
    public DateTime? DeletedAt { get; set; }

    // ── Навигация ────────────────────────────────────────────────────────────

    public Tank? Tank { get; set; }
    public ICollection<ShiftCounter> NozzleShiftCounters { get; set; } = new List<ShiftCounter>();

    // ── Рантайм-состояние (не персистируется) ───────────────────────────────
    // Эти поля обновляются в реальном времени из потока Worker'а.
    // TODO: вынести в NozzleRuntimeState (Application layer) и связать с UI через ViewModel.

    /// <summary>Текущий статус ТРК от контроллера</summary>
    [NotMapped] public NozzleStatus Status { get; set; }

    /// <summary>Программное управление (true) или клавиатурное (false)</summary>
    [NotMapped] public bool IsProgramControl { get; set; }

    /// <summary>Последнее показание счётчика (литры)</summary>
    [NotMapped] public decimal LastCounter { get; set; }

    /// <summary>Накопленная выручка за смену (для отображения)</summary>
    [NotMapped] public decimal SalesSum { get; set; }

    /// <summary>Текущая цена из резервуара</summary>
    [NotMapped] public decimal Price => Tank?.Fuel?.Price ?? 0;

    /// <summary>Пистолет поднят (заправка активна)</summary>
    [NotMapped] public bool Lifted { get; set; }

    /// <summary>Активная незавершённая продажа на данном пистолете</summary>
    [NotMapped] public FuelSale? FuelSale { get; set; }

    /// <summary>Порядковый номер пистолета в UI</summary>
    [NotMapped] public int Number { get; set; }

    /// <summary>Последнее сообщение от Worker-сервиса</summary>
    [NotMapped] public string? WorkerStateMessage { get; set; }

    /// <summary>Время последнего обновления от Worker-сервиса</summary>
    [NotMapped] public DateTimeOffset WorkerStateUpdatedAt { get; set; }

    // ── Бизнес-методы ───────────────────────────────────────────────────────

    /// <summary>Мягкое удаление ТРК</summary>
    public void Delete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
    }
}

public enum NozzleStatus
{
    [Display(Name = "Неизвестно")]
    Unknown = 0,

    [Display(Name = "Готов")]
    Ready = 1,

    [Display(Name = "Насос работает")]
    PumpWorking = 2,

    [Display(Name = "Ожидание остановки")]
    WaitingStop = 3,

    [Display(Name = "Насос остановлен")]
    PumpStop = 4,

    [Display(Name = "Ожидание снятия пистолета")]
    WaitingRemoved = 5,

    [Display(Name = "Блокировка")]
    Blocking = 6
}

public enum NozzleControlMode
{
    [Display(Name = "Неизвестно")]
    Unknown = 0,

    [Display(Name = "Программное управление")]
    Program = 1,

    [Display(Name = "Клавиатура")]
    Keyboard = 2
}
