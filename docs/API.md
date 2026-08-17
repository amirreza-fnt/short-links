# راهنمای API

همه درخواست‌ها/پاسخ‌ها JSON هستند (به جز صفحات ریدایرکت/خطا). خطاها مطابق RFC 7807 به شکل `application/problem+json` برمی‌گردند.

```
Content-Type: application/json
```

## لینک‌ها (`/api/links`)

### ایجاد لینک کوتاه

```
POST /api/links
```

بدنه:

```json
{
  "url": "https://map.sabzevar.ir/A/B/C?Q=1&W=2",   // الزامی
  "code": "mycode",                                  // اختیاری (3-32 حرف انگلیسی/رقم)
  "groupName": "u1",                                 // اختیاری
  "expiresAt": "2026-12-31T23:59:59+03:30",          // اختیاری (زمان انقضا)
  "isActive": true                                   // اختیاری (پیش‌فرض true)
}
```

پاسخ `201`:

```json
{
  "id": 1,
  "code": "i5SXNS",
  "shortUrl": "http://localhost:5000/i5SXNS",
  "targetUrl": "https://map.sabzevar.ir/A/B/C?Q=1&W=2",
  "groupName": null,
  "createdAt": "...",
  "updatedAt": "...",
  "expiresAt": null,
  "isActive": true,
  "clickCount": 0,
  "lastRedirectAt": null
}
```

خطاها: `400` (URL نامعتبر / کد نامعتبر)، `409` (کد تکراری)، `400` (گروه پیدا نشد).

### لیست لینک‌ها

```
GET /api/links?search=&groupName=&page=1&pageSize=20
```

پاسخ: `{ "items": [...], "totalCount": 0, "page": 1, "pageSize": 20 }`

### دریافت یک لینک

```
GET /api/links/{code}
```

### ویرایش لینک (بازگذاری کش به‌صورت خودکار)

```
PUT /api/links/{code}
```

بدنه (همه اختیاری):

```json
{
  "url": "https://new.example.com",
  "groupName": "utm",      // "" برای جدا کردن گروه
  "expiresAt": "2026-12-31T23:59:59+03:30",
  "isActive": true
}
```

### حذف لینک (حذف کش به‌صورت خودکار)

```
DELETE /api/links/{code}
```

پاسخ `204`.

## گروه‌ها / قالب‌های UTM (`/api/groups`)

### ایجاد گروه

```
POST /api/groups
```

```json
{
  "name": "u1",                       // فقط حروف/رقم/خط تیره/خط زیر
  "description": "وب‌سایت",
  "utmParams": { "utm_source": "WWW" }
}
```

`utmParams` هر جفت کلید/مقداری را در querystring مقصد اضافه می‌کند.

### فهرست گروه‌ها

```
GET /api/groups
```

### ویرایش گروه

```
PUT /api/groups/{name}
```

```json
{
  "description": "...",
  "utmParams": { "utm_source": "NEW", "utm_medium": "banner" },
  "isActive": true
}
```

### حذف گروه

```
DELETE /api/groups/{name}
```

اگر گروه به لینکی اختصاص داشته باشد پاسخ `409` می‌گیرید.

## آمار (`/api`)

| Endpoint | توضیح |
|---|---|
| `GET /api/links/{code}/stats/summary` | جمع کل، IP یکتا، بازه زمانی، تفکیک دستگاه/مرورگر/قالب |
| `GET /api/links/{code}/stats?page=1&pageSize=50&from=&to=` | ردیف‌های کلیک |
| `GET /api/links/{code}/stats/timeseries?bucket=day` | سری زمانی (`day`/`hour`/`week`/`month`) |
| `GET /api/stats/overview` | نمای کلی کل سیستم |

آمار به‌صورت غیرهمزمان نوشته می‌شود؛ تا چند ثانیه پس از کلیک ممکن است در گزارش دیده نشود.

## مسیرهای عمومی (Redirect / Frontend)

| مسیر | نتیجه |
|---|---|
| `GET /{code}` | `302` به آدرس مقصد |
| `GET /{group}/{code}` | `302` با ادغام پارامترهای قالب گروه |
| `GET /` | صفحه لندینگ (۲۰۰) |
| لینک پیدا نشد | `404` + صفحه فارسی «لینک یافت نشد» |
| لینک منقضی/غیرفعال | `410` + صفحه فارسی «لینک در دسترس نیست» |
| `GET /health` | وضعیت سلامت سرویس |

هر دو مسیر ریدایرکت از `GET` و `HEAD` پشتیبانی می‌کنند.

## ساختار خطا

```json
{
  "type": "AppValidationException",
  "title": "Bad Request",
  "status": 400,
  "detail": "URL must use http or https scheme.",
  "traceId": "..."
}
```

| کد | موارد |
|---|---|
| `400` | ورودی نامعتبر (URL/کد/گروه) |
| `404` | لینک یا گروه پیدا نشد |
| `409` | تداخل (کد تکراری، گروه در حال استفاده) |
| `410` | لینک منقضی یا غیرفعال (در مسیر ریدایرکت) |
| `500` | خطای داخلی / دیتابیس (با `traceId`) |
