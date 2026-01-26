namespace KIT.GasStation.Gilbarco.Utilities
{
    public enum Status
    {
        Unknown,

        /// <summary>
        /// Ошибка данных (0x0).
        /// </summary>
        DataError,

        /// <summary>
        /// Выключено (0x6).
        /// </summary>
        Off = 0x6,

        /// <summary>
        /// Вызов (0x7).
        /// </summary>
        Call = 0x7,

        /// <summary>
        /// Авторизовано/без отпуска (0x8).
        /// </summary>
        AuthorizedNotDelivering = 0x8,

        /// <summary>
        /// Занято (0x9).
        /// </summary>
        Busy = 0x9,

        /// <summary>
        /// Транзакция завершена (PEOT) (0xA).
        /// </summary>
        TransactionCompletePeot = 0xA,

        /// <summary>
        /// Транзакция завершена (FEOT) (0xB).
        /// </summary>
        TransactionCompleteFeot = 0xB,

        /// <summary>
        /// Остановка ТРК (0xC).
        /// </summary>
        PumpStop = 0xC,

        /// <summary>
        /// Отправить данные (0xD).
        /// Возвращается только на команду Data Next.
        /// </summary>
        SendData = 0xD
    }
}
