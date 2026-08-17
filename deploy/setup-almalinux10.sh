#!/usr/bin/env bash
#
# راه‌انداز خودکار سامانه کوتاه‌کننده لینک روی AlmaLinux 10 / RHEL 10
# ---------------------------------------------------------------
# نحوه استفاده (با رمز دیتابیس در متغیر محیطی — رمز در هیچ فایلی ذخیره نمی‌شود):
#
#   sudo DB_PASSWORD='5DgR$ep)**G' bash deploy/setup-almalinux10.sh
#
# نکته: رمز را داخل 'کوتیشن تکی' بگذارید تا $ یا * تغییری نکند.
# اگر دیتابیس هنوز ساخته نشده است، ابتدا بخش «ساخت دیتابیس» در پایین همین فایل
# (یا فایل deploy/README-Deployment.md) را ببینید.
#
set -euo pipefail

APP_NAME="shortlinks"
APP_DIR="/var/www/${APP_NAME}"
REPO_URL="https://github.com/amirreza-fnt/short-links.git"
PORT=5013

DB_SERVER="185.255.91.242,2019"
DB_NAME="apiweb-shortlink"
DB_USER="apiwebshortlinkuser"
# DB_PASSWORD را از متغیر محیطی بگیر (اجباری):
DB_PASSWORD="${DB_PASSWORD:-}"

if [ -z "${DB_PASSWORD}" ]; then
  echo "ERROR: متغیر محیطی DB_PASSWORD تنظیم نشده است."
  echo "مثال: sudo DB_PASSWORD='رمز-دیتابیس' bash $0"
  exit 1
fi

echo "==> [1/8] به‌روزرسانی مخازن"
dnf update -y

echo "==> [2/8] نصب .NET 8 Runtime + SDK"
if ! command -v dotnet &>/dev/null; then
  rpm -Uvh https://packages.microsoft.com/config/rhel/10/packages-microsoft-prod.rpm
  dnf install -y aspnetcore-runtime-8.0 dotnet-sdk-8.0
fi
dotnet --version

echo "==> [3/8] دریافت سورس پروژه"
rm -rf /tmp/short-links-src
git clone --depth 1 "${REPO_URL}" /tmp/short-links-src

echo "==> [4/8] انتشار (Publish) پروژه در ${APP_DIR}"
rm -rf "${APP_DIR}"
mkdir -p "${APP_DIR}"
dotnet publish /tmp/short-links-src/src/ShortLinks.Api/ShortLinks.Api.csproj \
  -c Release \
  --self-contained false \
  -o "${APP_DIR}"

echo "==> [5/8] ساخت فایل تنظیمات محیط Production (شامل رمز دیتابیس)"
# رمز به‌صورت مستقیم و امن داخل فایل می‌نشیند؛ دسترسی فایل فقط برای سرویس.
cat > "${APP_DIR}/appsettings.Production.json" <<EOF
{
  "ConnectionStrings": {
    "SqlServer": "Server=${DB_SERVER};Database=${DB_NAME};User Id=${DB_USER};Password=${DB_PASSWORD};TrustServerCertificate=True;Encrypt=True;MultipleActiveResultSets=true",
    "Redis": ""
  },
  "Public": {
    "BaseUrl": "http://${DB_SERVER%%:*}:${PORT}"
  },
  "Migrate": {
    "OnStartup": true
  }
}
EOF

echo "==> [6/8] کاربر اختصاصی سرویس"
if ! id "${APP_NAME}" &>/dev/null; then
  useradd --system --home "${APP_DIR}" --shell /sbin/nologin "${APP_NAME}"
fi
chown -R "${APP_NAME}:${APP_NAME}" "${APP_DIR}"
chmod 600 "${APP_DIR}/appsettings.Production.json"

echo "==> [7/8] نصب سرویس systemd روی پورت ${PORT}"
cp /tmp/short-links-src/deploy/shortlinks.service /etc/systemd/system/shortlinks.service
systemctl daemon-reload
systemctl enable shortlinks.service

echo "==> [8/8] باز کردن پورت ${PORT} در فایروال (در صورت فعال بودن firewalld)"
if systemctl is-active --quiet firewalld; then
  firewall-cmd --permanent --add-port=${PORT}/tcp || true
  firewall-cmd --reload || true
fi

systemctl restart shortlinks.service

echo ""
echo "======================================================================"
echo " نصب کامل شد."
echo "   سرویس : systemctl status shortlinks"
echo "   لاگ   : journalctl -u shortlinks -f"
echo "   آدرس  : http://$(hostname -I | awk '{print $1}'):${PORT}"
echo "   سلامت : curl http://localhost:${PORT}/health"
echo ""
echo " اگر در استارت‌آپ خطای اتصال دیتابیس دیدید، دو حالت است:"
echo "  ۱) دیتابیس '${DB_NAME}' ساخته نشده یا کاربر دسترسی ندارد:"
echo "     (با حساب دارای مجوز روی SQL Server اجرا کنید)"
echo "     sqlcmd -S ${DB_SERVER} -U <admin> -P '***' -Q \"CREATE DATABASE [${DB_NAME}];\""
echo "     و سپس اسکریپت deploy/schema.sql را روی آن اجرا کنید."
echo "  ۲) رمز/آدرس نادرست است: فایل ${APP_DIR}/appsettings.Production.json را بررسی کنید."
echo "======================================================================"