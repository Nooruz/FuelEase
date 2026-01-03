namespace KIT.GasStation.EKassa
{
    public sealed class EkassaOptions
    {
        /// <summary>Базовый URL eKassa, например: https://api.ekassa.tld</summary>
        public Uri BaseUri { get; set; } = default!;

        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;

        /// <summary>Таймаут HTTP-запроса.</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Сколько раз повторять запрос при 401 после обновления токена.</summary>
        public int MaxAuthRetry { get; set; } = 1;
    }
}
