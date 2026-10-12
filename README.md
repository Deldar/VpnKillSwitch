# VPN Kill Switch (Windows)

[English](#english) | [فارسی](#فارسی)

---

<a name="english"></a>
## English

A lightweight Windows system tray utility that prevents IP and traffic leaks by immediately blocking outbound network traffic for selected applications whenever your VPN connection drops or becomes unconfirmed.

### Why this exists

Modern AI development tools (such as Claude Desktop, Cursor, Google Antigravity, and VS Code extensions) and developer utilities often communicate continuously in the background. If your VPN connection drops, your computer may silently fall back to your direct internet connection, exposing your real IP address to remote APIs.

VPN Kill Switch monitors your VPN tunnel using multiple local and remote signals. If the tunnel drops, it applies outbound block rules to your protected applications within milliseconds via Windows Defender Firewall. When the tunnel reconnects, it detects the recovery automatically and restores access within seconds.

### Key Features

- Instant leak protection: Applies outbound block rules to protected applications the moment a tunnel drop is detected.
- Fast auto-recovery: Automatically detects when the VPN reconnects and removes firewall blocks within seconds, without requiring manual button clicks.
- Built-in IDE and tool detection: Discovers installed development tools automatically (Google Antigravity, Cursor, VS Code, Zed, Windsurf, Claude Desktop, Claude Code CLI, JetBrains IDEs, ChatGPT, Ollama, LM Studio, Postman, Docker Desktop, etc.).
- Version-aware path tracking: Handles self-updating applications and Windows Store (MSIX) packages that change install directory paths across updates.
- Multi-signal tunnel health monitoring:
  - Local proxy port checks (e.g. 127.0.0.1:7890).
  - VPN client process monitoring (OpenConnect, WireGuard, v2ray, Clash, Sing-box, Hiddify, etc.).
  - Virtual network adapter status checks (wintun, tap, wireguard).
  - External egress IP verification (verifying that traffic exits via the VPN endpoint rather than your physical ISP).
  - Instant reaction to Windows network-change events without waiting for poll intervals.
- Flexible alert preferences: Choose between Windows notifications, distinct synthesized audio tones (descending alert on disconnect, ascending chime on reconnect), both, or silent mode.
- Non-destructive firewall management: Operates exclusively within the `VpnKillSwitch.Managed` group and never modifies, overwrites, or removes your own custom firewall rules.
- System tray integration: Color-coded tray icon showing live connection health and protection state.

### Requirements

- Windows 10 (version 1809 or newer) or Windows 11
- .NET 10.0 Runtime or SDK
- Administrator privileges (required by Windows to create and remove firewall rules)

### Build and Run

1. Clone the repository:
   ```cmd
   git clone https://github.com/your-username/VpnKillSwitch.git
   cd VpnKillSwitch
   ```

2. Build the project:
   ```cmd
   dotnet build -c Release
   ```

3. Run as Administrator:
   ```cmd
   bin\Release\net10.0-windows\VpnKillSwitch.exe
   ```

### Basic Usage

1. Open Applications: Review your protected applications list. Click "Detect installed" to automatically discover your installed IDEs and AI tools, or drag and drop executable files directly into the window.
2. Open Tunnel: With your VPN connected, click "Detect automatically" to detect listening proxy ports, running VPN processes, and virtual network adapters.
3. Open Dashboard: Turn on the protection toggle. The status cards display live tunnel health.
4. Emergency cleanup: Click "Clear firewall rules" in the sidebar or tray menu at any time to remove all rules created by the application.

### Start with Windows

To run the application on startup:
1. Create a shortcut to `VpnKillSwitch.exe`.
2. Right-click the shortcut, select Properties, click Advanced, and check "Run as administrator".
3. Move the shortcut to:
   ```
   %APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
   ```

### License

This project is licensed under the MIT License. See the LICENSE file for details.

---

<a name="فارسی"></a>
## فارسی

یک برنامه سبک برای محیط ویندوز و سیستم‌تری (System Tray) که با قطع شدن یا افت کیفیت اتصال وی‌پی‌ان، ترافیک خروجی برنامه‌های انتخاب‌شده را از طریق فایروال ویندوز بلاک می‌کند تا آی‌پی واقعی شما نشت نکند.

### چرا این برنامه ساخته شد؟

ابزارهای توسعه مبتنی بر هوش مصنوعی (مانند کلاد دسکتاپ، کرسر، گوگل آنتی‌گرویتی و اکستنشن‌های وی‌اس‌کد) و سایر ابزارهای توسعه‌دهندگان همواره در پس‌زمینه با سرورهای خارجی در ارتباط هستند. هنگام قطع ناگهانی وی‌پی‌ان، ویندوز ترافیک را بلافاصله از طریق اتصال مستقیم اینترنت ارسال می‌کند که منجر به لو رفتن آی‌پی واقعی و مسدود شدن دسترسی به سرویس‌ها می‌شود.

این برنامه وضعیت تونل وی‌پی‌ان را به صورت لحظه‌ای و از طریق چندین سیگنال سخت‌افزاری و نرم‌افزاری بررسی می‌کند. در صورت قطعی، قوانین بلاک فایروال ویندوز ظرف چند میلی‌ثانیه اعمال می‌شوند. پس از اتصال مجدد وی‌پی‌ان نیز برنامه به طور خودکار ارتباط را تشخیص داده و دسترسی اینترنت برنامه‌ها را ظرف یک تا دو ثانیه بازمی‌گرداند.

### ویژگی‌های کلیدی

- حفاظت آنی در برابر نشت آی‌پی: مسدودسازی بلافاصله ترافیک خروجی برنامه‌های محافظت‌شده به محض بروز قطعی.
- بازگشت خودکار و سریع: تشخیص خودکار برقراری مجدد وی‌پی‌ان و باز کردن قفل برنامه‌ها ظرف ۱ الی ۲ ثانیه بدون نیاز به کلیک دستی.
- تشخیص خودکار محیط‌های توسعه و ابزارهای هوش مصنوعی: شناسایی خودکار برنامه‌های نصب‌شده روی سیستم (گوگل آنتی‌گرویتی، کرسر، وی‌اس‌کد، زد، ویندسرف، کلاد، جت‌برینز، چت‌جی‌پی‌تی، اولاما، ال‌ام استودیو، پست‌من، داکر و...).
- پشتیبانی از مسیرهای متغیر و برنامه‌های استور: سازگاری کامل با برنامه‌هایی که با هر آپدیت مسیر پوشه آن‌ها تغییر می‌کند (نظیر بسته‌های MSIX استور ویندوز).
- بررسی سلامت تونل از طریق چند سیگنال همزمان:
  - پورت محلی پراکسی (مانند 127.0.0.1:7890).
  - مانیتورینگ پروسه‌های وی‌پی‌ان (اوپن‌کانکت، وایرگارد، وی‌تو‌ری، کلش، سینگ‌باکس، هیدیفای و...).
  - وضعیت آداپتورهای مجازی شبکه (wintun و tap).
  - بررسی هویت آی‌پی خروجی جهت اطمینان از خروج ترافیک از سرور وی‌پی‌ان.
  - واکنش لحظه‌ای به رویدادهای تغییر شبکه ویندوز بدون معطلی در تایمرها.
- هشدارهای صوتی و تصویری قابل تنظیم: انتخاب بین نوتیفیکیشن ویندوز، هشدارهای صوتی اختصاصی (صدای نزولی هنگام قطع شدن و چایم صعودی هنگام اتصال مجدد)، هر دو، یا حالت کاملاً بی‌صدا.
- مدیریت امن فایروال: برنامه تنها با رول‌های گروه اختصاصی خود کار دارد و هیچ تغییری در قوانین شخصی فایروال ویندوز ایجاد نمی‌کند.
- آیکون تسک‌بار با رنگ‌های وضعیتی: نمایش زنده وضعیت اتصال از طریق تغییر رنگ آیکون در کنار ساعت ویندوز.

### پیش‌نیازها

- ویندوز ۱۰ (نسخه ۱۸۰۹ به بعد) یا ویندوز ۱۱
- ران‌تایم یا کیت توسعه .NET 10.0
- دسترسی مدیر سیستم (Run as administrator) برای ثبت قوانین در فایروال ویندوز

### نحوه بیلد و اجرا

۱. دریافت سورس کد:
```cmd
git clone https://github.com/your-username/VpnKillSwitch.git
cd VpnKillSwitch
```

۲. کامپایل پروژه:
```cmd
dotnet build -c Release
```

۳. اجرا به عنوان Administrator:
```cmd
bin\Release\net10.0-windows\VpnKillSwitch.exe
```

### راهنمای استفاده

۱. بخش Applications: لیست برنامه‌های محافظت‌شده را بازبینی کنید. با زدن دکمه "Detect installed" ابزارهای توسعه نصب‌شده به صورت خودکار شناسایی و اضافه می‌شوند. همچنین می‌توانید فایل‌های exe را مستقیماً به داخل پنجره بکشید و رها کنید.
۲. بخش Tunnel: در حالی که وی‌پی‌ان شما وصل است، دکمه "Detect automatically" را بزنید تا پورت، پروسه و آداپتور شبکه به صورت خودکار شناسایی شوند.
۳. بخش Dashboard: کلید بزرگ مرکزی را بزنید تا محافظت فعال شود. کارت‌های وضعیت سلامت تونل را نمایش می‌دهند.
۴. پاک‌سازی اضطراری: در صورت نیاز می‌توانید در هر زمان با زدن گزینه "Clear firewall rules" در منوی کناری یا منوی تسک‌بار، تمام قوانین ثبت‌شده توسط برنامه را از فایروال پاک کنید.

### اجرای خودکار با ویندوز

برای اجرای خودکار برنامه هنگام روشن شدن سیستم:
۱. از فایل `VpnKillSwitch.exe` یک میانبر (Shortcut) بسازید.
۲. روی میانبر راست‌کلیک کرده، گزینه Properties را انتخاب کنید، روی دکمه Advanced کلیک کرده و تیک "Run as administrator" را بزنید.
۳. فایل میانبر را به این مسیر منتقل کنید:
```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
```

### مجوز

این پروژه تحت مجوز MIT منتشر شده است. اطلاعات بیشتر در فایل LICENSE قرار دارد.
