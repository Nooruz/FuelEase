namespace KIT.GasStation.FuelDispenser.Commands
{
    public enum Command
    {
        /// <summary>
        /// Запрос состояния
        /// </summary>
        Status,

        /// <summary>
        /// Начать заливку топлива на сумму
        /// </summary>
        StartFillingSum,

        /// <summary>
        /// Начать заливку топлива на количество
        /// </summary>
        StartFillingQuantity,

        /// <summary>
        /// Остановить заливку топлива
        /// </summary>
        StopFilling,

        /// <summary>
        /// Завершить заливку
        /// </summary>
        CompleteFilling,

        /// <summary>
        /// Продолжить заливку
        /// </summary>
        ContinueFilling,

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

        /// <summary>
        /// Датчик пистолета ПК Электроникс
        /// </summary>
        Sensor,

        /// <summary>
        /// Настройка предклапана (снижение расхода)
        /// </summary>
        ReduceCosts,

        /// <summary>
        /// Время разгона насоса
        /// </summary>
        PumpAccelerationTime,

        /// <summary>
        /// Данные дисплея
        /// </summary>
        Screen
    }
}
