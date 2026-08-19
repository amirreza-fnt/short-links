# استقرار و نصب

## پیش‌نیازها

- .NET 8 SDK / Runtime (روی سرور)
- SQL Server 2019+ (دیتابیس به‌صورت خودکار با Migration ساخته می‌شود)
- (اختیاری) Redis 7 برای کش اشتراکی بین چند نمونه

## اجرای محلی / توسعه

```bash
dotnet restore
dotnet run --project src/ShortLinks.Api
```

- API: `http://localhost:5013` (`--launch-profile Api`)
- فرانت: `http://localhost:5014` (`--launch-profile Web`)
- در اولین اجرا دیتابیس ساخته و Migration اعمال می‌شود (`Migrate:OnStartup`)

## اجرای چند نمونه (مقیاس افقی)

1. `ConnectionStrings:Redis` را با آدرس Redis پر کنید تا همه نمونه‌ها کش مشترک داشته باشند.
2. `Public:BaseUrl` را به دامنه عمومی (مثلاً `https://sbzl.ir`) تنظیم کنید.
3. چند نمونه را پشت Load Balancer (مثلاً Nginx/IIS ARR/HAProxy) اجرا کنید. سرویس بی‌حالت است؛ فقط دیتابیس و کش مشترک باشند.
4. برای آمار مشترک بین نمونه‌ها، `ClickStatsQueue` را با RabbitMQ/Kafka جایگزین کنید (نقطه‌ی اتصال در `Program.cs`).

> نکته: در حالت پیش‌فرض (بدون Redis) کش داخل هر فرآیند است؛ برای محیط چند نمونه‌ای Redis الزامی است.

## انتشار (Publish)

```bash
dotnet publish src/ShortLinks.Api -c Release -o publish
```

## اجرا به‌صورت سرویس ویندوز (sc.exe)

```powershell
sc.exe create "ShortLinksApi" binPath= "C:\path\to\publish\ShortLinks.Api.exe" start= auto
sc.exe start "ShortLinksApi"
```

(برای وب‌ها، سرویس میزبانی ASP.NET Core با `Microsoft.NET.Sdk.Web` به‌صورت خودکار ساپورت می‌شود.)

## اجرا با IIS

- از ماژول ASP.NET Core Hosting Module استفاده کنید.
- `appsettings.json` را طوری تنظیم کنید که پورت/پروتکل روی Kestrel با تنظیمات IIS هماهنگ باشد؛ در حالت Reverse-Proxy معمولاً تنظیم `Kestrel` حذف و پورت از IIS داده می‌شود.

## دیتابیس

Migrationها در `src/ShortLinks.Api/Migrations` هستند و در استارت‌آپ به‌صورت خودکار اعمال می‌شوند. برای اعمال دستی:

```bash
dotnet ef database update --project src/ShortLinks.Api
```

برای ساخت دستی دیتابیس و جداول (بدون EF):

```powershell
sqlcmd -S localhost -E -C -i deploy/schema.sql
```

## پشتیبان‌گیری

- SQL Server: backup معمولی دیتابیس `ShortLinks`.
- کش (Redis): با حذف کش خطری پیش نمی‌آید؛ دیتابیس منبع حقیقت است.

## امنیت

- مسیرهای مدیریتی `/api/*` فعلاً بدون احراز هویت هستند. پیش از قرارگیری در محیط عمومی، درخواست‌های `/api/*` را با API Gateway/فایروال محدود کنید (مثلاً فقط از IP پنل مدیریت).
- `Public:BaseUrl` باید به‌درستی تنظیم شود تا لینک‌های ساخته‌شده دامنه صحیح داشته باشند.
