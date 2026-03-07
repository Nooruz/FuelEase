using KIT.GasStation.ViewModels;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace KIT.GasStation.Services
{
    public sealed class InternetMonitor : IAsyncDisposable
    {
        private readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        private readonly TimeSpan _interval;
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private static readonly string[] _testUrls = new[]
        {
            "https://www.google.com/",
            "https://www.cloudflare.com/",
            "https://www.amazon.com/",
            "https://www.gstatic.com/generate_204" // быстрый ответ 204 без тела
        };

        public InternetStatus Status { get; private set; } = InternetStatus.Checking;
        public event Action<InternetStatus>? StatusChanged;

        public InternetMonitor(TimeSpan? interval = null)
        {
            _interval = interval ?? TimeSpan.FromSeconds(5);
        }

        public void Start()
        {
            if (_loopTask != null) return;
            _cts = new CancellationTokenSource();
            _loopTask = MonitorLoopAsync(_cts.Token);
        }

        public async Task StopAsync()
        {
            if (_cts == null || _loopTask == null) return;
            _cts.Cancel();
            try { await _loopTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            _loopTask = null;
            _cts.Dispose();
            _cts = null;
        }

        private async Task MonitorLoopAsync(CancellationToken ct)
        {
            SetStatus(InternetStatus.Checking);

            while (!ct.IsCancellationRequested)
            {
                var connected = await CheckAnyAsync(ct).ConfigureAwait(false);
                SetStatus(connected ? InternetStatus.Connected : InternetStatus.Disconnected);

                await Task.Delay(_interval, ct).ConfigureAwait(false);
            }
        }

        private async Task<bool> CheckAnyAsync(CancellationToken ct)
        {
            foreach (var url in _testUrls)
            {
                try
                {
                    using var req = new HttpRequestMessage(HttpMethod.Get, url);
                    using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

                    if (resp.IsSuccessStatusCode)
                        return true;
                }
                catch { /* игнор — пробуем следующий */ }
            }
            return false;
        }

        private void SetStatus(InternetStatus newStatus)
        {
            if (Status == newStatus) return;
            Status = newStatus;
            StatusChanged?.Invoke(newStatus);
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync().ConfigureAwait(false);
            _http.Dispose();
        }
    }
}
