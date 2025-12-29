namespace KIT.GasStation.NewCas.Models
{
    public enum PayType
    {
        /// <summary>
        /// наличными
        /// </summary>
        Cash = 0,

        /// <summary>
        /// безналичными
        /// </summary>
        Cashless = 1,

        /// <summary>
        /// предоплата
        /// </summary>
        Prepayment = 2,

        /// <summary>
        /// постоплата
        /// </summary>
        PostPayment = 3
    }
}
