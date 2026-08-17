# راهنمای استقرار روی AlmaLinux 10 / RHEL 10

این راهنما کامل و دقیق است. مراحل به‌ترتیب اجرا کنید.

> پیش‌فرض‌ها: پورت سرویس `5013`، دیتابیس SQL Server روی `185.255.91.242,2019`،
> دایرکتوری برنامه `/var/www/shortlinks`.

---

## ۰) پیش‌نیازها

- سرور AlmaLinux 10 با دسترسی `root` یا کاربری با `sudo`
- دسترسی اینترنت برای دانلود بسته‌های .NET و کد از گیت‌هاب
- دیتابیس و کاربر دیتابیس (اطلاعات اتصال را از قبل دارید)

---

## ۱) اتصال به سرور و به‌روزرسانی

```bash
ssh root@SERVER_IP
dnf update -y
```

---

## ۲) نصب .NET 8 (Runtime + SDK)

```bash
rpm -Uvh https://packages.microsoft.com/config/rhel/10/packages-microsoft-prod.rpm
dnf install -y aspnetcore-runtime-8.0 dotnet-sdk-8.0

dotnet --version   # انتظار: 8.x
```

---

## ۳) ساخت دیتابیس (فقط اگر ساخته نشده باشد)

اگر دیتابیس `apiweb-shortlink` هنوز وجود ندارد، با یک کاربر دارای مجوز
(مثلاً `sa`) از هر سیستم دارای `sqlcmd` این‌طور بسازید:

```bash
sqlcmd -S "185.255.91.242,2019" -U sa -P 'YOUR_SA_PASSWORD' \
  -Q "CREATE DATABASE [apiweb-shortlink];"
```

سپس جداول را با اسکریپت `schema.sql` (همراه پروژه) بسازید — یا ساده‌تر،
اجازه دهید سامانه هنگام استارت‌آپ خودش Migration را اجرا کند
(`Migrate:OnStartup=true`). در حالت دوم کاربر `apiwebshortlinkuser`
باید حق ساخت جدول (DDL) روی این دیتابیس را داشته باشد:

```sql
USE [apiweb-shortlink];
ALTER ROLE [db_owner] ADD MEMBER [apiwebshortlinkuser];
```

> اگر کاربر دسترسی ساخت جدول نداشته باشد، سامانه در استارت‌آپ خطا می‌دهد؛
> در آن صورت `schema.sql` را با حساب دارای مجوز اجرا کرده و
> `Migrate:OnStartup=false` کنید.

---

## ۴) دریافت سورس و انتشار پروژه

```bash
cd /tmp
rm -rf short-links-src
git clone https://github.com/amirreza-fnt/short-links.git short-links-src

mkdir -p /var/www/shortlinks
dotnet publish /tmp/short-links-src/src/ShortLinks.Api/ShortLinks.Api.csproj \
  -c Release --self-contained false -o /var/www/shortlinks
```

---

## ۵) ساخت فایل تنظیمات Production (با رمز دیتابیس)

رمز دیتابیس را **در فایل روی سرور** قرار می‌دهیم (نه در گیت).

```bash
cat > /var/www/shortlinks/appsettings.Production.json <<'EOF'
{
  "ConnectionStrings": {
    "SqlServer": "Server=185.255.91.242,2019;Database=apiweb-shortlink;User Id=apiwebshortlinkuser;Password=5DgR$ep)**G;TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true",
    "Redis": ""
  },
  "Public": {
    "BaseUrl": "http://185.255.91.242:5013"
  },
  "Migrate": {
    "OnStartup": true
  }
}
EOF

chmod 600 /var/www/shortlinks/appsettings.Production.json
```

> `<<'EOF'` (کوتیشن تکی) باعث می‌شود رمز `5DgR$ep)**G` بدون تغییر بنشیند.
> اگر در فایل جای دیگری از `$` استفاده می‌کنید، به همین نکته دقت کنید.

---

## ۶) کاربر اختصاصی سرویس

```bash
useradd --system --home /var/www/shortlinks --shell /sbin/nologin shortlinks
chown -R shortlinks:shortlinks /var/www/shortlinks
```

---

## ۷) نصب سرویس systemd

```bash
cp /tmp/short-links-src/deploy/shortlinks.service /etc/systemd/system/shortlinks.service
systemctl daemon-reload
systemctl enable shortlinks.service
systemctl restart shortlinks.service
```

سرویس به‌صورت خودکار روی پورت `5013` و همه‌ی رابط‌ها اجرا می‌شود
(تنظیم در `appsettings.json` → `Kestrel:Endpoints:Http:Url`).

---

## ۸) فایروال و امنیت

```bash
# فقط اگر firewalld فعال است:
firewall-cmd --permanent --add-port=5013/tcp
firewall-cmd --reload

# بررسی SELinux (در صورت enforcing، اگر خطای bind داشتید):
setsebool -P httpd_can_network_connect 1
```

> اگر سرویس پشت Nginx/Reverse-Proxy قرار می‌گیرد، پورت 5013 را در فایروال
> باز نگذارید و فقط پورت 80/443 را باز کنید.

---

## ۹) بررسی نهایی

```bash
systemctl status shortlinks
curl http://localhost:5013/health          # → {"status":"healthy",...}
curl -I http://localhost:5013/somecode     # → 302 (اگر کد موجود باشد)
journalctl -u shortlinks -f                # مشاهده لاگ زنده
```

---

## ۱۰) به‌روزرسانی نسخه جدید

```bash
cd /tmp/short-links-src && git pull
dotnet publish src/ShortLinks.Api/ShortLinks.Api.csproj -c Release --self-contained false -o /var/www/shortlinks
# فایل appsettings.Production.json دست‌نخورده می‌ماند؟ خیر — پس از publish آن را دوباره بسازید
systemctl restart shortlinks
```

> بعد از هر `dotnet publish`، فایل `appsettings.Production.json` بازنویسی می‌شود؛
> پس از publish، دوباره بخش ۵ را اجرا کنید یا از اسکریپت خودکار استفاده کنید.

---

## ۱۱) راه‌اندازی کاملاً خودکار (یک‌دستوری)

```bash
sudo DB_PASSWORD='5DgR$ep)**G' bash /tmp/short-links-src/deploy/setup-almalinux10.sh
```

اسکریپت مراحل ۲ تا ۹ را خودکار انجام می‌دهد.

---

## عیب‌یابی سریع

| مشکل | راه‌حل |
|---|---|
| خطای اتصال دیتابیس در استارت | فایل `appsettings.Production.json` را بررسی کنید؛ دیتابیس و دسترسی کاربر را چک کنید |
| پورت باز نیست | فایروال (firewall-cmd) و Kestrel URL را بررسی کنید |
| خطای SELinux | `setsebool -P httpd_can_network_connect 1` |
| لینک‌های کوتاه در خروجی دامنه غلط دارند | `Public:BaseUrl` را در `appsettings.Production.json` به دامنه/آدرس عمومی تغییر دهید |
