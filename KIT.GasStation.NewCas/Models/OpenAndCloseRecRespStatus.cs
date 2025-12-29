namespace KIT.GasStation.NewCas.Models
{
    public enum OpenAndCloseRecRespStatus
    {
        /// <summary>
        /// запрос отработал без ошибок
        /// </summary>
        Success = 0,

        /// <summary>
        /// неизвестная команда
        /// </summary>
        UnknownCommand = 1,

        /// <summary>
        /// ошибка парсинга JSON
        /// </summary>
        JSONParsingError = 2,

        /// <summary>
        /// ошибка сериализации JSON
        /// </summary>
        JSONSerializationError = 3,

        /// <summary>
        /// ошибка бинарной сериализации
        /// </summary>
        BinarySerializationError = 4,

        /// <summary>
        /// внутренняя ошибка сервиса
        /// </summary>
        InternalErrorService = 5,

        /// <summary>
        /// ошибка фискального ядра, в полях extCode и extCode2 находятся дополнительные коды ошибок
        /// </summary>
        FiscalCoreError = 6,

        /// <summary>
        /// некорректный аргумент в запросе
        /// </summary>
        InvalidArgumentRequest = 7,

        /// <summary>
        /// лицензия ядра истекла или отсутствует
        /// </summary>
        LicenseHasExpiredOrMissing = 8,

        /// <summary>
        ///  ошибка принтера
        /// </summary>
        PrinterError = 9,

        /// <summary>
        /// принтер занят 
        /// Коды ошибок 9 и 10 появляются только после обработки запроса, то есть все операции уже закончены, например, фискальный документ отправлен на сервер, но печать произвести не удалось. В таких случаях нужно вызывать методы повторной печати.
        /// </summary>
        PrinterBusy = 10,


    }
}
