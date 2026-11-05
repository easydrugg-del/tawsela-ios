# ملاحظات ماكينات GitHub Actions لهذا المستودع

## الخلاصة السريعة

| الاستخدام | الليبل الصحيح |
|---|---|
| بناء التطبيق + لقطات الشاشة | **`xcode-27`** |
| بث المحاكي للمتصفح (`flutter-remote`) | **`xcode-27`** |

---

## ليه `xcode-27` بالتحديد؟

| الليبل | الملاحظة |
|---|---|
| **`xcode-27`** | ✅ الأفضل. Xcode 27.1 موجود، المحاكي بيقلع، والسعة متاحة (4 تشغيلات ناجحة في ~5 دقايق في 4 نوفمبر 2026) |
| `macos-26` | ⚠️ نفس الصورة تقريبًا لكن **الطابور مزدحم جدًا**. 3 محاولات فشلت بانتظار 15 دقيقة دون الحصول على ماكينة |
| `macos-15` | ❌ الماكينة بتتوفر فورًا لكن **المحاكي لا يقلع** — جهاز يفضل `Shutdown` لأكثر من 19 دقيقة |
| `macos-15-intel` | ❌ Intel — `serve-sim` محتاج ARM64 |

---

## الأخطاء المتكررة ومعناها

### 1. `The job was not acquired by Runner of type hosted even after multiple attempts`

مش خطأ في الكود — GitHub ماعندهاش ماكينة macOS فاضية. الحل: غيّر الليبل إلى `xcode-27`.

### 2. `Simulator <UDID> never reached Booted state`

الماكينة اتوفّرت لكن CoreSimulator مش قادر يقلع الجهاز. شوف `--- runtimes ---` و`--- boot attempt ---` في اللوج:
- لو `boot` بيرجع خطأ صريح → المشكلة في الصورة.
- لو صامت والجهاز يفضل `Shutdown` → الصورة تالفة، غيّر الليبل.

### 3. اللمس/الكيبورد بيتبعت لكن مفيش أثر (أخطر مشكلة)

**الأعراض:** مقبس الإدخال بيتصل بنجاح (`ok: true`)، الإطارات بتتبعت (`sent: 2`)، لكن الشاشة ما بتتغيرش أبدًا.

**السبب:** Xcode 27 فيه **Device Hub**، ولما بيبقى نشط بيفصل خدمات الإدخال القديمة (legacy HID) بتاعة المحاكي. serve-sim بيرسل الإطارات وiOS بيرميها بصمت.

**الفحص:**
```bash
xcrun simctl spawn <UDID> notifyutil -g com.apple.coredevice.dtuhidd.active
# الناتج "com.apple.coredevice.dtuhidd.active 1" = الإدخال مقطوع
```

**الإصلاح** (لازم قبل تثبيت التطبيق — الأمر بيعيد تشغيل SpringBoard وبيقفل التطبيقات):
```bash
npx --yes serve-sim repair-input -d <UDID>
```

الـ workflow فيه خطوة `Repair Device Hub input (Xcode 27)` بتعمل الفحص والإصلاح تلقائيًا، وبترفض تكمل لو المفتاح فضل `1`.
وفيه كذلك `SERVE_SIM_DISABLE_DEVICE_HUB_KEYBOARD=1` عشان الكيبورد يمشي على نفس المسار القديم.

**فحص سريع بعد التشغيل:** `GET /live-diag/<TOKEN>` بيرجّع `inputShadowed` و`deviceHubInput`.

### 4. `This serve-sim preview requires an auth token`

صفحة serve-sim نفسها محتاجة التوكن في الـ **fragment** مش الـ query:
```
https://<tunnel>/?k=<TOKEN>#token=<TOKEN>
```

### 5. `Remote access is disabled`

`serve-sim` بيرفض الوصول البعيد افتراضيًا. لازم `--auth-token` (مضاف في الـ workflow).

### 4. `Flutter Remote Session Locked`

البوابة بتقبل التوكن من `?k=` مرة واحدة وبعدها بتخزّنه في كوكي. لو ضاع الـ `?k=`، افتح الرابط كامل من جديد.

---

## طريقة التشغيل

```powershell
cd 'D:\programing\CafeManagement\tawsela_ios_ci'
flutter-remote up --app-release Tawsela-sim.zip --minutes 90 `
  --device "iPhone 17" --runner xcode-27 `
  --max-dimension 720 --fps 15 --no-open
```

## إيقاف الجلسة

```powershell
flutter-remote down
```
