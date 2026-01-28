namespace KIT.GasStation.Gilbarco.Utilities
{
    /// <summary>
    /// Статусы ответа ТРК (старший ниббл байта ответа) [3].
    /// </summary>
    public enum Status : byte
    {
        /// <summary>
        /// Ошибка в формате принятых данных или контрольной сумме (Data Error)
        /// </summary>
        DataError = 0x0,

        /// <summary>
        /// ТРК в режиме ожидания: пистолет на месте, авторизации нет (Off)
        /// </summary>
        Off = 0x6,

        /// <summary>
        /// Пистолет снят: ТРК ожидает авторизации от консоли (Call)
        /// </summary>
        Call = 0x7,

        /// <summary>
        /// ТРК авторизована, но подача топлива еще не началась (Authorized/Not Delivering)
        /// </summary>
        AuthorizedNotDelivering = 0x8,

        /// <summary>
        /// Идет активный процесс налива топлива (Busy)
        /// </summary>
        Busy = 0x9,

        /// <summary>
        /// Налив завершен, пистолет повешен: ожидание обработки транзакции (PEOT)
        /// </summary>
        TransactionCompletePeot = 0xA,

        /// <summary>
        /// Налив завершен, пистолет повешен: транзакция зафиксирована (FEOT)
        /// </summary>
        TransactionCompleteFeot = 0xB,

        /// <summary>
        /// ТРК находится в состоянии блокировки после команды Stop (Pump Stop)
        /// </summary>
        PumpStop = 0xC,

        /// <summary>
        /// Подтверждение готовности ТРК к приему расширенного блока данных (Send Data)
        /// </summary>
        /// <remarks>Возвращается только в ответ на команду Data Next</remarks>
        SendData = 0xD
    }
}
