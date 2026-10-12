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

**VpnKillSwitch** یک ابزار ویندوزی برای محافظت از اتصال اینترنت برنامه‌های انتخاب‌شده هنگام قطع شدن VPN است. برنامه در System Tray اجرا می‌شود و در صورت تشخیص قطعی یا اختلال در تونل VPN، ترافیک خروجی برنامه‌های تحت محافظت را از طریق Windows Firewall مسدود می‌کند.

هدف این است که برنامه‌های انتخاب‌شده هنگام قطع ناگهانی VPN، به اتصال مستقیم اینترنت برنگردند و از مسیر اینترنت معمولی به شبکه متصل نشوند.

### چرا VpnKillSwitch؟

ابزارهای توسعه، دستیارهای هوش مصنوعی و بسیاری از برنامه‌های روزمره در پس‌زمینه به سرورهای اینترنتی متصل‌اند. اگر VPN ناگهان قطع شود، ممکن است این ارتباط‌ها از طریق اتصال مستقیم سیستم ادامه پیدا کنند.

VpnKillSwitch برای جلوگیری از این اتفاق طراحی شده است. برنامه وضعیت اتصال را با استفاده از چند نشانه مختلف بررسی می‌کند و هنگام تشخیص قطعی، قوانین مسدودسازی فایروال را برای برنامه‌های انتخاب‌شده اعمال می‌کند. پس از شناسایی اتصال مجدد VPN، دسترسی آن‌ها به اینترنت به‌صورت خودکار برقرار می‌شود.

### ویژگی‌ها

- **محافظت در برابر نشت IP:** مسدود کردن ترافیک خروجی برنامه‌های تحت محافظت هنگام تشخیص قطعی VPN.
- **بازیابی خودکار اتصال:** بازگرداندن دسترسی اینترنت پس از شناسایی اتصال مجدد VPN، بدون نیاز به دخالت دستی.
- **شناسایی برنامه‌های پرکاربرد:** پشتیبانی از ابزارهای توسعه و هوش مصنوعی مانند Google Antigravity، Cursor، VS Code، Zed، Windsurf، Claude، JetBrains، ChatGPT، Ollama، LM Studio، Postman و Docker.
- **پشتیبانی از مسیرهای متغیر نصب:** سازگاری با برنامه‌هایی که مسیر نصب آن‌ها ممکن است پس از به‌روزرسانی تغییر کند، از جمله برخی برنامه‌های مبتنی بر MSIX و Microsoft Store.
- **بررسی وضعیت VPN با چند معیار:** استفاده از سیگنال‌های مختلف برای تشخیص وضعیت اتصال، از جمله:
  - دسترس‌پذیری پراکسی محلی، مانند `127.0.0.1:7890`
  - وضعیت پردازش‌های مرتبط با VPN، مانند OpenConnect، WireGuard، v2ray، Clash، sing-box و Hiddify
  - وضعیت آداپتورهای مجازی شبکه، مانند Wintun و TAP
  - بررسی IP خروجی برای ارزیابی مسیر اتصال
  - واکنش به رویدادهای تغییر شبکه در ویندوز
- **هشدارهای قابل تنظیم:** انتخاب اعلان ویندوز، هشدار صوتی، ترکیب هر دو یا حالت بی‌صدا. صداهای قطع و اتصال مجدد نیز از یکدیگر متمایزند.
- **مدیریت مستقل قوانین فایروال:** استفاده از قوانین اختصاصی برنامه برای جلوگیری از تداخل با سایر قوانین شخصی Windows Firewall.
- **نمایش وضعیت در System Tray:** مشاهده وضعیت فعلی برنامه از طریق آیکون رنگی کنار ساعت ویندوز.

### پیش‌نیازها

- ویندوز ۱۰ نسخه ۱۸۰۹ یا جدیدتر، یا ویندوز ۱۱
- .NET 10.0 Runtime یا SDK، متناسب با روش اجرا
- دسترسی Administrator برای مدیریت قوانین Windows Firewall

### ساخت و اجرا از سورس

ابتدا مخزن را دریافت کنید:

```powershell
git clone https://github.com/deldar/VpnKillSwitch.git
cd VpnKillSwitch
```

سپس پروژه را در حالت Release بسازید:

```powershell
dotnet build -c Release
```

برای اجرای برنامه، فایل خروجی را با دسترسی Administrator اجرا کنید:

```powershell
.\bin\Release\net10.0-windows\VpnKillSwitch.exe
```

### نکته مهم

VpnKillSwitch برای کاهش احتمال نشت ترافیک در اثر قطع VPN طراحی شده است. میزان محافظت به نحوه تشخیص وضعیت تونل، پیکربندی VPN، قوانین فایروال و نوع ترافیک بستگی دارد. پیش از اتکا به آن برای محافظت از ارتباطات حساس، عملکرد برنامه را در شرایط مختلف قطع و وصل VPN آزمایش کنید.
