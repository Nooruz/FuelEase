namespace KIT.GasStation.Gilbarco.Utilities
{
    public sealed class RequestTransactionData
    {
        public byte? IdBlockTypeRaw { get; init; }
        public int? PositionNumber { get; init; }
        public int? ErrorCode { get; init; }
        public int? ColumnType { get; init; }

        public int? Grade { get; init; }

        public decimal? Price { get; init; }
        public decimal? Volume { get; init; }
        public decimal? Amount { get; init; }
    }
}
