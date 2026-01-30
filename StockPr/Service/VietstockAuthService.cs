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
                _logger.LogInformation("Starting Robust Vietstock Login...");
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions 
                    { 
                        Headless = true,
                        Args = new[] { 
                            "--disable-blink-features=AutomationControlled",
                            "--no-sandbox",
                            "--disable-gpu"
                        }
                    });

                var context = await browser.NewContextAsync(new()
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
                    IgnoreHTTPSErrors = true,
                    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
                });

                var page = await context.NewPageAsync();
                
                _logger.LogInformation("Navigating to https://finance.vietstock.vn/...");
                try {
                    await page.GotoAsync("https://finance.vietstock.vn/", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 30000 });
                } catch (Exception ex) {
                    _logger.LogWarning($"Initial navigation timed out or failed: {ex.Message}. Trying to proceed.");
                }
                await Task.Delay(2000); // Allow some time for scripts to run

                // Thử đóng bất kỳ popup quảng cáo nào nếu có
                try {
                    await page.EvaluateAsync(@"
                        document.querySelectorAll('.ads-popup, .modal-backdrop, .close, .btn-close').forEach(el => {
                            if(el.offsetParent !== null) el.click();
                        });
                    ");
                } catch { }

                _logger.LogInformation("Opening login popup...");
                var loginTrigger = page.Locator("a.btnlogin-link:has-text('Đăng nhập'), a:has-text('Đăng nhập'), #login-link").First;
                if (await loginTrigger.CountAsync() > 0) {
                    await loginTrigger.ClickAsync(new LocatorClickOptions { Force = true });
                } else {
                    _logger.LogWarning("Login link with class btnlogin-link not found, trying common selectors...");
                    var fallbackTrigger = page.Locator("a:has-text('Đăng nhập'), a[href*='dang-nhap']").First;
                    if (await fallbackTrigger.CountAsync() > 0) {
                        await fallbackTrigger.ClickAsync(new LocatorClickOptions { Force = true });
                    } else {
                        _logger.LogWarning("Login link not found on homepage, navigating directly to id.vietstock.vn");
                        await page.GotoAsync("https://id.vietstock.vn/dang-nhap", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                    }
                }

                // Đợi form xuất hiện - Chuyển sang Attached để tránh lỗi Visibility nếu popup animation chậm
                var userSel = "#txtEmailLogin";
                var passSel = "#txtPassword"; // Theo thông tin ban đầu của bạn
                var btnSel = "#btnLoginAccount";

                _logger.LogInformation("Waiting for login form (Attached)...");
                try {
                    await page.WaitForSelectorAsync(userSel, new PageWaitForSelectorOptions { State = WaitForSelectorState.Attached, Timeout = 15000 });
                    
                    // Cưỡng bức xóa bỏ các lớp phủ (backdrop) có thể chặn tương tác
                    await page.EvaluateAsync(@"() => {
                        document.querySelectorAll('.modal-backdrop, .modal-stack, .loading-backdrop').forEach(el => el.remove());
                        document.body.classList.remove('modal-open');
                        const el = document.querySelector('#txtEmailLogin');
                        if(el) el.scrollIntoView();
                    }");
                } catch {
                    _logger.LogWarning("Selector #txtEmailLogin not attached, trying fallback...");
                    userSel = await page.Locator("#txtEmailLogin").CountAsync() > 0 ? "#txtEmailLogin" : "#email";
                    passSel = await page.Locator("#txtPassword").CountAsync() > 0 ? "#txtPassword" : "#password";
                    btnSel = await page.Locator("#btnLoginAccount").CountAsync() > 0 ? "#btnLoginAccount" : "#btnLogin";
                }

                _logger.LogInformation("Performing login via direct Fetch to bypass UI/Timeout issues...");
                
                var fetchSuccess = await page.EvaluateAsync<bool>($@"(data) => {{
                    return new Promise(async (resolve) => {{
                        try {{
                            let token = document.querySelector('input[name=""__RequestVerificationToken""]')?.value;
                            if (!token) {{
                                // Thử tìm trong toàn bộ document nếu popup chưa render hẳn
                                token = document.querySelector('[name=""__RequestVerificationToken""]')?.value;
                            }}
                            
                            if (!token) {{
                                console.error('CSRF Token not found');
                                return resolve(false);
                            }}

                            const formData = new URLSearchParams();
                            formData.append('__RequestVerificationToken', token);
                            formData.append('Email', data.user);
                            formData.append('Password', data.pass);
                            formData.append('responseCaptchaLoginPopup', '');
                            formData.append('g-recaptcha-response', '');
                            formData.append('Remember', 'false');
                            formData.append('X-Requested-With', 'XMLHttpRequest');

                            const response = await fetch('/Account/Login', {{
                                method: 'POST',
                                headers: {{
                                    'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                                    'X-Requested-With': 'XMLHttpRequest'
                                }},
                                body: formData.toString()
                            }});
                            
                            const result = await response.text();
                            console.log('Login response:', result);
                            resolve(response.ok);
                        }} catch (e) {{
                            console.error('Fetch error:', e);
                            resolve(false);
                        }}
                    }});
                }}", new { user = _opt.Username, pass = _opt.Password });

                if (fetchSuccess) {
                    _logger.LogInformation("Login success. Reloading page to synchronize CSRF tokens...");
                    // Điều hướng đến trang tài chính để lấy Token chuẩn nhất cho các API data
                    try {
                        await page.GotoAsync("https://finance.vietstock.vn/ACB/tai-chinh.htm", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 20000 });
                    } catch { }
                    await Task.Delay(3000); 
                } else {
                    _logger.LogWarning("Login fetch failed or token was missing. Trying UI fallback...");
                    // Fallback click just in case
                    try { await page.ClickAsync(btnSel, new PageClickOptions { Force = true, Timeout = 5000 }); } catch { }
                }

                // Đợi redirect hoặc xuất hiện dấu hiệu đã đăng nhập
                _logger.LogInformation("Waiting for authentication success sign...");
                bool loginSuccess = false;
                
                // Thử đợi trong 10 giây để thấy dấu hiệu đăng nhập thành công
                for (int i = 0; i < 10; i++)
                {
                    var cookiesCheck = await context.CookiesAsync();
                    if (cookiesCheck.Any(c => c.Name.Contains("CookieLogin") || c.Name.Contains("vts_usr_lg")))
                    {
                        loginSuccess = true;
                        _logger.LogInformation("Login verified via Cookies.");
                        break;
                    }
                    
                    var logoutExists = await page.Locator("a:has-text('Thoát'), .user-profile, .logout-link, a[href*='dang-xuat']").CountAsync();
                    if (logoutExists > 0)
                    {
                        loginSuccess = true;
                        _logger.LogInformation("Login verified via 'Logout' button.");
                        break;
                    }
                    await Task.Delay(1000);
                }

                if (!loginSuccess) {
                    _logger.LogWarning("Login indicators not found after 10s. Proceeding with current session data.");
                }

                var finalCookies = await context.CookiesAsync();
                var csrfToken = "";
                try {
                    csrfToken = await page.EvalOnSelectorAsync<string>("input[name='__RequestVerificationToken']", "el => el.value");
                } catch { 
                    _logger.LogWarning("Could not find __RequestVerificationToken on final page.");
                }

                _logger.LogInformation("Session data retrieved successfully.");

                return new AuthSession
                {
                    Cookies = finalCookies,
                    CsrfToken = csrfToken,
                    ExpiredAt = DateTime.UtcNow.AddMinutes(120)
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
