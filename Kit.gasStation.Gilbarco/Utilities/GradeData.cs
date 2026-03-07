namespace KIT.GasStation.Gilbarco.Utilities
{
    /// <summary>
    /// Данные по одному сорту топлива (пистолету),
    /// полученные из ответа Gilbarco TWOWIRE на команду 0x5 (Request for Pump Totals).
    /// </summary>
    public sealed class GradeData
    {
        /// <summary>
        /// Номер пистолета (сорта топлива).
        /// Значения начинаются с 1 (E0 → 1, E1 → 2 и т.д.).
        /// </summary>
        public int Nozzle { get; set; }

        /// <summary>
        /// Отпущенный объём топлива.
        /// Формат XXXXXX.XX (дробная часть — два знака).
        /// Значение уже приведено к decimal.
        /// </summary>
        public decimal Counter { get; set; }

        /// <summary>
        /// Сумма денег по пистолету.
        /// Передаётся как целое число (обычно в минимальных денежных единицах).
        /// </summary>
        public long Money { get; set; }

        /// <summary>
        /// Цена уровня 1 (Price Level 1).
        /// Формат XXXX (последние 2 цифры — дробная часть при делении на 100).
        /// </summary>
        public int PriceLevel1 { get; set; }

        /// <summary>
        /// Цена уровня 2 (Price Level 2).
        /// Формат XXXX (последние 2 цифры — дробная часть при делении на 100).
        /// </summary>
        public int PriceLevel2 { get; set; }
    }
}
