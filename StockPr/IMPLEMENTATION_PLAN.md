# Kế hoạch Nâng cấp Hệ thống StockPr

Tài liệu này dùng để theo dõi tiến độ và chi tiết kỹ thuật của quá trình refactor và nâng cấp project StockPr. Hệ thống sẽ được chuyển đổi từ một Worker Service chạy vòng lặp đơn giản sang một hệ thống phân tán tác vụ (Job-based) ổn định và chuyên nghiệp hơn.

---

## 📊 Trạng thái Tổng quát
- **Phiên bản hiện tại:** 1.0.0 (Legacy Worker)
- **Phiên bản mục tiêu:** 2.0.0 (Modular Jobs & Resilience)
- **Tiến độ:** 90%

---

## 🛠 Chi tiết các Giai đoạn Implement

### Giai đoạn 1: Chuẩn hóa Cấu hình và Hạ tầng (Core Foundation)
*Mục tiêu: Loại bỏ sự phụ thuộc vào biến static và thiết lập cơ chế xử lý lỗi mạng.*

- [x] **1.1. Loại bỏ StaticVal & Cấu hình hóa (Static to Options Pattern)**
    - [x] Di chuyển `_VietStock_Cookie`, `_VietStock_Token` từ `StaticVal` vào `VietStockSettings`.
    - [x] Thay thế mọi tham chiếu đến `StaticVal` bằng `IOptions<T>` thông qua Dependency Injection.
    - [x] Cập nhật `Program.cs` để bind cấu hình từ `appsettings.json` một cách chặt chẽ.
- [x] **1.2. Tích hợp Resilience (Polly Policy)**
    - [x] Cài đặt NuGet: `Polly`, `Microsoft.Extensions.Http.Polly`.
    - [x] Định nghĩa `RetryPolicy` (thử lại khi lỗi 5xx, 408) và `CircuitBreakerPolicy` (ngưng gọi khi API nguồn sập).
    - [x] Áp dụng Polly vào `HttpClientFactory` của hệ thống.

### Giai đoạn 2: Tái cấu trúc Dịch vụ API (Refactor IAPIService)
*Mục tiêu: Chia nhỏ "God Interface" IAPIService thành các module chuyên biệt.*

- [x] **2.1. Tách Scraper Service (Extraction)**
    - [x] Tạo `IScraperService` và `ScraperService`.
    - [x] Di chuyển mọi logic thu thập dữ liệu (scraping) từ `APIService` sang `ScraperService`.
    - [x] Đăng ký `ScraperService` trong `Program.cs` / `RegisterService.cs`.
    - [x] Cập nhật `APIService` để phụ thuộc vào `IScraperService`.
- [x] **2.2. Tách Market Data Service**
    - [x] Định nghĩa `IMarketDataService` cho các phương thức lấy dữ liệu giao dịch (SSI, Vietstock, Money24h).
    - [x] Triển khai `MarketDataService` và di chuyển logic từ `APIService`.
    - [x] Cập nhật `APIService` để sử dụng `IMarketDataService`.
- [x] **2.3. Tách Vietstock Service**
    - [x] Định nghĩa `IVietstockService` cho các phương thức lấy BCTC, KQKD, GICS.
    - [x] Triển khai `VietstockService` và sử dụng `IVietstockAuthService`.
    - [x] Cập nhật `APIService` để sử dụng `IVietstockService`.
- [x] **2.4. Tách Macro Data Service**
    - [x] Định nghĩa `IMacroDataService` cho PigPrice, Commodities, MacroMicro, TongCucThongKe.
    - [x] Triển khai `MacroDataService`.
    - [x] Cập nhật `APIService` để sử dụng `IMacroDataService`.
- [ ] **2.5. Tối ưu hóa Code logic**
    - [ ] Tách logic parse HTML (HtmlAgilityPack) ra các `Parsers` riêng lẻ để dễ viết Unit Test.
    - [ ] Sử dụng `System.Text.Json` (nếu có thể) thay cho `Newtonsoft.Json` để tối ưu hiệu năng.

### Giai đoạn 3: Hiện đại hóa Điều phối Tác vụ (Job Scheduler)
*Mục tiêu: Xóa bỏ vòng lặp `while(true)` và `if-else` thời gian phức tạp trong Worker.cs.*

- [x] **3.1. Tích hợp Quartz.NET**
    - [x] Cài đặt NuGet: `Quartz.Extensions.Hosting`.
    - [x] Cấu hình Quartz trong `Program.cs` để hỗ trợ Dependency Injection cho các Job.
- [x] **3.2. Định nghĩa danh mục Job** (Mỗi job là một class riêng):
    - [x] `AnalysisRealtimeJob`
    - [x] `NewsCrawlerJob`
    - [x] `EODStatsJob`
    - [x] `MorningSetupJob`
    - [x] `EPSRankJob`
    - [x] `BaoCaoPhanTichJob`
    - [x] `F319ScoutJob`
    - [x] `PortfolioJob`
    - [x] `TraceGiaJob`
    - [x] `TongCucThongKeJob`
    - [x] `TuDoanhJob`
    - [x] `ChartStatsJob`
    - [x] `Chart4UJob`
    - [x] `ForexMorningJob`
- [x] **3.3. Chuyển đổi Worker.cs**
    - [x] Di chuyển logic từ các phương thức `Process...` trong `Worker.cs` sang các Class Job tương ứng.
    - [x] Xóa bỏ hoàn toàn vòng lặp thời gian thủ công.

### Giai đoạn 4: Giám sát và Kiểm thử (Monitoring & Tests)
*Mục tiêu: Đảm bảo hệ thống chạy đúng mục đích và dễ dàng debug.*

- [x] **4.1. Structured Logging**
    - [x] Tích hợp Serilog (NuGet: `Serilog.AspNetCore`, `Serilog.Sinks.File`).
    - [x] Cấu hình ghi log ra file JSON (`Logs/log-*.txt`) để phục vụ phân tích.
    - [x] Tách biệt log level (Microsoft -> Warning, Quartz -> Information).
- [ ] **4.2. Kiểm thử Windows Service**
    - [ ] Kiểm tra khả năng tự khởi động lại (Self-healing) của Service khi gặp lỗi nghiêm trọng.
    - [ ] Kiểm tra việc giải phóng bộ nhớ (Memory management) của các Job chạy định kỳ.

---

## 📝 Nhật ký Cập nhật (Audit Log)
| Ngày | Chức năng | Trạng thái | Ghi chú |
| :--- | :--- | :--- | :--- |
| 01/02/2026 | Khởi tạo tài liệu kế hoạch | ✅ Hoàn thành | Thiết lập lộ trình nâng cấp |
| 01/02/2026 | Giai đoạn 1.1: Loại bỏ StaticVal | ✅ Hoàn thành | Đã chuyển session sang Service-based |
| 01/02/2026 | Giai đoạn 1.2: Tích hợp Resilience | ✅ Hoàn thành | Đã tích hợp Polly Retry & Circuit Breaker |
| 01/02/2026 | Giai đoạn 2.1: Tách Scraper Service | ✅ Hoàn thành | Đã tách logic scraping báo cáo/tin tức ra service riêng |
| 2025-02-01 | Giai đoạn 2.4: Tách Macro Data Service | ✅ Hoàn thành | Đã tách logic dữ liệu vĩ mô và hàng hóa |
| 2025-02-01 | Giai đoạn 2.3: Tách Vietstock Service | ✅ Hoàn thành | Đã tách logic báo cáo tài chính từ Vietstock |
| 01/02/2026 | Giai đoạn 2.2: Tách Market Data Service | ✅ Hoàn thành | Đã tách logic lấy dữ liệu thị trường ra service riêng |
| 01/02/2026 | Giai đoạn 3: Hiện đại hóa Tác vụ | ✅ Hoàn thành | Đã chuyển đổi hoàn toàn sang Quartz.NET |
| 01/02/2026 | Giai đoạn 4.1: Structured Logging | ✅ Hoàn thành | Tích hợp Serilog ghi log JSON |

---
*Lưu ý: Bạn có thể yêu cầu tôi thực hiện bất kỳ mục nào trong checklist này bằng cách nêu tên Step hoặc Giai đoạn.*
