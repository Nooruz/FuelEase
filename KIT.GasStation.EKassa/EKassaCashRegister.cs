using KIT.GasStation.CashRegisters.Exceptions;
using KIT.GasStation.CashRegisters.Models;
using KIT.GasStation.CashRegisters.Services;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.EKassa.Models;
using KIT.GasStation.EKassa.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using QRCoder;
using Serilog;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;


namespace KIT.GasStation.EKassa
{
    public class EKassaCashRegister : ICashRegisterService
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private ILogger _logger;
        private CashRegister _cashRegister;
        private EKassaCashRegisterSettings _settings;
        private EkassaClient _client;

        #endregion

        #region Constructors

        public EKassaCashRegister(IHardwareConfigurationService hardwareConfigurationService)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
            // Инициализация логгера
            InitLog();
        }

        #endregion

        #region Public Voids

        /// <inheritdoc/>
        public async Task CloseShiftAsync(string cashierName)
        {
            _logger.Information("Закрытие смены ККМ...");

            var request = new ShiftCloseRequest
            {
                FiscalNumber = _cashRegister.RegistrationNumber,
                Txt = _settings.TapeType == TapeType.TXT ? true : null,
                Txt80 = _settings.TapeType == TapeType.TXT80 ? true : null,
            };

            var data = await _client.ShiftCloseAsync(request);

            PrintText(data.Txt);
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

            if (cashRegister.Settings is EKassaCashRegisterSettings settings)
            {
                _settings = settings;
            }
            
            _cashRegister = cashRegister;

            // Авторизация на сервере ЕКасса
            await LoginAsync();
        }

        /// <inheritdoc/>
        public async Task OpenShiftAsync(string cashierName)
        {
            _logger.Information("Открытие смены ККМ...");

            var request = new ShiftOpenRequest
            {
                FiscalNumber = _cashRegister.RegistrationNumber,
                Txt = _settings.TapeType == TapeType.TXT ? true : null,
                Txt80 = _settings.TapeType == TapeType.TXT80 ? true : null,
            };

            var data = await _client.ShiftOpenAsync(request);

            PrintText(data.Txt);
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> SaleAsync(FuelSale fuelSale, Fuel fuel, string cashierName, bool isBefore = true)
        {
            _logger.Information("Начало продажи через ККМ. Сумма: {Sum}, Топливо: {Fuel}", fuelSale.Sum, fuel.Name);

            int price = (int)(fuelSale.Price * 100);
            decimal quantity = Math.Round(fuelSale.Sum / fuelSale.Price, 6);
            string discount = string.Empty;
            string? received = fuelSale.CustomerSum != null ? (fuelSale.CustomerSum * 100).ToString() : string.Empty;

            if (fuelSale.DiscountSale != null)
            {
                quantity = Math.Round(fuelSale.Sum / fuelSale.DiscountSale.DiscountPrice, 6);
                price = (int)(fuelSale.DiscountSale.DiscountPrice * 100);
                discount = (((quantity * fuelSale.Price) - fuelSale.Sum) * 100).ToString();
            }

            var googs = new List<ReceiptGood>()
            {
                new() 
                {
                    CalcItemAttributeCode = 0,
                    Name = fuel.Name,
                    Sgtin = fuel.TNVED,
                    Price = price,
                    Quantity = quantity,
                    Unit = fuel.UnitOfMeasurement.Name,
                    St = (int)(fuel.SalesTax * 100),
                    Vat = fuel.ValueAddedTax ? 12 : 0
                }
            };

            var request = new ReceiptV2Request()
            {
                FiscalNumber = _cashRegister.RegistrationNumber,
                Goods = googs,
                Discount = discount,
                Received = received,
                Cash = fuelSale.PaymentType == PaymentType.Cash,
                Operation = ReceiptOperation.INCOME,
                Txt = _settings.TapeType == TapeType.TXT ? true : null,
                Txt80 = _settings.TapeType == TapeType.TXT80 ? true : null,
            };

            var data = await _client.CreateReceiptV2Async(request);

            _logger.Information("Отправка данных в ККМ: {Data}", JsonSerializer.Serialize(request));

            return new FiscalData
            {
                FiscalModule = GetFiscalModule(data),
                FiscalDocument = GetFiscalDocument(data),
                RegistrationNumber = data.FiscalNumber.ToString(),
            };
        }

        /// <inheritdoc/>
        public async Task XReportAsync(bool printReceipt = true)
        {
            _logger.Information("Получения статуса Х-Отчет...");

            var request = new ShiftStateRequest
            {
                FiscalNumber = _cashRegister.RegistrationNumber,
                Txt = _settings.TapeType == TapeType.TXT ? true : null,
                Txt80 = _settings.TapeType == TapeType.TXT80 ? true : null
            };

            var shiftReportData = await _client.ShiftStateAsync(request);

            if (shiftReportData != null)
            {
                PrintText(shiftReportData.Txt);
            }
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> ReturnAsync(FuelSale fuelSale, Fuel fuel)
        {
            if (fuelSale.FiscalData == null)
            {
                _logger.Error("Нет данных для возврата по чеку. [{Timestamp}]", DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff"));
                throw new CashRegisterException("Нет данных для возврата по чеку.");
            }

            _logger.Information("Начало возврата через ККМ. Сумма: {Sum}, Топливо: {Fuel}", fuelSale.Sum, fuel.Name);

            var receipt = new ReceiptV2Request
            {
                FiscalNumber = _cashRegister.RegistrationNumber,
                Goods = new List<ReceiptGood>
                {
                    new()
                    {
                        CalcItemAttributeCode = 0,
                        Name = fuel.Name,
                        Sgtin = fuel.TNVED,
                        Price = (int)(fuelSale.Price * 100),
                        Quantity = Math.Round(fuelSale.Sum / fuelSale.Price, 6),
                        Unit = fuel.UnitOfMeasurement.Name,
                        St = (int)(fuel.SalesTax * 100),
                        Vat = fuel.ValueAddedTax ? 12 : 0
                    }
                },
                Cash = fuelSale.PaymentType == PaymentType.Cash,
                Operation = ReceiptOperation.INCOME_RETURN,
                OriginFdNumber = fuelSale.FiscalData.FiscalDocument,
            };

            var data = await _client.CreateReceiptV2Async(receipt);

            _logger.Information("Отправка данных в ККМ: {Data}", JsonSerializer.Serialize(receipt));

            return new FiscalData
            {
                FiscalModule = GetFiscalModule(data),
                FiscalDocument = GetFiscalDocument(data),
                RegistrationNumber = data.FiscalNumber.ToString()
            };
        }

        /// <inheritdoc/>
        public async Task<CashRegisterState> GetShiftStateAsync()
        {
            try
            {
                _logger.Information("Получения статуса Х-Отчет...");

                var request = new ShiftStateRequest
                {
                    FiscalNumber = _cashRegister.RegistrationNumber,
                    Txt = _settings.TapeType == TapeType.TXT ? true : null,
                    Txt80 = _settings.TapeType == TapeType.TXT80 ? true : null
                };

                var shiftReportData = await _client.ShiftStateAsync(request);

                _logger.Information("Статус кассы: {Status}", _cashRegister.Status);

                if (shiftReportData.Fields?.Tags.TryGetValue("1012", out var tag1012) == true)
                {
                    var openDate = DateTime.Parse(tag1012.GetString()!);

                    return new CashRegisterState
                    {
                        OpenedAt = openDate
                    };
                }
                return new CashRegisterState();
            }
            catch (EkassaHttpException e)
            {
                _logger.Error(e, "Ошибка при получении статуса смены ККМ. Код ошибки: {StatusCode}, Сообщение: {EkassaError}", e.StatusCode, e.EkassaError);
                return new CashRegisterState()
                {
                    Status = CashRegisterStatus.Close
                };
            }
            catch (Exception e)
            {
                _logger.Error(e, "Ошибка при получении статуса смены ККМ.");
                return new CashRegisterState();
            }
        }

        /// <inheritdoc/>
        public async Task<FiscalData?> ReturnAndReceivedSaleAsync(FuelSale fuelSale, Fuel fuel, string cashierName)
        {
            await ReturnAsync(fuelSale, fuel);

            if (fuelSale.ReceivedSum == 0)
            {
                _logger.Information("Продажа полученных сумм не требуется, так как сумма равна 0.");
                return null;
            }

            _logger.Information("Начало продажи через ККМ. Сумма: {Sum}, Топливо: {Fuel}", fuelSale.Sum, fuel.Name);

            decimal price = fuelSale.Price * 100;
            decimal quantity = Math.Round(fuelSale.ReceivedSum / fuelSale.Price, 6);
            string discount = string.Empty;
            string received = fuelSale.CustomerSum != null ? (fuelSale.CustomerSum * 100).ToString() : string.Empty;

            if (fuelSale.DiscountSale != null)
            {
                quantity = Math.Round(fuelSale.ReceivedSum / fuelSale.DiscountSale.DiscountPrice, 6);
                price = fuelSale.DiscountSale.DiscountPrice * 100;
                discount = (((quantity * fuelSale.Price) - fuelSale.ReceivedSum) * 100).ToString();
            }

            var receipt = new ReceiptV2Request
            {
                FiscalNumber = _cashRegister.RegistrationNumber,
                Received = received,
                Discount = discount,
                Txt = _settings.TapeType == TapeType.TXT ? true : null,
                Txt80 = _settings.TapeType == TapeType.TXT80 ? true : null,
                Goods = new List<ReceiptGood>
                {
                    new()
                    {
                        CalcItemAttributeCode = 0,
                        Name = fuel.Name,
                        Sgtin = fuel.TNVED,
                        Price = (int)price,
                        Quantity = quantity,
                        Unit = fuel.UnitOfMeasurement.Name,
                        St = (int)(fuel.SalesTax * 100),
                        Vat = fuel.ValueAddedTax ? 12 : 0
                    }
                },
                Cash = fuelSale.PaymentType == PaymentType.Cash,
                Operation = ReceiptOperation.INCOME,
            };

            _logger.Information("Отправка данных в ККМ: {Data}", JsonSerializer.Serialize(receipt));

            var data = await _client.CreateReceiptV2Async(receipt);

            return new FiscalData
            {
                FiscalModule = GetFiscalModule(data),
                FiscalDocument = GetFiscalDocument(data),
                RegistrationNumber = data.FiscalNumber.ToString()
            };
        }

        #endregion

        #region Private Voids

        /// <summary>
        /// Проверяет доступность интернета посредством ICMP-запроса.
        /// </summary>
        private bool IsInternetConnectionAvailable()
        {
            try
            {
                using Ping ping = new();
                string host = GetDomainFromUrl(_cashRegister.Address);

                PingReply reply = ping.Send(host, 3000);

                if (reply.Status != IPStatus.Success)
                {
                    throw new CashRegisterException(
                        "Не удалось связаться с сервером. Проверьте интернет-соединение или адрес кассы."
                    );
                }

                return true;
            }
            catch (PingException ex)
            {
                _logger.Error(ex, "Ошибка при проверке соединения (Ping).");
                throw new CashRegisterException(
                    "Ошибка проверки интернет-соединения. Проверьте интернет или доступ к серверу."
                );
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Неизвестная ошибка при проверке интернет-соединения.");
                throw new CashRegisterException(
                    "Не удалось проверить соединение с сервером. Проверьте интернет и попробуйте ещё раз."
                );
            }
        }

        /// <summary>
        /// Извлекает доменное имя из URL.
        /// </summary>
        private string GetDomainFromUrl(string url)
        {
            try
            {
                Uri uri = new(url);
                return uri.Host;
            }
            catch (UriFormatException e)
            {
                _logger.Error(e, "Неверный адрес URL адрес ККМ");
                return string.Empty;
            }
        }

        /// <summary>
        /// Авторизация на сервере ЕКасса.
        /// </summary>
        private async Task LoginAsync()
        {
            if (!IsInternetConnectionAvailable())
            {
                return;
            }

            var options = new EkassaOptions
            {
                BaseUri = new Uri(_cashRegister.Address),
                Email = _cashRegister.UserName,
                Password = _cashRegister.Password,
                Timeout = TimeSpan.FromSeconds(30),
                MaxAuthRetry = 1
            };

            var tokenStore = new EkassaTokenStore();

            var loginHttp = new HttpClient
            {
                BaseAddress = options.BaseUri,
                Timeout = options.Timeout
            };

            var loginApi = new EkassaLoginApi(loginHttp, options);

            var loginData = await loginApi.LoginAsync(CancellationToken.None);
            tokenStore.SetToken(loginData);

            var authHandler = new EkassaAuthHandler(tokenStore, options, loginApi)
            {
                InnerHandler = new HttpClientHandler()
            };

            var apiHttp = new HttpClient(authHandler)
            {
                BaseAddress = options.BaseUri,
                Timeout = options.Timeout
            };

            _client = new EkassaClient(apiHttp, loginApi, tokenStore, options);
        }

        private void PrintText(string? check, string? url = null)
        {
            try
            {
                PrintDocument printDoc = new();
                printDoc.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                printDoc.PrinterSettings.PrinterName = _settings.DefaultPrinterName;

                // Установим размер бумаги: 58мм или 80мм × большая высота (не ограничиваем)
                int paperWidthMm = GetPaperWidthMm(_settings.TapeType);
                int paperWidth = (int)(paperWidthMm / 25.4 * 100); // 1/100 дюйма
                int paperHeight = 800; // 50 дюймов - достаточно для любого чека
                printDoc.DefaultPageSettings.PaperSize = new PaperSize("Custom", paperWidth, paperHeight);

                printDoc.PrintPage += (sender, e) =>
                {
                    int printerDpi = 203;

                    LayoutInfo layout = CalculateLayout(printerDpi, paperWidthMm);

                    float yPos = layout.TopMargin;

                    // Уменьшим размер шрифта до 6pt
                    Font textFont = new Font("Courier New", 6f, FontStyle.Regular);

                    IEnumerable<string> lines = NormalizeLines(check ?? string.Empty, e.Graphics, textFont, layout.ContentWidth);
                    DrawTextLines(e.Graphics, textFont, layout.ContentWidth, layout.LeftMargin, ref yPos, lines);

                    float textHeight = yPos - layout.TopMargin;

                    bool canIncludeQr = !string.IsNullOrWhiteSpace(url);
                    bool qrDrawn = false;
                    float qrX = 0f;
                    float qrY = 0f;
                    float qrWidth = 0f;

                    if (canIncludeQr)
                    {
                        yPos += layout.TextQrSpacing;
                        using Bitmap qrCodeImage = GenerateQrCodeForThermalPrinter(url, layout.TargetQrSize);
                        if (qrCodeImage != null)
                        {
                            DrawQr(e.Graphics, qrCodeImage, layout.ContentWidth, layout.LeftMargin, layout.BottomMargin, layout.PrinterDpi, ref yPos,
                                out qrX, out qrY, out qrWidth);
                            qrDrawn = true;
                        }
                        else
                        {
                            yPos += layout.BottomMargin;
                        }
                    }
                    else
                    {
                        yPos += layout.BottomMargin;
                    }

                    // Логируем итоговые параметры
                    _logger.Information($"Расположение элементов:");
                    _logger.Information($"- Текст: начальная позиция {layout.TopMargin}, высота {textHeight}");
                    if (qrDrawn)
                    {
                        _logger.Information($"- QR-код: x={qrX:F2}, y={qrY:F2}, размер={qrWidth:F2}x{qrWidth:F2}");
                    }
                    else
                    {
                        _logger.Information("- QR-код: не печатается");
                    }

                    _logger.Information($"- Общая высота чека: {yPos:F2} (1/100 дюйма)");
                    _logger.Information($"- Это примерно: {yPos / 100 * 25.4:F1} мм");

                    textFont.Dispose();

                    e.HasMorePages = false; // Только одна страница
                };

                printDoc.Print();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка во время печати страницы");
            }
        }

        private LayoutInfo CalculateLayout(int printerDpi, int paperWidthMm)
        {
            // Немного уменьшим отступы
            int leftRightMarginMm = paperWidthMm <= 58 ? 3 : 6;
            int topBottomMarginMm = 10;  // 10мм сверху и снизу

            // Конвертируем мм в единицы принтера (dots)
            int leftMarginDots = (int)(leftRightMarginMm * printerDpi / 25.4);
            int rightMarginDots = (int)(leftRightMarginMm * printerDpi / 25.4);
            int topMarginDots = (int)(topBottomMarginMm * printerDpi / 25.4);
            int bottomMarginDots = (int)(topBottomMarginMm * printerDpi / 25.4);

            // Конвертируем dots в 1/100 дюйма для Graphics
            float leftMargin = leftMarginDots / (float)printerDpi * 100;
            float rightMargin = rightMarginDots / (float)printerDpi * 100;
            float topMargin = topMarginDots / (float)printerDpi * 100;
            float bottomMargin = bottomMarginDots / (float)printerDpi * 100;

            // Ширина бумаги в 1/100 дюйма
            float paperWidthInches = paperWidthMm / 25.4f;
            float printWidth = paperWidthInches * 100;

            // Уменьшим коэффициент безопасности
            float safetyFactor = 0.98f; // Уменьшили до 0.98
            float contentWidth = (printWidth - leftMargin - rightMargin) * safetyFactor;

            _logger.Information($"Ширина бумаги: {printWidth} (1/100 дюйма)");
            _logger.Information($"Доступная ширина контента: {contentWidth} (с учетом коэффициента безопасности {safetyFactor})");
            _logger.Information($"Отступы: слева={leftMargin}, справа={rightMargin}, сверху={topMargin}, снизу={bottomMargin}");

            // Размер QR-кода - занимает всю доступную ширину (но не более 300 точек)
            int maxQrSize = paperWidthMm <= 58 ? 200 : 250; // Уменьшили до 300
            int targetQrSize = Math.Min((int)(contentWidth / 100 * printerDpi), maxQrSize);

            _logger.Information($"Размер QR-кода: {targetQrSize}x{targetQrSize} dots");

            // Отступ между текстом и QR-кодом (в мм, конвертируем в 1/100 дюйма)
            int textQrSpacingMm = 5;
            float textQrSpacing = textQrSpacingMm / 25.4f * 100;

            return new LayoutInfo(printerDpi, leftMargin, rightMargin, topMargin, bottomMargin, contentWidth, printWidth, textQrSpacing, targetQrSize);
        }

        private IEnumerable<string> NormalizeLines(string text, Graphics graphics, Font font, float contentWidth)
        {
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            string longestLine = "";
            float maxLineWidth = 0;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    SizeF lineSize = graphics.MeasureString(trimmedLine, font);
                    if (lineSize.Width > maxLineWidth)
                    {
                        maxLineWidth = lineSize.Width;
                        longestLine = trimmedLine;
                    }
                }
            }

            _logger.Information($"Самая длинная строка: '{longestLine}'");
            _logger.Information($"Ширина самой длинной строки: {maxLineWidth}, доступная ширина: {contentWidth}");

            if (maxLineWidth > contentWidth)
            {
                _logger.Warning($"Строка не помещается! Превышение: {maxLineWidth - contentWidth}");
            }

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine))
                {
                    yield return string.Empty;
                    continue;
                }

                SizeF lineSize = graphics.MeasureString(trimmedLine, font);

                if (trimmedLine.StartsWith("-") && trimmedLine.Length > 1 && trimmedLine.All(c => c == '-'))
                {
                    int targetChars = (int)(contentWidth / (lineSize.Width / trimmedLine.Length));
                    if (targetChars < 3) targetChars = 3;
                    trimmedLine = new string('-', targetChars);
                    lineSize = graphics.MeasureString(trimmedLine, font);
                }

                if (lineSize.Width > contentWidth)
                {
                    string currentText = trimmedLine;
                    while (currentText.Length > 3)
                    {
                        currentText = currentText.Substring(0, currentText.Length - 1);
                        lineSize = graphics.MeasureString(currentText, font);
                        if (lineSize.Width <= contentWidth)
                        {
                            trimmedLine = currentText;
                            break;
                        }
                    }

                    _logger.Information($"Обрезана строка до: '{trimmedLine}'");
                }

                yield return trimmedLine;
            }
        }

        private void DrawTextLines(Graphics graphics, Font font, float contentWidth, float leftMargin, ref float yPos, IEnumerable<string> lines)
        {
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line))
                {
                    yPos += font.GetHeight(graphics);
                    continue;
                }

                SizeF lineSize = graphics.MeasureString(line, font);
                graphics.DrawString(line, font, Brushes.Black, leftMargin, yPos);
                yPos += lineSize.Height;
            }
        }

        private void DrawQr(
            Graphics graphics,
            Bitmap qrCodeImage,
            float contentWidth,
            float leftMargin,
            float bottomMargin,
            int printerDpi,
            ref float yPos,
            out float qrX,
            out float qrY,
            out float qrWidth)
        {
            qrWidth = qrCodeImage.Width / (float)printerDpi * 100;
            float qrHeight = qrWidth;

            qrX = leftMargin + (contentWidth - qrWidth) / 2;
            if (qrX < leftMargin)
            {
                qrX = leftMargin;
            }
            qrY = yPos;

            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

            graphics.DrawImage(qrCodeImage, qrX, qrY, qrWidth, qrHeight);
            yPos += qrHeight + bottomMargin;
        }

        private sealed record LayoutInfo(
            int PrinterDpi,
            float LeftMargin,
            float RightMargin,
            float TopMargin,
            float BottomMargin,
            float ContentWidth,
            float PrintWidth,
            float TextQrSpacing,
            int TargetQrSize);

        private Bitmap GenerateQrCodeForThermalPrinter(string url, int sizeInPixels)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;

            var gen = new QRCodeGenerator();
            var data = gen.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);

            using var qr = new QRCode(data);

            // Генерируем QR-код нужного размера
            int modules = data.ModuleMatrix.Count;
            int pixelsPerModule = Math.Max(1, sizeInPixels / modules);

            var bmp = qr.GetGraphic(
                pixelsPerModule: pixelsPerModule,
                darkColor: Color.Black,
                lightColor: Color.White,
                drawQuietZones: true);

            bmp.SetResolution(203, 203);

            // Если изображение не точно нужного размера, ресайзим
            if (bmp.Width != sizeInPixels || bmp.Height != sizeInPixels)
            {
                var resized = new Bitmap(sizeInPixels, sizeInPixels, PixelFormat.Format24bppRgb);
                resized.SetResolution(203, 203);

                using (var g = Graphics.FromImage(resized))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                    g.DrawImage(bmp, 0, 0, sizeInPixels, sizeInPixels);
                }

                bmp.Dispose();
                return resized;
            }

            return bmp;
        }

        private int GetPaperWidthMm(TapeType tapeType)
        {
            return tapeType switch
            {
                TapeType.TXT => 58,
                TapeType.TXT80 => 80,
                _ => 80
            };
        }

        #endregion

        #region Fiscal Documents

        private int? GetFiscalDocument(ReceiptData data)
        {
            if (data.Fields?.Tags.TryGetValue("1040", out JsonElement fd) == true)
            {
                string fiscalDocumentString = fd.ToString();
                if (int.TryParse(fiscalDocumentString, out int fiscalDocument))
                {
                    return fiscalDocument;
                }
            }
            return null;
        }

        private string? GetFiscalModule(ReceiptData data)
        {
            if (data.Fields?.Tags.TryGetValue("1041", out JsonElement fd) == true)
            {
                return fd.ToString();
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
            var logFilePath = Path.Combine(logsDir, $"{nameof(EKassaCashRegister)}_{DateTime.Now:dd.MM.yyyy}.log");

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
            _logger.Information("=======================ЕКасса инициализирован=======================");
        }

        #endregion
    }
}
