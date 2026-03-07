namespace KIT.GasStation.Lanfeng.Utilities
{
    /// <summary>
    /// Команда для Ланфенга
    /// </summary>
    public enum Command
    {
        /// <summary>
        /// Запрос состояния
        /// </summary>
        Status,

        /// <summary>
        /// Начать заливку топлива на сумму
        /// </summary>
        StartFuelingSum,

        /// <summary>
        /// Начать заливку топлива на количество
        /// </summary>
        StartFuelingQuantity,

        /// <summary>
        /// Остановить заливку топлива
        /// </summary>
        StopFueling,

        /// <summary>
        /// Завершить заливку
        /// </summary>
        CompleteFueling,

        /// <summary>
        /// Продолжить заливку
        /// </summary>
        ContinueFueling,

        /// <summary>
        /// Изменить цену
        /// </summary>
        ChangePrice,

        /// <summary>
        /// Запрос счетчика литров
        /// </summary>
        CounterLiter,

        /// <summary>
        /// Запрос счетчика суммы
        /// </summary>
        CounterSum,

        /// <summary>
        /// Версия прошивки
        /// </summary>
        FirmwareVersion,

        /// <summary>
        /// Включить (программный контроль)
        /// </summary>
        ProgramControlMode,

        /// <summary>
        /// Выключить (клавиатурный контроль)
        /// </summary>
        KeyboardControlMode,
    }
}
