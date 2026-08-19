# سامانه کوتاه‌کننده لینک — شهرداری سبزوار

بک‌اند و فرانت جدا اجرا می‌شوند.

| سرویس | دامنه | پورت پروژه | پورت دامنه (nginx) |
|---|---|---|---|
| بک‌اند API | `apiweb-shortlink.sabzevar.ir` | `5013` | `5015` HTTPS |
| فرانت + ریدایرکت | `sbzl.ir` | `5014` | `5016` HTTPS |
| ShortLinkBridge | داخلی | `5017` | — |

پورت `5015` مال nginx است، نه خودِ فرانت.

## ویژگی‌ها

- ایجاد لینک کوتاه (کد خودکار base62 یا کد دلخواه ۳ تا ۳۲ کاراکتری)
- ریدایرکت سریع `/{code}` به آدرس اصلی
- **گروه/قالب UTM**: `/{group}/{code}` → افزودن خودکار پارامترهای `utm_*` به URL مقصد
  - مثال: `sbzl.ir/u1/12345` → `https://map.sabzevar.ir/A/B/C?Q=1&W=2&utm_source=WWW`
  - مثال: `sbzl.ir/u2/12345` → `...&utm_source=BILBORD`
  - مثال: `sbzl.ir/utm/154dA` → افزودن مجموعه پارامترهای قالب `utm`
- مدیریت انقضا (در صورت وجود `expiresAt`، لینک منقضی با HTTP 410 پاسخ می‌دهد)
- ثبت آمار کلیک (تعداد، زمان، IP، نوع دستگاه، مرورگر، قالب UTM) به‌صورت **غیرهمزمان** بدون کندی در Redirect
- کش سریع با **بازگذاری/حذف هوشمند** هنگام ویرایش و حذف لینک یا گروه
- خطاهای ساخت‌یافته (ProblemDetails): لینک نامعتبر، پیدا نشد، منقضی، خطای دیتابیس، خطای داخلی
- فرانت ساده و اداری فارسی + صفحات «لینک یافت نشد» و «لینک در دسترس نیست» با لینک به [sabzevar.ir](https://sabzevar.ir)

## معماری

```
apiweb-shortlink:5013                 sbzl.ir:5014
POST /api/links ──► SQL               GET /{code} ──► Cache/DB ──► 302
                                      آمار کلیک غیرهمزمان ──► SQL
```

نکات طراحی برای نیازمندی «سرعت بالای خواندن»:

| نیاز | راه‌حل |
|---|---|
| خواندن بسیار سریع بر اساس کلید | کش `sl:link:{code}` و `sl:group:{name}` (Redis در تولید، InMemory در حالت پیش‌فرض) + ایندکس یکتای `Code` در دیتابیس |
| مقیاس‌پذیری افقی | سرویس بی‌حالت (Stateless)؛ چند نمونه به‌صورت هم‌زمان اجرا می‌شوند و کش/دیتابیس مشترک‌اند |
| تأخیر پایین در Redirect | مسیر ریدایرکت فقط از کش می‌خواند؛ در بدترین حالت یک کوئری ایندکس‌دار انجام می‌شود. آمار اصلاً در مسیر ریدایرکت نیست |
| پایداری بالا | SQL Server به‌عنوان منبع حقیقت (Source of truth)؛ کش صرفاً لایه خواندن است |
| آمار بدون کندی | Channel داخل فرآیند + BackgroundWorker با نوشتن بچ‌ای (batch insert) و به‌روزرسانی شمارنده‌ی تجمعی هر لینک با یک UPDATE |

> **تغییر به Redis**: کافی است `ConnectionStrings:Redis` را در `appsettings.json` مقداردهی کنید؛ سرویس به‌صورت خودکار از `AddStackExchangeRedisCache` استفاده می‌کند. بدون مقداردهی، از کش حافظه (تک‌نمونه) استفاده می‌شود.
>
> **مقیاس‌پذیری آمار در تولید**: برای چند نمونه، صف داخل فرآیند باید با یک Message Broker (مثلاً RabbitMQ/Kafka) جایگزین شود تا همه نمونه‌ها یک صف مشترک داشته باشند. واسط `ClickStatsQueue` طوری طراحی شده که این کار آسان است.

### معنی مسیرها

| مسیر | رفتار |
|---|---|
| `/{code}` | ریدایرکت به URL ذخیره‌شده (querystring فراخواننده ادغام می‌شود) |
| `/{group}/{code}` | خواندن قالب گروه، ادغام پارامترهای `utm_*` در URL مقصد و ریدایرکت |
| `/{code}?utm_source=...` | پارامترهای فراخواننده **بر** پارامترهای گروه اولویت دارند |

قانون ادغام: مقدار URL اصلی < مقدار قالب گروه < مقدار querystring فراخواننده.

## تکنولوژی‌ها

- .NET 8 (ASP.NET Core Minimal API)
- EF Core 8 + SQL Server 2022+ (Migration خودکار در استارت‌آپ)
- IDistributedCache (Redis در تولید / InMemory در توسعه)
- xUnit برای تست‌ها

## اجرای سریع

پیش‌نیاز: .NET SDK 8، SQL Server (ترجیحاً روی `localhost`).

```bash
cd short-links
dotnet restore
dotnet run --project src/ShortLinks.Api --launch-profile Api
dotnet run --project src/ShortLinks.Api --launch-profile Web
```

- API: `http://localhost:5013`
- فرانت: `http://localhost:5014`

تست‌ها:

```bash
dotnet test
```

## تنظیمات (`appsettings.json`)

| کلید | توضیح |
|---|---|
| `ConnectionStrings:SqlServer` | رشته اتصال دیتابیس |
| `ConnectionStrings:Redis` | خالی = کش حافظه؛ مقداردهی = Redis |
| `Hosting:Role` | `Api` بک‌اند، `Web` فرانت/ریدایرکت، `All` هر دو |
| `Public:BaseUrl` | دامنه عمومی لینک کوتاه (`https://sbzl.ir`) |
| `Cache:InstanceName` | پیشوند کلیدهای کش |
| `Cache:TtlMinutes` | عمر کش (پیش‌فرض ۱۴۴۰ = ۲۴ ساعت) |
| `Migrate:OnStartup` | اعمال خودکار Migration در استارت‌آپ |

## سناریوی نمونه (مطابق طرح)

```bash
# ۱) ساخت گروه‌ها (قالب‌های UTM)
curl -X POST http://localhost:5013/api/groups -H "Content-Type: application/json" -d '{"name":"u1","utmParams":{"utm_source":"WWW"}}'
curl -X POST http://localhost:5013/api/groups -H "Content-Type: application/json" -d '{"name":"u2","utmParams":{"utm_source":"BILBORD"}}'
curl -X POST http://localhost:5013/api/groups -H "Content-Type: application/json" -d '{"name":"utm","utmParams":{"utm_source":"SEO","utm_medium":"banner","utm_campaign":"sabzevar"}}'

# ۲) ایجاد لینک کوتاه
curl -X POST http://localhost:5013/api/links -H "Content-Type: application/json" \
     -d '{"url":"https://map.sabzevar.ir/A/B/C?Q=1&W=2"}'
# → { "code": "i5SXNS", "shortUrl": "https://sbzl.ir/i5SXNS", ... }

# ۳) ریدایرکت‌ها (فرانت)
curl -I http://localhost:5014/i5SXNS
curl -I http://localhost:5014/u1/i5SXNS

# ۴) آمار (بک‌اند)
curl http://localhost:5013/api/links/i5SXNS/stats/summary
```

## مستندات بیشتر

- [راهنمای API](docs/API.md)
- [معماری و تصمیمات فنی](docs/Architecture.md)
- [نصب و استقرار](docs/Deployment.md)

---
© ۱۴۰۵ شهرداری سبزوار