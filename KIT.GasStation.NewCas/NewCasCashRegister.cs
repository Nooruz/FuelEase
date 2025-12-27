using KIT.GasStation.CashRegisters.Exceptions;
using KIT.GasStation.CashRegisters.Services;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Serilog;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Drawing;
using System.Drawing.Printing;

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

        #region Actions

        public event Action OnShiftOpened;
        public event Action OnShiftClosed;
        public event Action OnReceiptPrinting;
        public event Action<FuelSale> OnReturning;
        public event Action<string> OnUnknownError;
        public event Action<CashRegisterStatus> OnStatusChanged;

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
                    printToBitmaps = true
                };

                var response = await SendRequest("/fiscal/shifts/closeDay", request);

                var message = JsonSerializer.Deserialize<OpenAndCloseRecResp>(
                    await response.Content.ReadAsStringAsync());

                if (message == null)
                    throw new CashRegisterException("ККМ вернула пустой ответ при закрытии смены.");

                if (message.Status != OpenAndCloseRecRespStatus.Success)
                    throw new CashRegisterException($"Ошибка ККМ при закрытии смены: {message.ErrorMessage}");

                CreateJGP(message.Bitmaps);

                OnShiftClosed?.Invoke();
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
                    cashierName = cashierName,
                    printToBitmaps = true
                };

                var response = await SendRequest("/fiscal/shifts/openDay", request);

                var message = JsonSerializer.Deserialize<OpenAndCloseRecResp>(
                    await response.Content.ReadAsStringAsync());

                if (message == null)
                    throw new CashRegisterException("ККМ вернула пустой ответ при открытии смены.");

                if (message.Status != OpenAndCloseRecRespStatus.Success)
                    throw new CashRegisterException($"Ошибка ККМ при открытии смены: {message.ErrorMessage}");

                CreateJGP(message.Bitmaps);

                OnShiftOpened?.Invoke();
                return;
            }

            throw new CashRegisterException("Неизвестное состояние смены ККМ.");
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> SaleAsync(FuelSale fuelSale, Fuel fuel, string cashierName, bool isBefore = true)
        {
            decimal sum = isBefore ? fuelSale.Sum : fuelSale.ReceivedSum;

            // Данные для отправки в запросе
            var openAndCloseRec = new OpenAndCloseRec()
            {
                RecType = RecType.Coming,
                CashierName = cashierName,
                Goods = new[]
                {
                    new Goods
                    {
                        Count    = Math.Round(sum / fuelSale.Price, 6),
                        Price    = fuelSale.Price,
                        ItemName = fuel.Name,
                        Article  = "",
                        Total    = sum.ToString(),
                        Unit     = "л",
                        VatNum   = fuel.ValueAddedTax ? 1 : 0,
                        StNum    = (int)(fuel.SalesTax * 100)
                    }
                },
                PayItems = new[]
                {
                    new PayItems
                    {
                        PayType = GetPayType(fuelSale.PaymentType),
                        Total   = sum.ToString()
                    }
                }
            };

            var response = await SendRequest("/fiscal/bills/openAndCloseRec", openAndCloseRec);

            var fiscalData = await CreateFiscalDataAsync(response);

            return fiscalData!;
        }

        /// <inheritdoc/>
        public async Task XReportAsync(bool printReceipt = true)
        {
            var request = new
            {
                printToBitmaps = true,
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
        public async Task<FiscalData?> ReturnAsync(FuelSale fuelSale, Fuel fuel)
        {
            if (fuelSale.FiscalData == null)
                throw new CashRegisterException("У продажи отсутствуют фискальные данные для возврата.");


            //Данные для отправки в запросе
            var openAndCloseRec = new OpenAndCloseRec()
            {
                RecType = RecType.ReturnComing,
                PrintToBitmaps = true,
                SourceFDNumber = fuelSale.FiscalData.FiscalDocument,
                SourceFMNumber = fuelSale.FiscalData.FiscalModule,
                Goods = new[]
                {
                    new Goods {
                        Count = Math.Round(fuelSale.Sum / fuelSale.Price, 6),
                        Price = fuelSale.Price,
                        ItemName = fuel.Name,
                        Article = "",
                        Total = fuelSale.Sum.ToString(),
                        Unit = fuel.UnitOfMeasurement.Name,
                        VatNum = fuel.ValueAddedTax ? 1 : 0,
                        StNum = (int)(fuel.SalesTax * 100) } 
                },
                PayItems = new []
                {
                    new PayItems() 
                    {
                             PayType = GetPayType(fuelSale.PaymentType),
                             Total = fuelSale.Sum.ToString()
                    }
                }
            };

            var response = await SendRequest("/fiscal/bills/openAndCloseRec", openAndCloseRec);

            var fiscalData = await CreateFiscalDataAsync(response);

            return fiscalData!;
        }

        /// <inheritdoc/>
        public async Task<string?> GetShiftStateAsync()
        {
            var state = await GetStateAsync(); // при ошибке уже бросит CashRegisterException

            return state switch
            {
                DayStateNewCas.ShiftOpened => "Открыта",
                DayStateNewCas.ShiftClosed => "Закрыта",
                _ => "Неизвестно"
            };
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> ReturnAndReceivedSaleAsync(FuelSale fuelSale, Fuel fuel, string cashierName)
        {
            // 1) Возврат старой продажи
            var returnFiscalData = await ReturnAsync(fuelSale, fuel);

            if (fuelSale.ReceivedQuantity <= 0)
                return returnFiscalData; // ничего нового продавать не нужно

            // 2) Новая продажа на реально полученное количество
            var openAndCloseRec = new OpenAndCloseRec()
            {
                RecType = RecType.Coming,
                CashierName = cashierName,
                PrintToBitmaps = true,
                Goods = new[]
                {
                        new Goods()
                        {
                            Count = Math.Round(fuelSale.ReceivedSum / fuelSale.Price, 6),
                            Price = fuelSale.Price,
                            ItemName = fuelSale.Tank.Fuel.Name,
                            Article = "",
                            Total = fuelSale.ReceivedSum.ToString(),
                            Unit = fuelSale.Tank.Fuel.UnitOfMeasurement.Name,
                            VatNum = fuelSale.Tank.Fuel.ValueAddedTax ? 1 : 0,
                            StNum = (int)(fuelSale.Tank.Fuel.SalesTax * 100)
                        }
                    },
                PayItems = new[]
                {
                        new PayItems()
                        {
                            PayType = GetPayType(fuelSale.PaymentType),
                            Total = fuelSale.ReceivedSum.ToString()
                        }
                    }
            };

            var saleResponse = await SendRequest("/fiscal/bills/openAndCloseRec", openAndCloseRec);

            var newFiscalData = await CreateFiscalDataAsync(saleResponse);

            return returnFiscalData;
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

    public class OpenAndCloseRec
    {
        [JsonPropertyName("recType")]
        public RecType RecType { get; set; }

        [JsonPropertyName("cashierName")]
        public string CashierName { get; set; }

        [JsonPropertyName("goods")]
        public Goods[] Goods { get; set; }

        [JsonPropertyName("payItems")]
        public PayItems[] PayItems { get; set; }

        [JsonPropertyName("printToBitmaps")]
        public bool PrintToBitmaps { get; set; }

        [JsonPropertyName("sourceFMNumber")]
        public string? SourceFMNumber { get; set; }

        [JsonPropertyName("sourceFDNumber")]
        public int? SourceFDNumber { get; set; }

        [JsonPropertyName("discounts")]
        public Discounts[] Discounts { get; set; }
    }

    /// <summary>
    /// перечисление 
    /// </summary>
    public enum RecType
    {
        /// <summary>
        /// Приход
        /// </summary>
        Coming = 1,

        /// <summary>
        /// Возврат прихода
        /// </summary>
        ReturnComing = 2,

        /// <summary>
        /// Расход
        /// </summary>
        Expenditure = 3,

        /// <summary>
        /// Возврат расхода
        /// </summary>
        ReturnExpenditure = 4
    }

    /// <summary>
    /// Товары или услуги
    /// </summary>
    public class Goods
    {
        /// <summary>
        /// количество
        /// </summary>
        [JsonPropertyName("count")]
        public decimal Count { get; set; }

        /// <summary>
        /// Цена
        /// </summary>
        [JsonPropertyName("price")]
        public decimal Price { get; set; }

        /// <summary>
        /// Наименование товара или услуги
        /// </summary>
        [JsonPropertyName("itemName")]
        public string ItemName { get; set; }

        /// <summary>
        /// Артикул
        /// </summary>
        [JsonPropertyName("article")]
        public string Article { get; set; }

        /// <summary>
        /// Сумма
        /// </summary>
        [JsonPropertyName("total")]
        public string Total { get; set; }

        /// <summary>
        /// Единица измерения
        /// </summary>
        [JsonPropertyName("unit")]
        public string Unit { get; set; }

        /// <summary>
        /// код ставки НДС
        /// </summary>
        [JsonPropertyName("vatNum")]
        public int VatNum { get; set; }

        /// <summary>
        /// код ставки НСП
        /// </summary>
        [JsonPropertyName("stNum")]
        public int StNum { get; set; }
    }

    public class Discounts
    {
        [JsonPropertyName("amount")]
        public string? Amount { get; set; }
    }

    public class PayItems
    {
        [JsonPropertyName("payType")]
        public PayType PayType { get; set; }

        [JsonPropertyName("total")]
        public string Total { get; set; }
    }

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

    public class OpenAndCloseRecResp
    {
        [JsonPropertyName("status")]
        public OpenAndCloseRecRespStatus Status { get; set; }

        [JsonPropertyName("extCode")]
        public int ExtCode { get; set; }

        [JsonPropertyName("extCode2")]
        public int ExtCode2 { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("qrCode")]
        public string? QRCode { get; set; }

        [JsonPropertyName("tin")]
        public string? TIN { get; set; }

        [JsonPropertyName("registrationNumber")]
        public string? RegistrationNumber { get; set; }

        [JsonPropertyName("fmNumber")]
        public string? FMNumber { get; set; }

        [JsonPropertyName("shiftNumber")]
        public int ShiftNumber { get; set; }

        [JsonPropertyName("fdNumber")]
        public int FDNumber { get; set; }

        [JsonPropertyName("dateTime")]
        public string? DateTime { get; set; }

        [JsonPropertyName("bitmaps")]
        public string[]? Bitmaps { get; set; }
    }

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

    public class GetSateNewCas
    {
        /// <summary>
        /// Статус смены, открыта или закрыта
        /// </summary>
        [JsonPropertyName("dayState")]
        public DayStateNewCas State { get; set; }

        /// <summary>
        /// 24 часа закончились или нет
        /// </summary>
        [JsonPropertyName("isShiftExpired")]
        public bool IsShiftExpired { get; set; }

        /// <summary>
        /// номер последней открытой смены
        /// </summary>
        [JsonPropertyName("shiftNumber")]
        public int ShiftNumber { get; set; }

        /// <summary>
        /// последний номер ФД
        /// </summary>
        [JsonPropertyName("documentNumber")]
        public int DocumentNumber { get; set; }

        /// <summary>
        /// дата/время ККМ
        /// </summary>
        [JsonPropertyName("dateTime")]
        public string? DateTime { get; set; }

        /// <summary>
        /// последний номер ФД типа чек
        /// </summary>
        [JsonPropertyName("billNumber")]
        public int BillNumber { get; set; }

        /// <summary>
        /// сумма чеков продаж
        /// </summary>
        [JsonPropertyName("saleSum")]
        public double SaleSum { get; set; }

        /// <summary>
        /// сумма наличных в кассе
        /// </summary>
        [JsonPropertyName("cashSum")]
        public double CashSum { get; set; }

        /// <summary>
        /// сумма безналичных в кассе
        /// </summary>
        [JsonPropertyName("cashlessSum")]
        public double CashlessSum { get; set; }

        /// <summary>
        /// количество чеков продажи
        /// </summary>
        [JsonPropertyName("saleNumber")]
        public int SaleNumber { get; set; }

        /// <summary>
        /// Регистрационный номер ККМ
        /// </summary>
        [JsonPropertyName("registrationNumber")]
        public string? RegistrationNumber { get; set; }

        /// <summary>
        /// номер ФМ
        /// </summary>
        [JsonPropertyName("fmNumber")]
        public string? FmNumber { get; set; }

        /// <summary>
        /// серийный номер ККМ
        /// </summary>
        [JsonPropertyName("serialNumber")]
        public string? SerialNumber { get; set; }
    }

    public enum DayStateNewCas
    {
        /// <summary>
        /// Смена закрыта
        /// </summary>
        [Display(Name = "Закрыта")]
        ShiftClosed = 0,

        /// <summary>
        /// Смена открыта
        /// </summary>
        [Display(Name = "Открыта")]
        ShiftOpened = 1,

        None = 2
    }
}
