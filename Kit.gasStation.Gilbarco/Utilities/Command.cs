namespace KIT.GasStation.Gilbarco.Utilities
{
    public enum Command
    {
        /// <summary>
        /// Запрос статуса (0x0).
        /// </summary>
        Status,

        /// <summary>
        /// Авторизация (0x1).
        /// </summary>
        Authorization,

        /// <summary>
        /// Изменение цены
        /// </summary>
        ChangePrice,

        /// <summary>
        /// Язычок поднят (0x2).
        /// </summary>
        LiftedStatus,

        /// <summary>
        /// Остановка ТРК (0x3).
        /// </summary>
        PumpStop = 0x3,

        /// <summary>
        /// Запрос данных транзакции (0x4).
        /// </summary>
        TransactionDataRequest = 0x4,

        /// <summary>
        /// Запрос суммарных данных ТРК (0x5).
        /// </summary>
        PumpTotalsDataRequest = 0x5,

        /// <summary>
        /// Запрос денег в реальном времени (0x6).
        /// </summary>
        RealTimeMoneyRequest = 0x6,

        /// <summary>
        /// Широковещательная команда остановки всех (0xFC).
        /// </summary>
        AllStop = 0xFC
    }
}
