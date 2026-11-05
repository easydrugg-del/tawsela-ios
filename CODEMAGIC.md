# البناء على Codemagic

الملف `codemagic.yaml` في جذر الريبو يجهّز **نسختين** من التطبيق:

| Workflow | الناتج | الاستخدام |
|---|---|---|
| `ios-simulator` | `Tawsela-sim.zip` (‏`.app`) | تشغيل على محاكي iOS |
| `ios-device-ipa` | `Tawsela-<ver>-<build>-unsigned.ipa` | تثبيت على iPhone حقيقي عبر **Sideloadly** |

> البناء يستخدم **Xcode 27.1** لأن حزمة .NET iOS 27.1 مبنية عليه.
> ويثبّت .NET 10 تلقائيًا لو مش موجود على الماكينة.

---

## 1) ربط الريبو (مرة واحدة)

1. افتح <https://codemagic.io/apps> وسجّل دخول بحساب GitHub.
2. **Add application** → اختر **GitHub** → اسمح بالوصول.
3. اختر الريبو: `easydrugg-del/tawsela-ios`.
4. Codemagic هيقرأ `codemagic.yaml` تلقائيًا (اختَر **codemagic.yaml** لو سألك عن طريقة الإعداد).

---

## 2) تشغيل بناء

من صفحة التطبيق: **Start new build** → اختر الفرع `main` → اختر الـ workflow:

- **iOS Device IPA (unsigned)** ← لو عايز `.ipa` للآيفون
- **iOS Simulator (.app)** ← لو عايز نسخة المحاكي

مدة البناء المتوقعة: **10–15 دقيقة** (أول مرة أطول بسبب تحميل .NET + workload).

---

## 3) تنزيل الناتج

بعد ما البناء يخلص، تحت في صفحة البناء: **Artifacts** → نزّل `.ipa` أو `.zip`.

---

## 4) التشغيل من سطر الأوامر (اختياري)

محتاج **API token**: من Codemagic → **Teams** → **Personal account** → **Integrations** → **Codemagic API** → **Show token**.

```bash
TOKEN="ضع-التوكن-هنا"

# إضافة التطبيق (مرة واحدة) — بيرجّع appId
curl -s -H "Content-Type: application/json" -H "x-auth-token: $TOKEN" \
  -d '{"repositoryUrl":"https://github.com/easydrugg-del/tawsela-ios"}' \
  -X POST https://api.codemagic.io/apps

# بدء بناء
curl -s -H "Content-Type: application/json" -H "x-auth-token: $TOKEN" \
  -d '{"appId":"<APP_ID>","workflowId":"ios-device-ipa","branch":"main"}' \
  -X POST https://api.codemagic.io/builds

# متابعة الحالة
curl -s -H "x-auth-token: $TOKEN" https://api.codemagic.io/builds/<BUILD_ID>
```

---

## 5) الحدود

- الخطة المجانية: **500 دقيقة/شهر** على ماكينات macOS، وبيتخصم منها وقت البناء فقط.
- الماكينة المستخدمة: `mac_mini_m2` (متاحة في المجاني). `mac_mini_m4` تحتاج حساب مدفوع.
- الناتج محفوظ في Codemagic لمدة **30 يوم** تقريبًا — بعدها نزّله واحفظه.

---

## بديل جاهز

لو مش عايز تربط Codemagic دلوقتي، الـ `.ipa` الحالي موجود على GitHub Releases:

```
https://github.com/easydrugg-del/tawsela-ios/releases/tag/device-ipa
```

ونسخة المحاكي:

```
https://github.com/easydrugg-del/tawsela-ios/releases/tag/flutter-remote-build
```
