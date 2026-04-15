namespace KIT.GasStation.CashRegisters.Models
{
    /// <summary>
    /// Отчёт по продажам за текущую смену ККМ.
    /// Не хранится в БД — используется только для отображения в отчётах.
    /// </summary>
    public class ShiftSalesReport
    {
        #region Приход (продажи)

        public int SaleReceiptCount { get; set; }
        public decimal CashSaleSum { get; set; }
        public decimal CashlessSaleSum { get; set; }

        #endregion

        #region Возврат прихода

        public int ReturnReceiptCount { get; set; }
        public decimal CashReturnSum { get; set; }
        public decimal CashlessReturnSum { get; set; }

        #endregion

        #region Переопределения для нестандартных ККМ

        public decimal? TotalSaleSumOverride { get; set; }
        public decimal? TotalReturnSumOverride { get; set; }
        public decimal? NetSumOverride { get; set; }

        #endregion

        #region Вычисляемые свойства

        public decimal TotalSaleSum => TotalSaleSumOverride ?? (CashSaleSum + CashlessSaleSum);

        public decimal TotalReturnSum => TotalReturnSumOverride ?? (CashReturnSum + CashlessReturnSum);

        public decimal NetSum => NetSumOverride ?? (TotalSaleSum - TotalReturnSum);

        #endregion
    }
}
