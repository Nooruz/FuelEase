namespace KIT.GasStation.Gilbarco.Utilities
{
    /// <summary>
    /// Коды команд протокола Gilbarco TWOTP (старший ниббл байта команды).
    /// </summary>
    public enum Command : byte
    {
        /// <summary>
        /// Запрос статуса (0x0).
        /// </summary>
        Status = 0x0,

        /// <summary>
        /// Авторизация (0x1).
        /// </summary>
        Authorization = 0x1,

        /// <summary>
        /// Перевод ТРК в состояние приема/передачи блока данных (Data Next)
        /// </summary>
        /// <remarks>Используется для смены цен, установки пресетов и специальных функций</remarks>
        DataNext = 0x2,

        /// <summary>
        /// Команда принудительной остановки налива или деавторизации (Pump Stop)
        /// </summary>
        PumpStop = 0x3,

        /// <summary>Запрос данных завершенной или текущей транзакции (Transaction Data Request)</summary>
        TransactionDataRequest = 0x4,

        /// <summary>Запрос накопительных счетчиков объема и денег по сортам (Pump Totals Data Request)</summary>
        PumpTotalsDataRequest = 0x5,

        /// <summary>Запрос текущей суммы налива в реальном времени (Real-Time Money Request)</summary>
        RealTimeMoneyRequest = 0x6,

        /// <summary>Широковещательная команда остановки всех колонок в линии (All Stop)</summary>
        AllStop = 0xFC
    }
}
