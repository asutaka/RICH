# Research Folder

## Mục Đích

Folder này chứa các service dùng cho **research** và **backtesting** các chiến lược trading. Code trong folder này **KHÔNG** được sử dụng trong production Worker, chỉ dùng để test và phát triển các chỉ báo kỹ thuật mới.

---

## Cấu Trúc

```
Research/
├── BacktestService.cs    # Service chính cho backtesting
└── Models/               # Models cho research (nếu cần)
```

---

## BacktestService

Service này cung cấp các methods để test các chiến lược trading:

### Methods

| Method | Mô Tả |
|--------|-------|
| `BatDayCK()` | Test chiến lược bắt đáy với Bollinger Bands và volume |
| `CheckAllDay_OnlyVolume()` | Test patterns dựa trên volume |
| `CheckGDNN()` | Test chiến lược theo GDNN (foreign investors) |
| `CheckCungCau()` | Test supply/demand patterns |
| `CheckCrossMa50_BB()` | Test MA50 cross với Bollinger Bands |
| `CheckWycKoff()` | Test Wyckoff patterns |

---

## Cách Sử Dụng

### Option 1: Inject vào Worker (Temporary Testing)

Nếu muốn test trong Worker, uncomment code trong `Worker.cs`:

```csharp
// Trong Worker constructor
private readonly IBacktestService _backtestService;

public Worker(..., IBacktestService backtestService)
{
    _backtestService = backtestService;
}

// Trong ExecuteAsync
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // Test một lần khi start
    await _backtestService.BatDayCK();
    
    // Hoặc chạy theo schedule
    // ...
}
```

### Option 2: Tạo Console App Riêng (Khuyến nghị)

Tạo một console app riêng để chạy backtests:

```csharp
// Program.cs
var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices((context, services) =>
{
    services.ServiceDependencies();
    services.DALDependencies(context.Configuration);
});

var host = builder.Build();
var backtestService = host.Services.GetRequiredService<IBacktestService>();

// Chạy backtest
await backtestService.BatDayCK();
```

### Option 3: Unit Test

Tạo unit test để chạy backtests:

```csharp
[Fact]
public async Task Test_BatDayCK_Strategy()
{
    // Arrange
    var backtestService = CreateBacktestService();
    
    // Act
    await backtestService.BatDayCK();
    
    // Assert
    // Verify results
}
```

---

## Lưu Ý

- ⚠️ **KHÔNG** chạy backtests trong production Worker
- ⚠️ Backtests có thể mất nhiều thời gian (vài phút đến vài giờ)
- ⚠️ Cần VietStock API token hợp lệ
- ✅ Kết quả được in ra Console
- ✅ Service đã được đăng ký trong DI container

---

## Kết Quả

Kết quả backtests thường được in ra console theo format:

```
{Symbol}|BUY: {Date}|SELL: {Date}|Rate: {Profit}%
Total(100)| T3(80): 120%| T5(75): 150%| T10(70): 180%| Signal(85): 200%|End: 15
```

Trong đó:
- **T3**: Profit sau 3 ngày
- **T5**: Profit sau 5 ngày  
- **T10**: Profit sau 10 ngày
- **Signal**: Profit theo signal thoát lệnh
- **End**: Số lệnh giữ đến hết 10 ngày

---

## Development

Khi thêm chiến lược mới:

1. Thêm method vào `IBacktestService` interface
2. Implement method trong `BacktestService` class
3. Test bằng console app hoặc unit test
4. Document kết quả và tham số tối ưu
