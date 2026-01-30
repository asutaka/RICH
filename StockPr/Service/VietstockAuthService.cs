using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using StockPr.Config;

namespace StockPr.Service
{
    public interface IVietstockAuthService
    {
        Task<AuthSession> LoginAsync();
    }
    public class VietstockAuthService : IVietstockAuthService
    {
        private readonly VietstockOptions _opt;
        private readonly ILogger<VietstockAuthService> _logger;

        public VietstockAuthService(IOptions<VietstockOptions> opt, ILogger<VietstockAuthService> logger)
        {
            _opt = opt.Value;
            _logger = logger;
        }

        public async Task<AuthSession> LoginAsync()
        {
            try
            {
                _logger.LogInformation("Starting Robust Vietstock Login (Final Attempt)...");
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions 
                    { 
                        Headless = true,
                        Args = new[] { 
                            "--disable-blink-features=AutomationControlled",
                            "--no-sandbox",
                            "--disable-gpu",
                            "--host-resolver-rules=MAP id.vietstock.vn 183.81.34.197, MAP finance.vietstock.vn 221.132.38.227"
                        }
                    });

                var context = await browser.NewContextAsync(new()
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
                    IgnoreHTTPSErrors = true,
                    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
                });

                var page = await context.NewPageAsync();
                
                string[] urls = { 
                    "https://id.vietstock.vn/dang-nhap",
                    "https://finance.vietstock.vn/",
                    "https://id.vietstock.vn.ftech.ai/dang-nhap"
                };
                
                bool foundForm = false;
                foreach (var url in urls)
                {
                    try {
                        _logger.LogInformation($"Navigating to {url}...");
                        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.Commit, Timeout = 30000 });
                        await Task.Delay(3000);

                        // Xóa các thành phần gây nhiễu nhưng giữ lại form
                        await page.EvaluateAsync(@"
                            document.querySelectorAll('.ads-popup, .modal-backdrop, .popup:not(:has(#email)):not(:has(#txtUsername))').forEach(el => el.remove());
                            document.body.classList.remove('modal-open');
                        ");

                        // Kiểm tra form
                        var userField = page.Locator("#txtUsername, #email").First;
                        if (await userField.CountAsync() > 0) {
                            foundForm = true;
                            _logger.LogInformation($"Form detected on {url}");
                            break;
                        }

                        // Nếu là homepage, thử click nút Đăng nhập
                        if (page.Url.Contains("finance.vietstock.vn")) {
                            var loginBtn = page.Locator("a:has-text('Đăng nhập'), a:has-text('Login'), a[href*='dang-nhap']").First;
                            if (await loginBtn.CountAsync() > 0) {
                                await loginBtn.ClickAsync(new LocatorClickOptions { Force = true });
                                await Task.Delay(5000);
                                if (await page.Locator("#txtUsername, #email").First.CountAsync() > 0) {
                                    foundForm = true;
                                    break;
                                }
                            }
                        }
                    } catch { }
                }

                if (!foundForm) throw new Exception("Login form not found on any URL.");

                // Xác định bộ SELECTOR chính xác
                string userSel = await page.Locator("#txtUsername").CountAsync() > 0 ? "#txtUsername" : "#email";
                string passSel = await page.Locator("#txtPassword").CountAsync() > 0 ? "#txtPassword" : "#password";
                
                // Cải tiến: Tìm nút submit thông minh hơn
                var submitBtn = page.Locator("#btnLogin, #btnSubmit, button[type='submit'], input[type='submit']").First;
                string btnSel = await submitBtn.CountAsync() > 0 ? await submitBtn.EvaluateAsync<string>("el => { if(el.id) return '#' + el.id; return el.className ? '.' + el.className.split(' ')[0] : 'button[type=submit]'; }") : "#btnLogin";

                _logger.LogInformation($"Using selectors: User={userSel}, Pass={passSel}, Btn={btnSel}");

                // Điền thông tin bằng JS để vượt qua mọi rào cản Visibility
                await page.EvaluateAsync($@"(data) => {{
                    const u = document.querySelector('{userSel}');
                    const p = document.querySelector('{passSel}');
                    if (u) {{ u.value = data.user; u.dispatchEvent(new Event('input', {{bubbles:true}})); }}
                    if (p) {{ p.value = data.pass; p.dispatchEvent(new Event('input', {{bubbles:true}})); }}
                }}", new { user = _opt.Username, pass = _opt.Password });

                _logger.LogInformation("Clicking the login button...");
                try {
                    // Thử click bằng Playwright trước
                    await submitBtn.ClickAsync(new LocatorClickOptions { Force = true, Timeout = 5000 });
                } catch {
                    // Nếu Playwright click lỗi, dùng JS click cưỡng bức
                    _logger.LogWarning("Playwright click failed, trying JavaScript forced click...");
                    await page.EvaluateAsync($@"() => {{
                        const btn = document.querySelector('{btnSel}') || document.querySelector('button[type=""submit""]') || document.querySelector('#btnLogin') || document.querySelector('#btnSubmit');
                        if (btn) btn.click();
                    }}");
                }

                _logger.LogInformation("Waiting for authentication to complete...");
                await Task.Delay(5000); // Chờ redirect

                var cookies = await context.CookiesAsync();
                var csrfToken = "";
                try {
                    csrfToken = await page.EvalOnSelectorAsync<string>("input[name='__RequestVerificationToken']", "el => el.value");
                } catch { }

                _logger.LogInformation("Session data retrieved successfully.");

                return new AuthSession
                {
                    Cookies = cookies,
                    CsrfToken = csrfToken,
                    ExpiredAt = DateTime.UtcNow.AddMinutes(60)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VietstockAuthService Error.");
                throw;
            }
        }
    }

    public class AuthSession
    {
        public IReadOnlyList<BrowserContextCookiesResult> Cookies { get; set; } = [];
        public string CsrfToken { get; set; } = "";
        public DateTime ExpiredAt { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiredAt;
    }
}
