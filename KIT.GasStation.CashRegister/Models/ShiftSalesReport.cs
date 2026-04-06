namespace KIT.GasStation.CashRegisters.Models
{
    /// <summary>
    /// Отчёт по продажам за текущую смену ККМ.
    /// Не хранится в БД — используется только для отображения в отчётах.
    /// </summary>
    public class ShiftSalesReport
    {
        #region Приход (продажи)

        /// <summary>Количество чеков прихода за смену.</summary>
        public int SaleReceiptCount { get; set; }

        /// <summary>Сумма продаж наличными (в сомах).</summary>
        public decimal CashSaleSum { get; set; }

        /// <summary>Сумма продаж безналичными (в сомах).</summary>
        public decimal CashlessSaleSum { get; set; }

        #endregion

        #region Возврат прихода

        /// <summary>Количество чеков возврата прихода за смену.</summary>
        public int ReturnReceiptCount { get; set; }

        /// <summary>Сумма возвратов наличными (в сомах).</summary>
        public decimal CashReturnSum { get; set; }

        /// <summary>Сумма возвратов безналичными (в сомах).</summary>
        public decimal CashlessReturnSum { get; set; }

        #endregion

        #region Вычисляемые свойства

        /// <summary>Итого продажи (нал + безнал).</summary>
        public decimal TotalSaleSum => CashSaleSum + CashlessSaleSum;

        /// <summary>Итого возвраты (нал + безнал).</summary>
        public decimal TotalReturnSum => CashReturnSum + CashlessReturnSum;

        /// <summary>Итого чистая выручка (продажи − возвраты).</summary>
        public decimal NetSum => TotalSaleSum - TotalReturnSum;

        #endregion
    }
}
