# مراجعة الامتثال لمتاجر التطبيقات — توصيلة (Android + iOS)

> تاريخ المراجعة: 2026-11-04
> المرجع: كود المشروع الفعلي + `AndroidManifest.xml` + `Info.plist` + `PrivacyInfo.xcprivacy` + `PLAY_STORE_CHECKLIST.md`

---

## 1. الحكم النهائي (بصراحة)

| السؤال | الجواب |
|---|---|
| **هل النسختان تحققان الشروط التقنية؟** | **نعم تقريبًا** — النواقص التقنية الحقيقية (أيقونة بألفا، ATS، حذف حساب، إبلاغ/حظر) كلها مُصلحة الآن. |
| **هل سيُقبل التطبيق تلقائيًا؟** | **لا. القبول ليس مضمونًا** — لأن هناك **5 مخاطر حقيقية** بعضها خارج الكود (مالية/قانونية/تشغيلية). |
| **أندرويد (Google Play)** | التطبيق **منشور بالفعل** (1.3.27) وهذا يعني أنه اجتاز المراجعة سابقًا. الخطر الأكبر هو **إقرار الخدمات المالية**. |
| **iOS (App Store)** | **أول تقديم** = تدقيق أشد. أتوقع **دورة رفض واحدة على الأقل** إذا لم تُجهَّز النقاط في القسم 4. |

> باختصار: **الكود جاهز. الملفات (App Review / Play Console) هي اللي هتحدد القبول.**

---

## 2. Apple App Store — بند بند

| الدليل | البند | الحالة | الملاحظة |
|---|---|---|---|
| **2.1** | اكتمال التطبيق (حساب تجريبي للمراجعة) | ❌ **مطلوب منك** | التطبيق كله خلف تسجيل دخول — بدون حساب ديمو **سيُرفض حتمًا** |
| **2.1** | التطبيق يعمل وقت المراجعة | ⚠️ **خطر** | السيرفر `https://116.202.66.142:5001` — IP خام + بورت غير قياسي |
| **2.3** | بيانات وصفية دقيقة | ⚠️ | لا تذكر «دفع داخل التطبيق» في الوصف — كل الدفع خارجي |
| **2.5.1** | APIs خاصة | ✅ | لا يوجد استخدام |
| **3.1.1** | IAP للسلع الرقمية | ✅ | لا نبيع محتوى رقميًا |
| **3.1.5(a)** | خدمات خارج التطبيق | ⚠️ **يحتاج تبرير** | محفظة + اشتراك سائق — انظر القسم 4 |
| **4.2** | الحد الأدنى من الوظائف | ✅ | 79 شاشة ووظائف كاملة |
| **4.8** | Sign in with Apple | ✅ **لا ينطبق** | لا يوجد تسجيل عبر Google/Facebook — اسم/كلمة مرور فقط |
| **5.1.1(v)** | حذف الحساب **داخل التطبيق** | ✅ | `ProfileViewController.DeleteAccount` + معالجة الرصيد المتبقي |
| **5.1.1** | سياسة الخصوصية (رابط) | ✅ | `https://tawsela-app.blogspot.com/p/privacy-policy.html` |
| **5.1.2** | Data Use / Privacy Manifest | ✅ | `PrivacyInfo.xcprivacy` + App Privacy في الكونسول |
| **5.1.5** | بيانات الموقع | ✅ | `NSLocationWhenInUseUsageDescription` بالعربية |
| **1.2** | محتوى المستخدمين (UGC) | ✅ | محادثة + **إبلاغ** + **حظر** + فريق مراجعة |
| **1.4.1** | ضرر جسدي (نقل/توصيل) | ⚠️ **خطر** | آبل قد تطلب إثبات ترخيص النشاط في مصر |
| **2.5.4** | الموقع في الخلفية | ✅ | غير مستخدم (`UIBackgroundModes` = remote-notification فقط) |
| **ATT** | تتبع المستخدم | ✅ | لا يوجد تتبع ولا SDK إعلاني |
| **ATS** | `NSAllowsArbitraryLoads` | ✅ **أُزيل** | لم يكن لازمًا أصلًا (انظر القسم 5) |
| **—** | أيقونة بدون قناة ألفا | ✅ **مُصلحة** | كانت سبب رفض مؤكد (`ITMS-90717`) |
| **—** | iPad | ✅ **مؤكد** | `UIDeviceFamily = [1]` (iPhone فقط) — تحقّق آلي في CI، فلا خطر رفض بسبب تخطيط iPad |

---

## 3. Google Play — بند بند

| البند | الحالة | الملاحظة |
|---|---|---|
| **Target API level** | ✅ | `net10.0-android36.0` (المطلوب 35+) |
| **16 KB page size** | ✅ | مذكور ومُتحقَّق في `PLAY_STORE_CHECKLIST.md` |
| **Data safety form** | ⚠️ | لازم يطابق الكود: الموقع الدقيق، الاسم، الهاتف، الصور، معلومات الدفع |
| **Account deletion** | ✅ | داخل التطبيق + رابط ويب |
| **UGC policy** | ✅ | إبلاغ + حظر |
| **Background location** | ✅ | **غير مطلوب** — لا يوجد `ACCESS_BACKGROUND_LOCATION` |
| **Foreground service** | ✅ | لا توجد خدمات خلفية |
| **الصلاحيات الحساسة** | ✅ | لا `READ_MEDIA_*` ولا `MANAGE_EXTERNAL_STORAGE` ولا `QUERY_ALL_PACKAGES` |
| **الإعلانات** | ✅ | إعلانات داخلية للمنصة فقط، لا SDK إعلاني |
| **Payments policy** | ⚠️ | اشتراك السائق خارج Play Billing |
| **Financial services** | ⚠️ **أعلى خطر** | محفظة + شحن + سحب |
| **App access (بيانات دخول للمراجعة)** | ⚠️ | لازم حساب ديمو في Play Console |

---

## 4. المخاطر الحقيقية (مرتبة بالأهمية)

### 🔴 1) المحفظة والأموال — أعلى خطر في المتجرين
التطبيق فيه: شحن محفظة بتحويل بنكي + رفع إيصال + موافقة إدارية + سحب أرباح للسائق.

- **Google Play:** سياسة *Financial services* تطلب أن يكون التطبيق **مؤسسة مالية مرخّصة** أو **شريكًا لمزوّد مرخّص**، إلا إذا كانت المحفظة **مغلقة (closed-loop)** تُستخدم فقط لدفع خدمات التطبيق نفسه.
- **Apple:** نفس المنطق تحت 3.1.1/3.1.5.

**ما يجب فعله:** في Play Console → *App content → Financial features* صرّح بأنها **محفظة مغلقة لدفع خدمات النقل/التوصيل فقط**، ولا تُستخدم للتحويل بين المستخدمين (وهذا صحيح فعليًا: `WalletTransferEnabled = false`). وأي تحويل P2P مستقبلًا يحتاج ترخيصًا.

### 🔴 2) اشتراك السائق خارج أنظمة الدفع
`POST /api/subscription/subscribe` — اشتراك شهري/سنوي للسائق.

- **Apple 3.1.5(a):** مقبول لأنه **خدمة فعلية** (وصول للمنصة) وليس ميزة رقمية.
- **Google:** مقبول كـ *service fee* لكن **يجب** توضيحه في وصف التطبيق وأن الدفع يتم خارج المتجر.

**الخطر:** لو صنّفه المراجع كـ "اشتراك رقمي" → رفض. الحل: سمّه دائمًا **«رسم تشغيل/اشتراك مهني للسائق»** وليس «اشتراك مميزات».

### 🟠 3) السيرفر: IP خام + شهادة ذاتية + بورت 5001
- المراجع سيفتح التطبيق على شبكة عادية. لو الشبكة حجبت 5001 → **«التطبيق لا يعمل»** = رفض.
- **الحل الموصى به:** نطاق حقيقي + شهادة Let's Encrypt على **443**، ثم إزالة التثبيت الذاتي للشهادة من `Api.cs`.
- حتى ذلك الحين: أضف في App Review Notes أن السيرفر يعمل واختُبر.

### 🟠 4) حساب تجريبي للمراجعة (إلزامي)
- **Apple:** App Store Connect → *App Review Information* → Demo account.
- **Google:** Play Console → *App content → App access*.
- **مهم:** أعطهم **حساب عميل عادي**، **وليس أدمن** — لأن التطبيق يحتوي لوحات إدارة (27 قسمًا) وقد يثير سؤالًا عن سبب وجودها في تطبيق مستهلك.

### 🟡 5) تراخيص النشاط (مصر)
- **Apple 1.4.1:** تطبيقات النقل قد تُطلب إثبات ترخيص.
- **Google:** قد تطلب نفس الشيء عند التصريح بالخدمات المالية.

**الحل:** جهّز مستندًا/رابطًا يثبت أن النشاط مسجّل، واحتفظ به جاهزًا لو طلبه المراجع.

---

## 5. ما تم إصلاحه في الكود (هذه الجولة)

| # | التغيير | السبب |
|---|---|---|
| 1 | **إزالة `NSAllowsArbitraryLoads` من `Info.plist`** | لم يكن مطلوبًا: نداءات الـ API تمر عبر `SocketsHttpHandler` (لأن `UseNativeHttpHandler=false`) وهو لا يخضع لـ ATS، وبلاطات الخرائط كلها HTTPS بشهادات صحيحة. وجوده كان **علامة حمراء** في المراجعة |
| 2 | **إزالة قناة ألفا من 15 أيقونة** | `ITMS-90717` — رفض مؤكد عند الرفع |
| 3 | **تصحيح نسبة شعار الإقلاع** | كان مضغوطًا أفقيًا |
| 4 | **ربط `Entitlements.plist`** | كان غير مرتبط فلم يُطبَّق `aps-environment` |
| 5 | **حارس Popover على iPad** في شاشتي الإحالة والإيصال | كان Crash على iPad |
| 6 | **نسخ أصول Leaflet متكررًا** | كانت `images/` لا تُنسخ → علامات الخريطة مكسورة |

**تحقق:** البناء النظيف **0 Warning / 0 Error**، و`Info.plist` و`PrivacyInfo.xcprivacy` صالحان كـ XML.

**تحقق آلي من داخل الـ CI على `.app` المبني فعليًا (run 41805851781):**

| المفتاح | القيمة |
|---|---|
| `CFBundleIdentifier` | `com.tawsela.app` |
| `CFBundleShortVersionString` | `1.3.27` |
| `UIDeviceFamily` | `[1]` → **iPhone فقط** |
| `NSAppTransportSecurity` | **غير موجود** (لا استثناءات) |
| حجم الحزمة | 60 MB |

---

## 6. ما يجب عليك فعله (ليس كود)

### Apple
1. **حساب ديمو** في App Review Information (عميل، برصيد بسيط).
2. **App Privacy** في App Store Connect — يجب أن يطابق `PrivacyInfo.xcprivacy`:
   - الموقع الدقيق ✅
   - الاسم / الهاتف ✅
   - صور (إيصالات/مستندات) ✅
   - معلومات الدفع ✅
   - **Tracking = No** ✅
3. **Age rating** — 4+ (لا محتوى حساس).
4. **Export Compliance** — التطبيق يستخدم HTTPS قياسي → *exempt* (أو `ITSAppUsesNonExemptEncryption = false`).
5. **رابط سياسة الخصوصية** في صفحة التطبيق (نفس رابط Blogger).
6. **Screenshots** — 6.7" و6.5" على الأقل.

### Google
1. **Data safety** — راجع `PLAY_STORE_CHECKLIST.md` قسم 4 وتأكد أنه محدّث.
2. **App access** — بيانات الحساب الديمو.
3. **Financial features** — التصريح المذكور في القسم 4.1.
4. **Content rating** — استبيان جديد إن تغيّرت الميزات.

---

## 7. نص جاهز لـ App Review Notes (انسخه كما هو)

```
Tawsela is a ride-hailing and delivery platform in Egypt.

DEMO ACCOUNT (customer):
  username: <ضع الحساب هنا>
  password: <ضع كلمة المرور هنا>

Notes for the reviewer:
- The app requires a live backend; it is reachable 24/7.
- All payments are for REAL-WORLD services (rides / delivery orders).
  The in-app wallet is CLOSED-LOOP: it can only be used to pay for rides
  and delivery inside this app. There is no peer-to-peer money transfer.
- Driver subscription is a professional service fee for drivers,
  paid outside the app. It is not a digital content subscription.
- Account deletion is available in-app: Profile → حذف الحساب.
- Chat between customer and driver supports reporting and blocking.
- No third-party login is offered, so Sign in with Apple is not applicable.
- No tracking / no advertising SDKs.
- The admin and driver sections are part of the same business app and are
  not accessible from the demo customer account.
```

---

## 8. الخلاصة

- **تقنيًا:** النسختان الآن في أفضل حالة ممكنة من ناحية الكود — لا مخالفات معروفة في الصلاحيات أو المحتوى أو الخصوصية.
- **إجرائيًا:** القبول يعتمد على **4 ملفات**: الحساب الديمو، App Privacy، تصريح الخدمات المالية، وإثبات النشاط.
- **أكبر تهديد واحد:** المحفظة. لو طُلب ترخيص مالي، البديل الأسرع هو تحويل الشحن إلى **دفع مباشر للرحلة** بدون رصيد مخزّن.
