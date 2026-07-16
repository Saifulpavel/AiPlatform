# Troubleshooting & Environment Hacks (বাংলা)

> মেশিন/env-এর সমস্যা আর তাদের ফিক্স এখানে জমা হয়। কোড-এর bug নয় — **পরিবেশের** সমস্যা
> (port, Docker, Windows, driver ইত্যাদি) যেগুলো একবার সমাধান করলেও পরে আবার ভোগায়।
>
> নিয়ম: নতুন এমন কোনো hack/ফিক্স বের হলেই এই ফাইলে নতুন entry যোগ করা হবে। প্রতিটা entry-তে —
> **লক্ষণ (হুবহু error) → আসল কারণ → ফিক্স → কেন কাজ করে → এককালীন না বারবার।**

---

## সূচি
1. [SQL Server port 1433 — "forbidden by its access permissions"](#১-sql-server-port-1433--forbidden-by-its-access-permissions)
2. [Build "file locked by AiPlatform.Api" — চলন্ত API না মরা](#২-build-file-locked-by-aiplatformapi--চলন্ত-api-না-মরা)

---

## ১. SQL Server port 1433 — "forbidden by its access permissions"

**লক্ষণ** (`docker compose up -d`):
```
Error response from daemon: ports are not available: exposing port TCP 0.0.0.0:1433 -> 127.0.0.1:0:
listen tcp 0.0.0.0:1433: bind: An attempt was made to access a socket in a way forbidden by its access permissions.
```

> ⚠️ এটা সাধারণ "port already in use" নয়। "forbidden by its access permissions" = **Windows নিজেই** port-টা reserve করে রেখেছে, অন্য কোনো app নয়।

**আসল কারণ:** Hyper-V / WinNAT একটা dynamic TCP port range **সংরক্ষণ** করে, আর 1433 সেই range-এর ভেতরে পড়ে গেছে। এই reservation প্রতি reboot-এ এলোমেলোভাবে বদলায় — তাই আগে চলছিল, container বন্ধ থাকার ফাঁকে port ছাড়া পেল, Windows গিলে ফেলল।

**নির্ণয়** (normal PowerShell) — 1433 কি excluded range-এ?
```powershell
netsh interface ipv4 show excludedportrange protocol=tcp
```
আউটপুটে যদি কোনো range-এর `Start Port ≤ 1433 ≤ End Port` হয় (যেমন `1365  1464`), তাহলে এটাই সমস্যা।

**ফিক্স** — **Administrator PowerShell** (Start → PowerShell-এ ডান ক্লিক → "Run as administrator"):
```powershell
net stop winnat
netsh int ipv4 add excludedportrange protocol=tcp startport=1433 numberofports=1
net start winnat
```
তারপর:
```powershell
cd D:\Saiful\AiPlatform
docker compose up -d
docker ps
```

**কেন কাজ করে:**
- `net stop winnat` — Windows NAT থামায় → dynamic reservation ছেড়ে দেয়।
- `netsh ... add excludedportrange` — 1433-কে **administered exclusion** বানায়। মানে Windows-কে বলা "এই port আমার, dynamic allocation-এ কখনো নিও না"। explicit bind (Docker যেটা করে) তখনো কাজ করে। তালিকায় `*` চিহ্ন = administered।
- `net start winnat` — NAT ফিরিয়ে আনে।

**⚠️ সাবধানতা:** `net stop winnat` কয়েক সেকেন্ডের জন্য Docker-এর networking ফেলে দেয় — চলন্ত container-এর network হারাতে পারে, তাই পরে `docker compose up -d` আবার চালাও।

**এককালীন?** হ্যাঁ। administered exclusion reboot-এও টেকে, সমস্যা আর ফিরবে না।

**পার্শ্ব-নোট:** integration test (Testcontainers) এই সমস্যায় পড়ে না — ওরা random high port ব্যবহার করে, fixed 1433 নয়। তাই `dotnet test` চললেও `docker compose up` ফেল করতে পারে।

---

## ২. Build "file locked by AiPlatform.Api" — চলন্ত API না মরা

**লক্ষণ** (`dotnet build`/`dotnet run` চলমান API-র সময়):
```
error MSB3027: Could not copy "...\obj\Debug\net10.0\apphost.exe" to "bin\Debug\net10.0\AiPlatform.Api.exe".
Exceeded retry count of 10. Failed. The file is locked by: "AiPlatform.Api (22852)"
```

**আসল কারণ:** আগের `dotnet run` এখনো চলছে, `.exe` ধরে রেখেছে। build নতুন binary ওখানে কপি করতে পারছে না। প্রায়ই ঘটে যখন background-এ API চালিয়ে আবার build/run করি।

**Windows-এর ফাঁদ:** bash-এর `pkill -f "AiPlatform.Api"` এখানে **কাজ করে না** — process নামে ধরা যায় না, বা signal ঠিকমতো পৌঁছায় না। তাই "মেরেছি" মনে করলেও process বেঁচে থাকে, পরের build ওই একই lock error দেয়।

**ফিক্স** (PowerShell — এটাই নির্ভরযোগ্য):
```powershell
Get-Process -Name "AiPlatform.Api" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2
```
তারপর আবার build/run।

**নির্ণয়** — সত্যিই মরেছে কিনা:
```powershell
Get-Process -Name "AiPlatform.Api" -ErrorAction SilentlyContinue
```
কিছু না দেখালে পরিষ্কার। PID ধরে মারতে চাইলে: `Stop-Process -Id <pid> -Force`।

**কেন কাজ করে:** `Stop-Process -Force` Windows API দিয়ে সরাসরি process টার্মিনেট করে (bash signal-এর উপর নির্ভর করে না), তাই `.exe` lock ছাড়ে।

**এককালীন?** না — যতবার background API চালিয়ে rebuild করবে ততবার লাগতে পারে। অভ্যাস: rebuild-এর আগে উপরের Stop-Process চালাও, অথবা আগের `dotnet run` টার্মিনাল Ctrl+C দিয়ে বন্ধ করো।

---

<!-- নতুন entry উপরের ফরম্যাটে নিচে যোগ করো; সূচিতেও লাইন যোগ করো। -->
