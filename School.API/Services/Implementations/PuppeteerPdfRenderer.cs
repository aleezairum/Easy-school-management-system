using PuppeteerSharp;
using PuppeteerSharp.Media;
using School.API.Services.Interfaces;

namespace School.API.Services.Implementations
{
    // HTML -> PDF using a single shared headless Chromium instance (PuppeteerSharp).
    // Chromium is downloaded once on first use (cached under the app folder).
    // Registered as a singleton so the browser is launched only once for the
    // whole process; a SemaphoreSlim serializes browser launch / disposal.
    public sealed class PuppeteerPdfRenderer : IPdfRenderer, IAsyncDisposable
    {
        private readonly ILogger<PuppeteerPdfRenderer> _logger;
        private readonly SemaphoreSlim _initLock = new(1, 1);
        private IBrowser? _browser;

        public PuppeteerPdfRenderer(ILogger<PuppeteerPdfRenderer> logger)
        {
            _logger = logger;
        }

        private async Task<IBrowser> GetBrowserAsync()
        {
            if (_browser is { IsConnected: true }) return _browser;

            await _initLock.WaitAsync();
            try
            {
                if (_browser is { IsConnected: true }) return _browser;

                _logger.LogInformation("[PDF] Ensuring Chromium is available (one-time download if missing)...");
                var browserFetcher = new BrowserFetcher();
                var installed = await browserFetcher.DownloadAsync();
                var execPath = installed.GetExecutablePath();

                _logger.LogInformation("[PDF] Launching headless Chromium from {Path}...", execPath);
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Headless = true,
                    ExecutablePath = execPath,
                    Timeout = 60000,
                    Args = new[]
                    {
                        "--no-sandbox",
                        "--disable-setuid-sandbox",
                        "--disable-dev-shm-usage",
                        "--disable-gpu",
                        "--no-first-run",
                        "--disable-extensions"
                    }
                });
                return _browser;
            }
            finally
            {
                _initLock.Release();
            }
        }

        public async Task<byte[]> RenderHtmlToPdfAsync(string html)
        {
            var browser = await GetBrowserAsync();
            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(html, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 }
            });

            return await page.PdfDataAsync(new PdfOptions
            {
                Format = PaperFormat.A4,
                PrintBackground = true,
                MarginOptions = new MarginOptions
                {
                    Top = "6mm",
                    Bottom = "6mm",
                    Left = "8mm",
                    Right = "8mm"
                }
            });
        }

        public async ValueTask DisposeAsync()
        {
            if (_browser is not null)
            {
                await _browser.CloseAsync();
                await _browser.DisposeAsync();
            }
            _initLock.Dispose();
        }
    }
}
