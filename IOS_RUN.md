# تشغيل نسخة iOS — دليل عملي

> المشروع: `Tawsela.iOS/Tawsela.iOS.csproj` — الهدف `net10.0-ios` — أقل إصدار iOS **15.0** — المعرّف `com.tawsela.app`
>
> **قاعدة أساسية:** بناء وتشغيل تطبيقات iOS يحتاج **macOS + Xcode**. ويندوز يصلح لكتابة الكود والتحقق منه فقط (وهو ما تم هنا: 0 أخطاء).

---

## 1. الطريق الأسهل لو عندك ويندوز + جهاز Mac على نفس الشبكة

1. على الـ Mac: ثبّت **Xcode** من App Store ثم:
   ```bash
   sudo xcodebuild -license accept
   xcodebuild -runFirstLaunch
   ```
2. على الـ Mac: ثبّت **.NET 10 SDK** ثم:
   ```bash
   dotnet workload install ios
   ```
3. على ويندوز في **Visual Studio 2026**:
   - `Tools → iOS → Pair to Mac` واختر الجهاز.
   - افتح `Tawsela.iOS/Tawsela.iOS.csproj`.
   - اختر `Debug` + محاكي (مثل iPhone 16) واضغط **Run**.
   > المحاكي يظهر داخل ويندوز عبر Remote Simulator to Windows.

---

## 2. الطريق الثاني: سطر أوامر على macOS (الأوضح والأضمن)

### 2.1 المتطلبات

```bash
xcode-select -p            # لازم يطبع مسار Xcode
dotnet --version           # لازم 10.x
dotnet workload list       # لازم يظهر ios
```

إن لم يوجد الـ workload:
```bash
dotnet workload install ios
```

### 2.2 انسخ المشروع للـ Mac

```bash
# من ويندوز: اضغط المجلد أو استخدم git، ثم على الـ Mac:
cd ~/tawsela_play-ios3
```

> ⚠️ **مهم:** ملف `global.json` في جذر المشروع يثبّت إصدار SDK على `11.0.100-preview.4`.
> لو ثبّتت .NET 10 على الـ Mac، **احذفه** أو غيّر الإصدار:
> ```bash
> rm global.json
> # أو
> echo '{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }' > global.json
> ```

> 💡 لفتح المشروع في Rider أو Visual Studio: افتح **`Tawsela.iOS.sln`** (يضم `Tawsela.Core` + `Tawsela.iOS`).
> لا تفتح `tawsela_play.sln` — هذا خاص بأندرويد.

### 2.3 تشغيل على المحاكي

```bash
# Apple Silicon (M1/M2/M3/M4)
dotnet build Tawsela.iOS/Tawsela.iOS.csproj \
  -t:Run -f net10.0-ios -c Debug -r iossimulator-arm64

# Intel Mac
dotnet build Tawsela.iOS/Tawsela.iOS.csproj \
  -t:Run -f net10.0-ios -c Debug -r iossimulator-x64
```

`-t:Run` يفتح المحاكي ويثبّت التطبيق ويشغّله.

### 2.4 اختيار محاكي معيّن

```bash
xcrun simctl list devices available | grep iPhone
# خُذ الـ UDID ثم:
dotnet build Tawsela.iOS/Tawsela.iOS.csproj \
  -t:Run -f net10.0-ios -c Debug -r iossimulator-arm64 \
  -p:_DeviceName=:v2:udid=XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX
```

### 2.5 تشغيل يدوي (بدون `-t:Run`)

```bash
dotnet build Tawsela.iOS/Tawsela.iOS.csproj -c Debug -f net10.0-ios -r iossimulator-arm64

open -a Simulator
xcrun simctl boot "iPhone 16" 2>/dev/null || true
xcrun simctl install booted \
  Tawsela.iOS/bin/Debug/net10.0-ios/iossimulator-arm64/Tawsela.app
xcrun simctl launch booted com.tawsela.app
```

---

## 3. على iPhone حقيقي

يحتاج **Apple Developer** (حساب مجاني يكفي للتجربة 7 أيام، لكن Push يحتاج مدفوعًا).

1. سجّل UDID الجهاز في حساب المطوّر.
2. أنشئ **App ID** بالمعرّف `com.tawsela.app` (وفعّل Push إن أردت الإشعارات).
3. أنشئ Provisioning Profile ونزّله.
4. ابنِ:

```bash
dotnet build Tawsela.iOS/Tawsela.iOS.csproj \
  -t:Run -f net10.0-ios -c Release -r ios-arm64 \
  -p:CodesignKey="Apple Development: اسمك (TEAMID)" \
  -p:CodesignProvision="Tawsela Profile"
```

أو افتح `Tawsela.iOS.xcworkspace`/المشروع في Xcode بعد البناء لتشغيله من هناك.

---

## 4. بدون Mac خالص: GitHub Actions

الملف **`.github/workflows/ios-build.yml`** جاهز داخل المشروع. ارفع المشروع إلى GitHub ثم:

1. `Actions` → `ios-build` → **Run workflow**.
2. انتظر ~10–15 دقيقة.
3. نزّل الـ Artifact `Tawsela-simulator-NNN` → بداخله `Tawsela.app`.

ثم على أي Mac (أو خدمة Mac سحابية) ثبّته على المحاكي:

```bash
xcrun simctl boot "iPhone 16" 2>/dev/null || true
open -a Simulator
xcrun simctl install booted Tawsela.app
xcrun simctl launch booted com.tawsela.app
```

> الـ Workflow يتجاوز `global.json` تلقائيًا (لأنه يثبّت SDK 11 preview) ويستخدم .NET 10 المستقر.
> لبناء نسخة جهاز حقيقي (`.ipa`) تحتاج شهادات Apple — لا يمكن لـ CI فعل ذلك بدونها.

بديل آخر: خدمة Mac سحابية (MacinCloud / Scaleway / AWS EC2 mac) بنفس خطوات القسم 2.

---

## 5. ملاحظات خاصة بهذا التطبيق

| البند | التفصيل |
|---|---|
| **السيرفر** | `https://116.202.66.142:5001` بشهادة ذاتية. التثبيت يعمل فقط لأن `UseNativeHttpHandler=false` مضبوط في الـ csproj — **لا تحذفه** وإلا يفشل تسجيل الدخول |
| **أول تشغيل** | يحتاج إنترنت؛ الشاشة الأولى Splash ثم Intro ثم تسجيل الدخول |
| **الموقع** | على المحاكي: `Features → Location → Custom Location` (القاهرة مثلًا 30.0444, 31.2357) |
| **الكاميرا / Face ID** | لا تعمل على المحاكي — اختبرها على جهاز حقيقي |
| **الإدخال الصوتي** | يستخدم ميكروفون الـ Mac على المحاكي؛ التعرّف العربي قد لا يعمل بلا إنترنت |
| **Push** | لا يعمل على المحاكي ولا بدون شهادة APNs؛ لكن **الإشعارات الداخلية** (استطلاع دوري) تعمل دائمًا |
| **الخرائط** | WKWebView + Leaflet من أصول المشروع — تحتاج إنترنت لبلاطات الخرائط |
| **iPad** | التطبيق iPhone-first؛ يعمل على iPad بوضع التوافق |

---

## 6. أخطاء شائعة وحلولها

| الخطأ | الحل |
|---|---|
| `A valid Xcode installation could not be found` | `sudo xcode-select -s /Applications/Xcode.app` |
| `The current .NET SDK does not support targeting .NET 10` | ثبّت .NET 10 SDK، أو عدّل `global.json` |
| `error MT1007: Failed to launch the application` | غالبًا المحاكي غير مُقلع: `xcrun simctl boot "iPhone 16"` |
| `Could not find a valid provisioning profile` | شغّل على المحاكي، أو اضبط `CodesignProvision` |
| التطبيق يفتح ثم يغلق فورًا | افتح Console.app وابحث عن `Tawsela` — غالبًا استثناء في أول تحميل |
| فشل تسجيل الدخول على جهاز حقيقي | تأكد أن الجهاز يصل للـ IP (جرّب المتصفح على نفس الشبكة) |
| `NU1301` / فشل استعادة الحزم | `dotnet nuget locals all --clear` ثم أعد البناء |

---

## 7. للإصدار على App Store لاحقًا

1. استبدل الشهادة الذاتية بنطاق + Let's Encrypt، ثم احذف `NSAllowsArbitraryLoads` من `Info.plist`.
2. أضف `GoogleService-Info.plist` وفعّل `-p:EnableFirebase=true` إن أردت Push.
3. أضف `platform` إلى `Api.SetDeviceToken` على الخادم (iOS/Android).
4. Archive + Upload من Xcode، ثم App Store Connect.
