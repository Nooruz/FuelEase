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

namespace KIT.GasStation.NewCas
{
    public class NewCasCashRegister : ICashRegisterService
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private ILogger _logger;
        private CashRegister _cashRegister;
        private HttpClient _client;

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
            DayStateNewCas state = await GetStateAsync();

            if (state == DayStateNewCas.ShiftOpened)
            {
                var response = new
                {
                    cashierName = cashierName,
                    printToBitmaps = true
                };

                HttpResponseMessage? closeShift = await SendRequest("/fiscal/shifts/closeDay", response);

                if (closeShift != null && closeShift.IsSuccessStatusCode)
                {
                    OpenAndCloseRecResp? message = JsonSerializer.Deserialize<OpenAndCloseRecResp>(await closeShift.Content.ReadAsStringAsync());
                    if (message != null)
                    {
                        await CreateJGP(message.Bitmaps);
                        return;
                    }
                }
            }

            if (state == DayStateNewCas.ShiftClosed)
            {
                throw new CashRegisterException("Смена уже закрыта.");
            }
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

            _cashRegister = cashRegister;
        }

        /// <inheritdoc/>
        public async Task OpenShiftAsync(string cashierName)
        {
            DayStateNewCas state = await GetStateAsync();

            if (state == DayStateNewCas.ShiftClosed)
            {
                var response = new
                {
                    cashierName = cashierName,
                    printToBitmaps = true
                };

                HttpResponseMessage? openShift = await SendRequest("/fiscal/shifts/openDay", response);

                if (openShift != null && openShift.IsSuccessStatusCode)
                {
                    throw new CashRegisterException("Смена успешно открыта.");
                }
            }

            if (state == DayStateNewCas.ShiftOpened)
            {
                throw new CashRegisterException("Смена уже открыта.");
            }
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> SaleAsync(FuelSale fuelSale, Fuel fuel, string cashierName)
        {
            // Данные для отправки в запросе
            OpenAndCloseRec openAndCloseRec = new()
            {
                RecType = RecType.Coming,
                CashierName = cashierName,
                PrintToBitmaps = true,
                Goods = new[]
                {
                    new Goods
                    {
                        Count    = Math.Round(fuelSale.Sum / fuelSale.Price, 6),
                        Price    = fuelSale.Price,
                        ItemName = fuel.Name,
                        Article  = "",
                        Total    = fuelSale.Sum.ToString(),
                        Unit     = fuel.UnitOfMeasurement.Name,
                        VatNum   = fuel.ValueAddedTax ? 1 : 0,
                        StNum    = (int)(fuel.SalesTax * 100)
                    }
                },
                PayItems = new[]
                {
                    new PayItems
                    {
                        PayType = GetPayType(fuelSale.PaymentType),
                        Total   = fuelSale.Sum.ToString()
                    }
                }
            };

            HttpResponseMessage? sale = await SendRequest("/fiscal/bills/openAndCloseRec", openAndCloseRec);

            return await CreateFiscalDataAsync(sale);
        }

        /// <inheritdoc/>
        public async Task XReportAsync(bool printReceipt = true)
        {
            var response = new
            {
                printToBitmaps = true,
            };

            var stateMessage = await SendRequest("/fiscal/shifts/printXReport", response);

            if (stateMessage != null && stateMessage.IsSuccessStatusCode)
            {
                OpenAndCloseRecResp state = JsonSerializer.Deserialize<OpenAndCloseRecResp>(await stateMessage.Content.ReadAsStringAsync());
                if (state != null)
                {
                    await CreateJGP(state.Bitmaps);
                }
            }
        }

        /// <inheritdoc/>
        public async Task ReturnAsync(FuelSale fuelSale, Fuel fuel)
        {
            //Данные для отправки в запросе
            OpenAndCloseRec openAndCloseRec = new()
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
                        ItemName = fuelSale.Tank.Fuel.Name,
                        Article = "",
                        Total = fuelSale.Sum.ToString(),
                        Unit = fuelSale.Tank.Fuel.UnitOfMeasurement.Name,
                        VatNum = fuelSale.Tank.Fuel.ValueAddedTax ? 1 : 0,
                        StNum = (int)(fuelSale.Tank.Fuel.SalesTax * 100) } 
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

            HttpResponseMessage? sale = await SendRequest("/fiscal/bills/openAndCloseRec", openAndCloseRec);

            if (sale != null && sale.IsSuccessStatusCode)
            {
                //await UpdateFuelSaleWhenReturn(fuelSale, sale);
            }

        }

        /// <inheritdoc/>
        public async Task<string?> GetShiftStateAsync()
        {
            return null;
        }

        /// <inheritdoc/>
        public async Task ReturnAndReceivedSaleAsync(FuelSale fuelSale, Fuel fuel)
        {

        }

        #region Private Helpers

        private async Task<HttpResponseMessage?> SendRequest(string endpoint, object data)
        {
            try
            {
                HttpResponseMessage? response = await GetResponseMessage(endpoint, data);
                if (response != null)
                {
                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }
                    else
                    {
                        var responseContent = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());
                        //HandleError(responseContent);
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }

            return default; // Возвращайте значение по умолчанию в случае ошибки
        }

        private async Task<HttpResponseMessage?> GetResponseMessage(string api, object message)
        {
            try
            {
                if (_client == null)
                {
                    await GetOnlineCashRegister();
                }

                if (_client != null)
                {
                    JsonSerializerOptions options = new()
                    {
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, // Игнорировать свойства, равные null
                        WriteIndented = false
                    };

                    LogData(Newtonsoft.Json.JsonConvert.SerializeObject(message, new Newtonsoft.Json.JsonSerializerSettings
                    {
                        StringEscapeHandling = Newtonsoft.Json.StringEscapeHandling.Default,
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                    }));

                    return await _client.PostAsync(api, new StringContent(JsonSerializer.Serialize(message, options),
                        Encoding.UTF8, "application/json"));
                }

                return null;
            }
            catch (Exception e)
            {
                LogError(e);
                return null;
            }
        }

        private async Task GetOnlineCashRegister()
        {
            try
            {
                if (_cashRegister != null)
                {
                    if (await IsInternetConnectionAvailable())
                    {
                        _client = new()
                        {
                            BaseAddress = new Uri(_cashRegister.Address)
                        };
                        _client.DefaultRequestHeaders.Accept.Clear();
                        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                        _client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
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

        private async Task CreateJGP(string[]? base64Strings)
        {
            try
            {
                if (base64Strings != null)
                {
                    // Обрабатываем каждую строку Base64
                    foreach (string base64String in base64Strings)
                    {
                        // 1. Декодируем Base64 в массив байтов
                        byte[] imageBytes = Convert.FromBase64String(base64String);

                        // 2. Записываем байты напрямую в файл JPG
                        string outputPath = $"D:\\Чеки\\output_image {DateTime.Now:dd.MM.yyyy HH-mm-ss-ffff}.jpg";
                        await File.WriteAllBytesAsync(outputPath, imageBytes);
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
        }

        private async Task<DayStateNewCas> GetStateAsync()
        {
            var response = new
            {

            };

            HttpResponseMessage? stateMessage = await SendRequest("/fiscal/shifts/getState", response);

            if (stateMessage != null && stateMessage.IsSuccessStatusCode)
            {
                GetSateNewCas? state = JsonSerializer.Deserialize<GetSateNewCas>(await stateMessage.Content.ReadAsStringAsync());

                if (state != null)
                {
                    return state.State;
                }
            }
            return DayStateNewCas.None;
        }

        private PayType GetPayType(PaymentType paymentType)
        {
            return paymentType switch
            {
                PaymentType.Cash => PayType.Cash,
                PaymentType.Cashless => PayType.Cashless,
                _ => PayType.Cash,
            };
        }

        private async Task<FiscalData?> CreateFiscalDataAsync(HttpResponseMessage? responseMessage)
        {
            try
            {
                if (responseMessage == null)
                {
                    return null;
                }

                OpenAndCloseRecResp? saleResult = JsonSerializer.Deserialize<OpenAndCloseRecResp>(await responseMessage.Content.ReadAsStringAsync());

                if (saleResult != null)
                {

                    LogData(Newtonsoft.Json.JsonConvert.SerializeObject(saleResult, new Newtonsoft.Json.JsonSerializerSettings
                    {
                        StringEscapeHandling = Newtonsoft.Json.StringEscapeHandling.Default,
                        NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore
                    }));

                    if (saleResult.Status == OpenAndCloseRecRespStatus.Success)
                    {
                        FiscalData fiscalData = new()
                        {
                            FiscalModule = saleResult.FMNumber,
                            FiscalDocument = saleResult.FDNumber,
                            RegistrationNumber = saleResult.RegistrationNumber,
                            Check = saleResult.QRCode,
                        };

                        await CreateJGP(saleResult.Bitmaps);

                        return fiscalData;
                    }
                }
            }
            catch (Exception e)
            {
                LogError(e);
            }
            return null;
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
