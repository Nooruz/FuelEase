using KIT.GasStation.CashRegisters.Exceptions;
using KIT.GasStation.CashRegisters.Models;
using KIT.GasStation.CashRegisters.Services;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.NewCas.Models;
using Serilog;
using System.Drawing;
using System.Drawing.Printing;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KIT.GasStation.NewCas
{
    public class NewCasCashRegister : ICashRegisterService
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private ILogger _logger;
        private CashRegister _cashRegister;
        private HttpClient _client;
        private NewCasCashRegisterSettings _settings;

        #endregion

        #region Constructors

        public NewCasCashRegister(IHardwareConfigurationService hardwareConfigurationService)
        {
            _hardwareConfigurationService = hardwareConfigurationService;

            // Инициализация логгера
            InitLog();
        }

        #endregion

        /// <inheritdoc/>
        public async Task CloseShiftAsync(string cashierName)
        {
            var state = await GetStateAsync(); // кидает CashRegisterException при ошибке

            if (state == DayStateNewCas.ShiftClosed)
                throw new CashRegisterException("Смена уже закрыта.");

            if (state == DayStateNewCas.ShiftOpened)
            {
                var request = new
                {
                    cashierName = cashierName,
                };

                var response = await SendRequest("/fiscal/shifts/closeDay", request);

                var message = JsonSerializer.Deserialize<OpenAndCloseRecResp>(
                    await response.Content.ReadAsStringAsync()) ?? throw new CashRegisterException("ККМ вернула пустой ответ при закрытии смены.");
                if (message.Status != OpenAndCloseRecRespStatus.Success)
                    throw new CashRegisterException($"Ошибка ККМ при закрытии смены: {message.ErrorMessage}");

                CreateJGP(message.Bitmaps);

                return;
            }

            throw new CashRegisterException("Неизвестное состояние смены ККМ.");
        }

        /// <inheritdoc/>
        public async Task InitializationAsync(Guid cashRegisterId)
        {
            CashRegister? cashRegister = await _hardwareConfigurationService.GetCashRegisterAsync(cashRegisterId);
            
            if (cashRegister == null)
            {
                _logger.Error("Касса не найдена. [{Timestamp}]", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"));
                throw new CashRegisterException("ККМ не найдена. Проверьте настройки ККМ.");
            }

            if (cashRegister.Settings is NewCasCashRegisterSettings settings)
            {
                _settings = settings;
            }

            _cashRegister = cashRegister;
        }

        /// <inheritdoc/>
        public async Task OpenShiftAsync(string cashierName)
        {
            var state = await GetStateAsync(); // сам по себе уже кидает CashRegisterException при ошибке

            if (state == DayStateNewCas.ShiftOpened)
                throw new CashRegisterException("Смена уже открыта.");

            if (state == DayStateNewCas.ShiftClosed)
            {
                var request = new
                {
                    cashierName,
                };

                var response = await SendRequest("/fiscal/shifts/openDay", request);

                var message = JsonSerializer.Deserialize<OpenAndCloseRecResp>(
                    await response.Content.ReadAsStringAsync());

                if (message == null)
                    throw new CashRegisterException("ККМ вернула пустой ответ при открытии смены.");

                if (message.Status != OpenAndCloseRecRespStatus.Success)
                    throw new CashRegisterException($"Ошибка ККМ при открытии смены: {message.ErrorMessage}");

                CreateJGP(message.Bitmaps);

                return;
            }

            throw new CashRegisterException("Неизвестное состояние смены ККМ.");
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> SaleAsync(FiscalData fiscalData, string cashierName)
        {
            // Данные для отправки в запросе
            var openAndCloseRec = new OpenAndCloseRec()
            {
                RecType = RecType.Coming,
                CashierName = cashierName,
                Goods = new[]
                {
                    new Goods
                    {
                        Count    = Math.Round(fiscalData.Total / fiscalData.Price, 6),
                        Price    = fiscalData.Price,
                        ItemName = fiscalData.UnitOfMeasurement,
                        Article  = "",
                        Total    = fiscalData.Total.ToString(),
                        Unit     = "л",
                        VatNum   = fiscalData.ValueAddedTax ? 1 : 0,
                        StNum    = (int)(fiscalData.SalesTax * 100)
                    }
                },
                PayItems = new[]
                {
                    new PayItems
                    {
                        PayType = GetPayType(fiscalData.PaymentType),
                        Total   = fiscalData.Total.ToString()
                    }
                }
            };

            var response = await SendRequest("/fiscal/bills/openAndCloseRec", openAndCloseRec);

            var newfiscalData = await CreateFiscalDataAsync(response);

            return fiscalData.UpdatedFiscalData(newfiscalData);
        }

        /// <inheritdoc/>
        public async Task XReportAsync(bool printReceipt = true)
        {
            var request = new
            {

            };

            var response = await SendRequest("/fiscal/shifts/printXReport", request);

            var state = JsonSerializer.Deserialize<OpenAndCloseRecResp>(
                await response.Content.ReadAsStringAsync());

            if (state == null)
                throw new CashRegisterException("ККМ вернула пустой ответ при печати X-отчета.");

            if (state.Status != OpenAndCloseRecRespStatus.Success)
                throw new CashRegisterException($"Ошибка ККМ при печати X-отчета: {state.ErrorMessage}");

            if (printReceipt)
                CreateJGP(state.Bitmaps);
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> ReturnAsync(FiscalData fiscalData)
        {
            //Данные для отправки в запросе
            var openAndCloseRec = new OpenAndCloseRec()
            {
                RecType = RecType.ReturnComing,
                SourceFDNumber = fiscalData.FiscalDocument,
                SourceFMNumber = fiscalData.FiscalModule,
                Goods = new[]
                {
                    new Goods {
                        Count = Math.Round(fiscalData.Total / fiscalData.Price, 6),
                        Price = fiscalData.Price,
                        ItemName = fiscalData.FuelName,
                        Article = string.IsNullOrEmpty(fiscalData.Tnved) ? "" : fiscalData.Tnved,
                        Total = fiscalData.Total.ToString(),
                        Unit = fiscalData.UnitOfMeasurement,
                        VatNum = fiscalData.ValueAddedTax ? 1 : 0,
                        StNum = (int)(fiscalData.SalesTax * 100) } 
                },
                PayItems = new []
                {
                    new PayItems() 
                    {
                             PayType = GetPayType(fiscalData.PaymentType),
                             Total = fiscalData.Total.ToString()
                    }
                }
            };

            var response = await SendRequest("/fiscal/bills/openAndCloseRec", openAndCloseRec);

            var newFiscalData = await CreateFiscalDataAsync(response);

            return fiscalData.UpdatedFiscalData(newFiscalData);
        }

        /// <inheritdoc/>
        public async Task<CashRegisterState> GetShiftStateAsync()
        {
            var response = await SendRequest("/fiscal/shifts/getState", new { });

            var state = JsonSerializer.Deserialize<GetSateNewCas>(
            await response.Content.ReadAsStringAsync()) ?? throw new CashRegisterException("ККМ вернула неверный формат состояния смены.");

            var shiftState = new CashRegisterState();

            if (state.IsShiftExpired)
            {
                shiftState.Status = CashRegisterStatus.Exceeded24Hours;
            }
            else
            {
                switch (state.State)
                {
                    case DayStateNewCas.ShiftClosed:
                        shiftState.Status = CashRegisterStatus.Close;
                        break;
                    case DayStateNewCas.ShiftOpened:
                        shiftState.Status = CashRegisterStatus.Open;
                        break;
                }
            }

            return shiftState;
        }

        #region Private Helpers

        private async Task<HttpResponseMessage?> SendRequest(string endpoint, object data)
        {
            try
            {
                var response = await GetResponseMessage(endpoint, data);

                if (response == null)
                    throw new CashRegisterException($"ККМ не ответила на запрос {endpoint}");

                if (!response.IsSuccessStatusCode)
                {
                    string body = await response.Content.ReadAsStringAsync();
                    throw new CashRegisterException($"Ошибка ККМ ({response.StatusCode}): {body}");
                }

                return response;
            }
            catch (Exception e)
            {
                LogError(e);
                throw new CashRegisterException($"Ошибка выполнения запроса к ККМ: {e.Message}", e);
            }
        }

        private async Task<HttpResponseMessage?> GetResponseMessage(string api, object message)
        {
            try
            {
                if (_client == null)
                    await GetOnlineCashRegister();

                if (_client == null)
                    throw new CashRegisterException("ККМ недоступна (клиент не создан)");

                var json = JsonSerializer.Serialize(message, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                LogData(json);

                return await _client.PostAsync(api, new StringContent(json, Encoding.UTF8, "application/json"));
            }
            catch (Exception e)
            {
                LogError(e);
                throw new CashRegisterException($"Ошибка связи с ККМ: {e.Message}", e);
            }
        }

        private async Task GetOnlineCashRegister()
        {
            try
            {
                if (_cashRegister == null)
                    throw new CashRegisterException("ККМ не инициализирована.");

                if (!await IsInternetConnectionAvailable())
                    throw new CashRegisterException("ККМ недоступна по сети.");

                _client = new HttpClient
                {
                    BaseAddress = new Uri(_cashRegister.Address)
                };

                _client.DefaultRequestHeaders.Accept.Clear();
                _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
            }
            catch (Exception e)
            {
                LogError(e);
                throw new CashRegisterException($"Ошибка подключения к ККМ: {e.Message}", e);
            }
        }

        private async Task<bool> IsInternetConnectionAvailable()
        {
            try
            {
                using Ping ping = new();
                // Выберите IP-адрес, который вы будете проверять
                string host = GetDomainFromUrl(_cashRegister.Address);

                // Отправьте ICMP-пакет (Ping) на указанный хост
                PingReply reply = await ping.SendPingAsync(host);

                if (reply.Status != IPStatus.Success)
                {
                    //OnError?.Invoke(_cashRegister, "Ошибка: Смарт-карта не подключена или локальный сайт недоступен. Проверьте подключение смарт-карты.");
                }

                // Проверьте статус ответа
                return reply.Status == IPStatus.Success;
            }
            catch (Exception e)
            {
                // Если произошла ошибка, считаем, что интернет недоступен
                //OnError?.Invoke(_cashRegister, "Ошибка: Не удалось подключиться к локальному сайту смарт-карты. Проверьте подключение смарт-карты или попробуйте снова.");
                LogError(e);
                return false;
            }
        }

        private string GetDomainFromUrl(string url)
        {
            try
            {
                Uri uri = new(url);
                return uri.Host;
            }
            catch (UriFormatException e)
            {
                //OnError?.Invoke(_cashRegister, "Не верный адрес ККМ");
                LogError(e);
                return string.Empty; // В случае недопустимого URL
            }
        }

        private void CreateJGP(string[]? base64Strings)
        {
            try
            {
                if (base64Strings == null || base64Strings.Length == 0)
                    return;

                // Обрабатываем каждую строку Base64
                foreach (var base64String in base64Strings)
                {
                    byte[] imageBytes = Convert.FromBase64String(base64String);

                    using var ms = new MemoryStream(imageBytes);
                    using var img = Image.FromStream(ms);

                    PrintImage(img); // синхронно печатает 1 картинку
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        private void PrintImage(Image img)
        {
            using var pd = new PrintDocument();

            pd.PrinterSettings.PrinterName = _settings.DefaultPrinterName;

            // Если имя неправильное — Print() молча НЕ напечатает
            if (!pd.PrinterSettings.IsValid)
                throw new CashRegisterException("Принтер не найден или недоступен.");

            // ВАЖНО: используем поля как "origin", чтобы драйвер не делал свою магию
            pd.OriginAtMargins = true;

            // Небольшие поля — чтобы QR имел “quiet zone” и не резался
            pd.DefaultPageSettings.Margins = new Margins(15, 15, 6, 6);

            pd.PrintPage += (s, e) =>
            {
                // Печатная область, которую реально можно печатать (без не-печатаемых краёв)
                var pa = e.PageSettings.PrintableArea;

                // Точка старта от полей
                float x = e.MarginBounds.Left;
                float y = e.MarginBounds.Top;

                // Ширина по printable area, а не по MarginBounds (иногда MarginBounds врёт)
                float printableWidth = pa.Width - (pd.DefaultPageSettings.Margins.Left + pd.DefaultPageSettings.Margins.Right);

                // Масштаб только по ширине (чек — это “лента”)
                float scale = printableWidth / img.Width;

                // Если драйвер вернул какую-то дичь — страховка
                if (scale <= 0 || float.IsNaN(scale) || float.IsInfinity(scale))
                    scale = e.MarginBounds.Width / (float)img.Width;

                int w = (int)(img.Width * scale);
                int h = (int)(img.Height * scale);

                // Рисуем без сглаживания (для чека полезно)
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

                e.Graphics.DrawImage(img, x, y, w, h);
                e.HasMorePages = false;
            };

            pd.Print();
        }

        private async Task<DayStateNewCas> GetStateAsync()
        {
            try
            {
                var response = await SendRequest("/fiscal/shifts/getState", new { });

                var state = JsonSerializer.Deserialize<GetSateNewCas>(
                await response.Content.ReadAsStringAsync());

                if (state == null)
                    throw new CashRegisterException("ККМ вернула неверный формат состояния смены.");

                return state.State;
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw new CashRegisterException($"Ошибка при получении статуса смены: {ex.Message}", ex);
            }
        }

        private async Task<FiscalData?> CreateFiscalDataAsync(HttpResponseMessage? responseMessage)
        {
            if (responseMessage == null)
                throw new CashRegisterException("ККМ не ответила (responseMessage == null).");

            var body = await responseMessage.Content.ReadAsStringAsync();

            OpenAndCloseRecResp? saleResult;

            try
            {
                saleResult = JsonSerializer.Deserialize<OpenAndCloseRecResp>(body);
            }
            catch (Exception ex)
            {
                LogError(ex);
                throw new CashRegisterException($"ККМ вернула некорректный JSON: {ex.Message}. Body: {body}", ex);
            }

            if (saleResult == null)
                throw new CashRegisterException($"ККМ вернула пустой ответ. Body: {body}");

            // Логируем всегда (и успех, и ошибку)
            LogData(Newtonsoft.Json.JsonConvert.SerializeObject(saleResult, new Newtonsoft.Json.JsonSerializerSettings
            {
                StringEscapeHandling = Newtonsoft.Json.StringEscapeHandling.Default,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
            }));

            // ✅ Успех
            if (saleResult.Status == OpenAndCloseRecRespStatus.Success)
            {
                // Иногда чек успешный, но печать/битмапы упали — это отдельная история
                if (saleResult.Bitmaps != null)
                    CreateJGP(saleResult.Bitmaps);

                return new FiscalData
                {
                    FiscalModule = saleResult.FMNumber,
                    FiscalDocument = saleResult.FDNumber,
                    RegistrationNumber = saleResult.RegistrationNumber,
                    Check = saleResult.QRCode,
                };
            }

            // ❌ Ошибка ККМ
            var msg =
                $"Ошибка ККМ: {saleResult.Status}. " +
                $"Message: {saleResult.ErrorMessage ?? "(нет текста)"}; " +
                $"ExtCode={saleResult.ExtCode}, ExtCode2={saleResult.ExtCode2}";

            // Тут можно делать более “человечные” сообщения по статусу
            throw saleResult.Status switch
            {
                OpenAndCloseRecRespStatus.PrinterError =>
                    new CashRegisterException("Ошибка печати на ККМ. Проверь принтер/бумагу/крышку. " + msg),

                OpenAndCloseRecRespStatus.PrinterBusy =>
                    new CashRegisterException("Принтер ККМ занят. Попробуй повторную печать. " + msg),

                OpenAndCloseRecRespStatus.LicenseHasExpiredOrMissing =>
                    new CashRegisterException("Лицензия фискального ядра истекла или отсутствует. " + msg),

                OpenAndCloseRecRespStatus.InvalidArgumentRequest =>
                    new CashRegisterException("ККМ не приняла параметры запроса (InvalidArgumentRequest). " + msg),

                OpenAndCloseRecRespStatus.FiscalCoreError =>
                    new CashRegisterException("Фискальное ядро вернуло ошибку (FiscalCoreError). " + msg),

                _ =>
                    new CashRegisterException(msg)
            };
        }

        private PayType GetPayType(PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cash => PayType.Cash,
                PaymentType.Cashless => PayType.Cashless,
                _ => PayType.Cash
            };
        }

        #endregion

        #region Logs

        /// <summary>
        /// Инициализация логгера.
        /// </summary>
        private void InitLog()
        {
            // 1. Создадим/убедимся, что существует папка logs
            var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDir);

            // 2. Формируем имя файла. Можно добавить время, 
            //    но обязательно без «:» (двоеточий). Например, yyyy-MM-dd_HH-mm-ss.
            var logFilePath = Path.Combine(logsDir, $"{nameof(NewCasCashRegister)}_{DateTime.Now:dd.MM.yyyy}.log");

            // 3. Настраиваем Serilog
            _logger = new LoggerConfiguration()
                // Указываем минимальный уровень
                .MinimumLevel.Debug()
                // Пишем в файл с «дневным» ротационным интервалом
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day,
                    // Можно задать, сколько файлов хранить
                    retainedFileCountLimit: 7,
                    // Можно включить автопереход на новый файл при достижении лимита размера
                    rollOnFileSizeLimit: true
                )
                // При желании можно добавить вывод в консоль
                //.WriteTo.Console()
                .CreateLogger();

            // 4. Пробный лог на уровне Information
            _logger.Information("=======================NewCas инициализирован=======================");
        }

        private void LogError(Exception e)
        {
            _logger.Information($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss.fff}]\t{e.Message}");
        }

        private void LogData(string data)
        {

            _logger.Information($"[{DateTime.Now:dd.MM.yyyy HH:mm:ss.fff}] - {data}");

        }

        #endregion
    }
}
