namespace FuelEase.HardwareConfigurations.Models
{
    /// <summary>
    /// Перечисление команд для управления топливораздаточной колонкой.
    /// </summary>
    public enum NozzleCommand
    {
        None,

        /// <summary>
        /// Запрос текущего статуса.
        /// </summary>
        Status,

        /// <summary>
        /// Начать заливку на сумму.
        /// </summary>
        StartFillingSum,

        /// <summary>
        /// Начать заливку на объем.
        /// </summary>
        StartFillingQuantity,

        /// <summary>
        /// Остановить текущую заливку.
        /// </summary>
        StopFilling,

        /// <summary>
        /// Завершить текущую заливку.
        /// </summary>
        CompleteFilling,

        /// <summary>
        /// Продолжить приостановленную заливку.
        /// </summary>
        ContinueFilling,

        /// <summary>
        /// Изменить текущую цену топлива.
        /// </summary>
        ChangePrice,

        /// <summary>
        /// Запросить значение счетчика литров.
        /// </summary>
        RequestCounterLiter,

        /// <summary>
        /// Запросить значение счетчика суммы.
        /// </summary>
        RequestCounterSum,

        /// <summary>
        /// Запрос версии прошивки устройства.
        /// </summary>
        FirmwareVersion,

        /// <summary>
        /// Запрос состояния датчика пистолета.
        /// </summary>
        Sensor,

        /// <summary>
        /// Изменение постоянного расхода.
        /// </summary>
        ConstantFlowReduction,

        /// <summary>
        /// Задать время разгона насоса.
        /// </summary>
        PumpAccelerationTime,

        /// <summary>
        /// Получить значения с табло.
        /// </summary>
        Screen,

        /// <summary>
        /// Переключить управление на клавиатуру.
        /// </summary>
        KeyboardControlMode,

        /// <summary>
        /// Переключить управление на программу.
        /// </summary>
        ProgramControlMode,

        /// <summary>
        /// Установить параметры ГРК (Технопроект).
        /// </summary>
        SetSetup,

        /// <summary>
        /// Сбросить параметры ГРК (Технопроект).
        /// </summary>
        Reset,

        /// <summary>
        /// Получить параметры ГРК (Технопроект).
        /// </summary>
        GetSetup,

        /// <summary>
        /// Запустить процесс ГРК (Технопроект).
        /// </summary>
        Start
    }
}
